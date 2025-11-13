using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MvLAuthServer2.Models.Database;
using MvLAuthServer2.Models.Discord;
using MvLAuthServer2.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace MvLAuthServer2.Endpoints
{
    public static class DiscordEndpoints
    {
        private static HttpClient Client;
        private static Dictionary<Guid, Guid> DiscordSecrets = new();

        public static IEndpointRouteBuilder MapDiscordEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/discord", Discord);
            return endpoints;
        }

        private static async Task<IResult> Discord(HttpContext context, [FromQuery] string code, [FromQuery] string state, IConfigFileService configFileService, IConfiguration configuration, ILog log, MvLDBContext database)
        {
            if (Client == null)
            {
                Client = new();
                Client.DefaultRequestHeaders.Add("User-Agent", configuration["Discord:UserAgent"]);
            }

            string hash = Utils.HashSHA256ToHexString(state);
            UserEntry? userEntry = await database.UserEntry.FirstOrDefaultAsync(ue => ue.HashedToken == hash);
            if (userEntry == null)
            {
                return Results.BadRequest();
            }

            HttpRequestMessage tokenGenerateMethod = new(HttpMethod.Post, "https://discord.com/api/oauth2/token");
            Dictionary<string, string> encodedParams = new();
            encodedParams["client_id"] = configuration["Discord:ClientId"];
            encodedParams["client_secret"] = configuration["Discord:ApiSecret"];
            encodedParams["grant_type"] = "authorization_code";
            encodedParams["code"] = code;
            encodedParams["redirect_uri"] = "https://mariovsluigi.azurewebsites.net/discord";
            tokenGenerateMethod.Content = new FormUrlEncodedContent(encodedParams);

            var response = await Client.SendAsync(tokenGenerateMethod);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Guid rand = Guid.NewGuid();
                log.Error($"Discord Error {rand}\n  Request: {await tokenGenerateMethod.Content.ReadAsStringAsync()}\n  Response: {await response.Content.ReadAsStringAsync()}");
                return Results.Text($"An error occurred while trying to link your Discord account!\nThis is not your fault. Please report this in #technical-support and ping @ipodtouch0218 with a screenshot of the following code: {rand}");
            }

            try
            {
                // Make the API request.
                string json;
                using (Stream s = response.Content.ReadAsStream())
                {
                    using StreamReader sr = new(s);
                    json = await sr.ReadToEndAsync();
                }

                AccessTokenResponse? responseFromDiscord = JsonConvert.DeserializeObject<AccessTokenResponse>(json, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore
                });
                HttpRequestMessage requestMessage = new(HttpMethod.Get, "https://discord.com/api/v10/users/@me/guilds/956396409731551263/member");
                requestMessage.Headers.Authorization = new(responseFromDiscord.TokenType, responseFromDiscord.AccessToken);

                response = await Client.SendAsync(requestMessage);
                using (Stream s = response.Content.ReadAsStream())
                {
                    using StreamReader sr = new(s);
                    json = await sr.ReadToEndAsync();
                }

                GuildInfoResponse? rolesResponse = JsonConvert.DeserializeObject<GuildInfoResponse>(json);

                // Find the color
                if (rolesResponse.Roles != null && rolesResponse.Roles.Length > 0)
                {
                    foreach ((var rolesForThisColor, string color) in configFileService.DiscordRoles)
                    {
                        try
                        {
                            if (!rolesForThisColor.Except(rolesResponse.Roles).Any())
                            {
                                userEntry.NicknameColor = color;
                                await database.SaveChangesAsync();

                                return Results.Text($"Your role color has been assigned ({color}). You may close this window and return to MvL.");
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error(rolesForThisColor  + " - " + color + "\n" + e);
                            throw;
                        }
                    }
                }

                userEntry.NicknameColor = null;
                database.UserEntry.Update(userEntry);
                await database.SaveChangesAsync();
                return Results.Text("You do not have a role color available. You may close this window and return to MvL.");
            }
            catch (Exception e)
            {
                log.Error("Discord API error!\n" + e);
                return Results.StatusCode(500);
            }
        }
    }
}
