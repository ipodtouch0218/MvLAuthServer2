using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using MvLAuthServer2.Models.Photon.Webhook;
using MvLAuthServer2.Models.Photon.Webhook.PUN;
using MvLAuthServer2.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MvLAuthServer2.Endpoints
{
    public static class PunWebhookEndpoints
    {
        public static readonly Dictionary<Guid, string> CurrentIPs = new();
        private static readonly Dictionary<string, string> RoomBans = new();

        public static IEndpointRouteBuilder MapPunWebhookEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var punWebhooksGroup = endpoints.MapGroup("/webhooks");

            punWebhooksGroup.AddEndpointFilter(async (context, next) =>
            {
                var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                string authSecret = config["Secrets:PunWebhooks"];

                context.HttpContext.Request.Headers.TryGetValue("X-Secret", out StringValues key);
                if (key == StringValues.Empty || key != authSecret)
                {
                    var log = context.HttpContext.RequestServices.GetRequiredService<ILog>();
                    log.Warn($"REQ: Got PUN Webhook request ({context.HttpContext.Request.Path.Value}) with an invalid key! Key supplied: {key}");
                    return Utils.CreateResult(new WebhookResult(WebhookResultCode.Failed_Application, "Bad Request (Invalid Request Origin)"), 400);
                }

                return await next(context);
            });

            punWebhooksGroup.MapPost("/RoomCreate", RoomCreate);
            punWebhooksGroup.MapPost("/PlayerConnect", PlayerConnect);
            punWebhooksGroup.MapPost("/PlayerLeave", PlayerLeave);
            punWebhooksGroup.MapPost("/RoomProperties", (Delegate)RoomProperties);
            punWebhooksGroup.MapPost("/RoomClose", (Delegate)RoomClose);

            // Fallback
            punWebhooksGroup.MapPost("/{name}", (HttpContext context, string name) => Utils.CreateResult(new WebhookResult(WebhookResultCode.Success, "Success"), 200));

            return endpoints;
        }

        private static async Task<IResult> RoomCreate(HttpContext context, IBanService banService, IUserEntryLogService userEntryLogService, ILog log)
        {
            string json;
            using (StreamReader sr = new(context.Request.Body))
            {
                json = await sr.ReadToEndAsync();
            }

            WebhookData? data = JsonConvert.DeserializeObject<WebhookData>(json);
            if (data == null)
            {
                return Utils.CreateResult(new WebhookResult(WebhookResultCode.Failed_Protocol, "Bad Request (No Data)"), 400);
            }

            // Check for GUID.
            if (!Guid.TryParse(data.UserId, out Guid userId))
            {
                return Utils.CreateResult(new WebhookResult(WebhookResultCode.Failed_Application, $"Rejected (Invalid UserID {data})"), 400);
            }

            // Check for IP.
            string? ip = null;
            if (data.AuthCookie != null && data.AuthCookie.ContainsKey("ip"))
            {
                ip = (string)data.AuthCookie["ip"];
            }
            else if (CurrentIPs.ContainsKey(userId))
            {
                ip = CurrentIPs[userId];
            }
            uint? ipNumberNullable = Utils.IpToNumber(ip);

            string version = data.AppVersion!;
            string[] splitVersion = version.Split('.');
            if (int.TryParse(splitVersion[0], out int release) && int.TryParse(splitVersion[1], out int major))
            {
                if (release > 1 || (release == 1 && major > 6))
                {
                    if (ip == null)
                    {
                        // 1.7, but no IP. Reject.
                        return Utils.CreateResult(new WebhookResult(WebhookResultCode.Failed_Application, "Rejected (Invalid AuthCookie for given AppVersion)"), 400);
                    }

                    if (data.GameId!.Length != 8)
                    {
                        // 1.7, but the game ID isn't 8 chars. Reject.
                        return Utils.CreateResult(new WebhookResult(WebhookResultCode.Failed_Application, "Rejected (Invalid Game Arguments)"), 400);
                    }
                }
            }

            // Check that they aren't banned
            var banResult = await banService.CheckForBansAsync("RoomCreate", userId, ip);
            if (banResult != null)
            {
                return banResult;
            }

            // Update nicknames + ip history
            await userEntryLogService.SavePlayerInfoGuid(userId, ipNumberNullable, data.Nickname);

            log.Info($"GAME: ({data.AppVersion}) [{data.Region}-{data.GameId}]: {data.Nickname} Created '{userId}' ({ip})");

            // *** Allow join.
            return Utils.CreateResult(new WebhookResult(WebhookResultCode.Success, "Success"));
        }

        private static async Task<IResult> PlayerConnect(HttpContext context, IBanService banService, IUserEntryLogService userEntryLogService, ILog log)
        {
            string json;
            using (StreamReader sr = new(context.Request.Body))
            {
                json = await sr.ReadToEndAsync();
            }

            WebhookData? data = JsonConvert.DeserializeObject<WebhookData>(json);
            if (data == null)
            {
                return Utils.CreateResult(new WebhookResult(WebhookResultCode.Failed_Protocol, "Bad Request (No Data)"), 400);
            }

            // Check for GUID.
            if (!Guid.TryParse(data.UserId, out Guid userId))
            {
                return Utils.CreateResult(new WebhookResult(WebhookResultCode.Failed_Application, $"Rejected (Invalid UserID)"), 400);
            }

            // Check for IP.
            string? ip = null;
            if (data.AuthCookie != null && data.AuthCookie.ContainsKey("ip"))
            {
                ip = (string)data.AuthCookie["ip"];
            }
            else if (CurrentIPs.ContainsKey(userId))
            {
                ip = CurrentIPs[userId];
            }
            else
            {
                // Failed to get the IP. Before 1.7, this is ok. Check the version and if it's lower than 1.7.0.0, reject.
                string version = data.AppVersion!;
                string[] splitVersion = version.Split('.');
                if (int.TryParse(splitVersion[0], out int release) && int.TryParse(splitVersion[1], out int major))
                {
                    if (release > 1 || (release == 1 && major > 6))
                    {
                        // 1.7, but no IP. Reject.
                        return Utils.CreateResult(new WebhookResult(WebhookResultCode.Failed_Application, "Rejected (Invalid AuthCookie for given AppVersion)"), 400);
                    }
                }
            }

            uint? ipNumberNullable = Utils.IpToNumber(ip);

            // Check that they aren't banned
            var banResult = await banService.CheckForBansAsync("RoomJoin", userId, ip);
            if (banResult != null)
            {
                return banResult;
            }

            // Room bans
            if (RoomBans.TryGetValue(data.GameId!, out string roomBanList))
            {
                //int bans = sql.ExecuteScalar<int>("SELECT COUNT(*) FROM UserEntry e JOIN IpLog l ON e.UserId = l.UserId WHERE e.UserId IN (?) AND l.IpAddress = ?", roomBanList, ipAsNumber);
                ;           //if (bans > 0)
                if (roomBanList.Contains(userId.ToString()))
                {
                    log.Info($"GAME: ({data.AppVersion}) [{data.Region}-{data.GameId}]: {data.Nickname} Tried to join, but is banned from the room! '{userId}' ({ip})");
                    return Utils.CreateResult(new WebhookResult(WebhookResultCode.Failed_Application, "Rejected (User is banned from room)"), 403);
                }
            }

            // Update nicknames + ip
            await userEntryLogService.SavePlayerInfoGuid(userId, ipNumberNullable, data.Nickname);

            log.Info($"GAME: ({data.AppVersion}) [{data.Region}-{data.GameId}]: {data.Nickname} Joined Room '{userId}' ({ip})");

            // *** Allow join.
            return Utils.CreateResult(new WebhookResult(WebhookResultCode.Success, "Success"));
        }

        private static async Task<IResult> PlayerLeave(HttpContext context, ILog log)
        {
            string json;
            using (StreamReader sr = new(context.Request.Body))
            {
                json = await sr.ReadToEndAsync();
            }

            WebhookData? data = JsonConvert.DeserializeObject<WebhookData>(json);
            if (data == null)
            {
                return Utils.CreateResult(new WebhookResult(WebhookResultCode.Failed_Protocol, "Bad Request (No Data)"), 400);
            }

            // Check for GUID.
            if (!Guid.TryParse(data.UserId, out Guid userId))
            {
                return Utils.CreateResult(new WebhookResult(WebhookResultCode.Failed_Application, $"Rejected (Invalid UserID)"), 400);
            }

            // Check for IP.
            string? ip = null;
            if (data.AuthCookie != null && data.AuthCookie.ContainsKey("ip"))
            {
                ip = (string)data.AuthCookie["ip"];
            }
            else if (CurrentIPs.ContainsKey(userId))
            {
                ip = CurrentIPs[userId];
            }
            else
            {
                // Failed to get the IP. Before 1.7, this is ok. Check the version and if it's lower than 1.7.0.0, reject.
                string version = data.AppVersion!;
                string[] splitVersion = version.Split('.');
                if (int.TryParse(splitVersion[0], out int release) && int.TryParse(splitVersion[1], out int major))
                {
                    if (release > 1 || (release == 1 && major > 6))
                    {
                        // 1.7, but no IP. Reject.
                        return Utils.CreateResult(new WebhookResult(WebhookResultCode.Failed_Application, "Rejected (Invalid AuthCookie for given AppVersion)"), 400);
                    }
                }
            }

            log.Info($"GAME: ({data.AppVersion}) [{data.Region}-{data.GameId}]: {data.Nickname} Left '{data.UserId}' ({ip})");

            return Utils.CreateResult(new WebhookResult(WebhookResultCode.Success, "Success"), 200);
        }

        private static async Task<IResult> RoomProperties(HttpContext context)
        {
            string json;
            using (StreamReader sr = new(context.Request.Body))
            {
                json = await sr.ReadToEndAsync();
            }

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (data == null)
            {
                return Utils.CreateResult(new WebhookResult(WebhookResultCode.Failed_Protocol, "Bad Request (No Data)"), 400);
            }

            if (!data.TryGetValue("Properties", out object properties))
            {
                return Utils.CreateResult(new WebhookResult(WebhookResultCode.Failed_Protocol, "Bad Request (No 'Properties' in JSON data)"), 401);
            }

            Dictionary<string, object> propertyMap = ((JObject)properties).ToObject<Dictionary<string, object>>();

            if (!propertyMap.TryGetValue("B", out object banObject) || banObject is not JArray banArray)
            {
                return Utils.CreateResult(new WebhookResult(WebhookResultCode.Success, "Success"), 200);
            }

            if (banArray.Count <= 0)
            {
                return Utils.CreateResult(new WebhookResult(WebhookResultCode.Success, "Success"), 200);
            }

            string? banString;
            if (banArray.Count <= 0 || (banString = banArray[0].ToObject<string>()) == null)
            {
                RoomBans.Remove((string)data["GameId"]);
            }
            else
            {
                RoomBans[(string)data["GameId"]] = string.Join(',',
                    banString.Split(',').Select(b64 => Encoding.UTF8.GetString(Utils.FromBase64(b64)).Split(':')[1])
                        .Select(userId => '\'' + userId + '\'')
                        .ToArray());
            }

            return Utils.CreateResult(new WebhookResult(WebhookResultCode.Success, "Success"), 200);
        }

        private static async Task<IResult> RoomClose(HttpContext context)
        {
            string json;
            using (StreamReader sr = new(context.Request.Body))
            {
                json = await sr.ReadToEndAsync();
            }

            WebhookData? data = JsonConvert.DeserializeObject<WebhookData>(json);
            if (data == null)
            {
                return Utils.CreateResult(new WebhookResult(WebhookResultCode.Failed_Protocol, "Bad Request (No Data)"), 400);
            }

            RoomBans.Remove(data.GameId);

            return Utils.CreateResult(new WebhookResult(WebhookResultCode.Success, "Success"), 200);
        }
    }
}
