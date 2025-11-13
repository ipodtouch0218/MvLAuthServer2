using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using MvLAuthServer2.Models.Database;
using MvLAuthServer2.Models.Photon.Webhook;
using MvLAuthServer2.Models.Photon.Webhook.Quantum;
using MvLAuthServer2.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MvLAuthServer2.Endpoints
{
    public static class QuantumWebhookEndpoints
    {
        private static readonly Dictionary<Guid, string> Nicknames = new();
        private static readonly Dictionary<string, string> RoomVersions = new();

        public static IEndpointRouteBuilder MapQuantumWebhookEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var quantumWebhooksGroup = endpoints.MapGroup("/quantum");

            quantumWebhooksGroup.AddEndpointFilter(RequireKeyEndpointFilter);

            quantumWebhooksGroup.MapPost("/game/configs", GameConfigs);
            quantumWebhooksGroup.MapPost("/game/create", GameCreate);
            quantumWebhooksGroup.MapPost("/game/join", GameJoin);
            quantumWebhooksGroup.MapPost("/game/leave", GameLeave);
            quantumWebhooksGroup.MapPost("/game/close", GameClose);
            quantumWebhooksGroup.MapPost("/player/add", PlayerAdd);

            return endpoints;
        }

        private static async ValueTask<object?> RequireKeyEndpointFilter(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            string authSecret = config["Secrets:QuantumWebhooks"];

            context.HttpContext.Request.Headers.TryGetValue("X-SecretKey", out StringValues key);
            if (key == StringValues.Empty || key != authSecret)
            {
                var log = context.HttpContext.RequestServices.GetRequiredService<ILog>();
                log.Warn($"REQ: Got Quantum Webhook request ({context.HttpContext.Request.Path.Value}) with an invalid key! Key supplied: {key}");
                return Utils.CreateResult(new WebhookResult(WebhookResultCode.Failed_Application, "Bad Request (Invalid Request Origin)"), 400);
            }

            return await next(context);
        }

        private static async Task<IResult> GameConfigs(HttpContext context, IConfigFileService configFileService, ILog log)
        {
            string json;
            using (StreamReader sr = new(context.Request.Body))
            {
                json = await sr.ReadToEndAsync();
            }

            GameConfigsRequest request = JsonConvert.DeserializeObject<GameConfigsRequest>(json);
            GameConfigsResponse response = new()
            {
                SessionConfig = configFileService.SessionConfig
            };

            if (request.SessionConfig != null && response.SessionConfig != null)
            {
                response.SessionConfig.InputFixedSize = request.SessionConfig.InputFixedSize;
            }

            log.Info(JsonConvert.SerializeObject(response));
            return Utils.CreateResult(response);
        }

        private static async Task<IResult> GameCreate(HttpContext context, IBanService banService, ILog log)
        {
            string json;
            using (StreamReader sr = new(context.Request.Body))
            {
                json = await sr.ReadToEndAsync();
            }

            CreateGameRequest request = JsonConvert.DeserializeObject<CreateGameRequest>(json);

            string ip = (string)request.AuthCookie["ip"];
            Guid.TryParse(request.UserId, out Guid userId);

            // Check that they aren't banned
            IResult? banResult = await banService.CheckForBansAsync($"CREATE {request.GameId}", userId, ip);
            if (banResult != null)
            {
                return banResult;
            }

            CreateGameResponse response = new CreateGameResponse
            {
                RoomOptions = new RoomOptions
                {
                    PublishUserId = true,
                    SuppressPlayerInfo = false,
                    SuppressRoomEvents = false,
                    EmptyRoomTtl = 0,
                    PlayerTtl = 0,
                }
            };

            RoomVersions[request.GameId] = request.AppVersion;
            log.Info($"GAME: ({request.AppVersion}) [{request.GameId}]: {userId} Created Room ({ip})");
            return Utils.CreateResult(response, 200);
        }

        private static async Task<IResult> GameJoin(HttpContext context, IBanService banService, ILog log)
        {
            string json;
            using (StreamReader sr = new(context.Request.Body))
            {
                json = await sr.ReadToEndAsync();
            }

            JoinGameRequest request = JsonConvert.DeserializeObject<JoinGameRequest>(json);

            string ip = (string)request.AuthCookie["ip"];
            Guid.TryParse(request.UserId, out Guid userId);

            // Check that they aren't banned
            IResult? banResult = await banService.CheckForBansAsync($"JOIN {request.GameId}", userId, ip);
            if (banResult != null)
            {
                return banResult;
            }

            JoinGameResponse response = new JoinGameResponse
            {
                MaxPlayerSlots = 1,
            };

            RoomVersions.TryGetValue(request.GameId, out string version);
            // log.Info($"GAME: ({version}) [{request.GameId}]: {userId} Joined Room ({ip})");
            return Utils.CreateResult(response, 200);
        }

        private static async Task<IResult> GameLeave(HttpContext context, ILog log)
        {
            string json;
            using (StreamReader sr = new(context.Request.Body))
            {
                json = await sr.ReadToEndAsync();
            }

            LeaveGameRequest request = JsonConvert.DeserializeObject<LeaveGameRequest>(json);

            string ip = (string)request.AuthCookie["ip"];
            Guid.TryParse(request.UserId, out Guid userId);

            RoomVersions.TryGetValue(request.GameId, out string version);
            Nicknames.TryGetValue(userId, out string nickname);
            log.Info($"GAME: ({version}) [{request.GameId}]: {nickname} '{userId}' Left Room ({ip})");
            return Utils.CreateResult(new object(), 200);
        }

        private static async Task<IResult> PlayerAdd(HttpContext context, MvLDBContext database, IUserEntryLogService userEntryLogService, ILog log)
        {
            string json;
            using (StreamReader sr = new(context.Request.Body))
            {
                json = await sr.ReadToEndAsync();
            }

            AddPlayerRequest request = JsonConvert.DeserializeObject<AddPlayerRequest>(json);
            Dictionary<string, object> requestRuntimePlayer = request.RuntimePlayer;

            string ip = (string)request.AuthCookie["ip"];
            if (!Guid.TryParse(request.UserId, out Guid userId))
            {
                return Utils.CreateResult(new WebhookError
                {
                    Status = 400,
                    Error = "InvalidUserId",
                    Message = "Invalid UserId",
                }, 400);
            }

            UserEntry? entry = database.UserEntry.AsNoTracking().FirstOrDefault(ue => ue.UserId == userId);

            bool useColoredNickname = true;
            if (requestRuntimePlayer.TryGetValue("UseColoredNickname", out object coloredNickname))
            {
                useColoredNickname = coloredNickname as bool? ?? true;
            }
            requestRuntimePlayer["NicknameColor"] = (useColoredNickname ? entry?.NicknameColor : null) ?? "#FFFFFF";
            requestRuntimePlayer["UserId"] = userId.ToString();
            string playerNickname = "noname";
            if (requestRuntimePlayer.TryGetValue("PlayerNickname", out object playerNicknameObj))
            {
                playerNickname = playerNicknameObj as string ?? "noname";
            }
            requestRuntimePlayer["PlayerNickname"] = playerNickname;

            await userEntryLogService.SavePlayerInfoGuid(userId, Utils.IpToNumber(ip), playerNickname);

            AddPlayerResponse response = new AddPlayerResponse
            {
                RuntimePlayer = requestRuntimePlayer,
            };

            RoomVersions.TryGetValue(request.GameId, out string version);
            Nicknames[userId] = playerNickname;
            log.Info($"GAME: ({version}) [{request.GameId}]: {playerNickname} '{userId}' Added Player ({ip})");
            return Utils.CreateResult(response, 200);
        }

        private static async Task<IResult> GameClose(HttpContext context, ILog log)
        {
            string json;
            using (StreamReader sr = new(context.Request.Body))
            {
                json = await sr.ReadToEndAsync();
            }

            CloseGameRequest? request = JsonConvert.DeserializeObject<CloseGameRequest>(json);

            RoomVersions.TryGetValue(request.GameId, out string version);
            log.Info($"GAME: ({version}) [{request.GameId}]: Closed Room ({request.CloseReason})");

            RoomVersions.Remove(request.GameId);
            return Utils.CreateResult(new object(), 200);
        }
    }
}
