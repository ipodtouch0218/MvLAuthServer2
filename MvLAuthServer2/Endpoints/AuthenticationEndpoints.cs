using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MvLAuthServer2.Models.Database;
using MvLAuthServer2.Models.Photon.Authentication;
using MvLAuthServer2.Services;
using Newtonsoft.Json;
using ProxyCheckUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MvLAuthServer2.Endpoints
{
    public static class AuthenticationEndpoints
    {
        private static readonly Dictionary<string, long> LastLog = new();

        private static async Task<IResult> Init(HttpContext context, [FromQuery] string? userid, [FromQuery] string? token, [FromQuery] int? args, MvLDBContext database, IEncryptionService encryptionService, ILog log)
        {
            try
            {
                // Parse to GUID
                Guid? useridGuid = null;
                if (userid != null && Guid.TryParse(userid, out Guid guid))
                {
                    useridGuid = guid;
                }

                // Get user IP address
                string? ip = context.GetServerVariable("HTTP_X_FORWARDED_FOR");
                ip ??= context.GetServerVariable("REMOTE_ADDR");
                ip ??= context.Connection.RemoteIpAddress?.MapToIPv4().ToString();
                ip = ip?.Split(':')[0];

                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                try
                {
                    uint? numericIp = Utils.IpToNumber(ip);

                    var ban = await database.Ban.AsNoTracking()
                        .Where(ue => ue.UserId == useridGuid || (ue.IpRangeMin <= numericIp && numericIp <= ue.IpRangeMax))
                        .Where(ue => ue.Expiration == null || ue.Expiration > now)
                        .FirstOrDefaultAsync();
                    
                    if (ban != null)
                    {
                        if (!LastLog.TryGetValue(ip, out long timestamp)
                            || now - timestamp > 4)
                        {
                            log.Info($"AUTH: '{useridGuid}' ({ip}) tried to authenticate, but is banned! ({ban.Id})");
                            LastLog[ip] = now;
                        }
                        return Utils.CreateResult(new MvLBanResult(ban), 403);
                    }
                }
                catch (Exception e)
                {
                    log.Info(e);
                }

                // Create response
                MvLAuthRequest response = new()
                {
                    UserId = useridGuid,
                    Token = token,
                    IpAddress = ip,
                    Args = args,
                    Timestamp = now
                };
                string json = JsonConvert.SerializeObject(response);

                // Encrypt response
                string base64 = encryptionService.EncryptToBase64(json);

                // Return encrypted response
                return Results.Ok(base64);
            }
            catch (Exception e)
            {
                log.Error(e);
                throw;
            }
        }

        private static async Task<IResult> Game(HttpContext context, [FromQuery] string? key, [FromQuery] string? data, MvLDBContext database, IEncryptionService encryptionService, IConfigFileService configFileService, IConfiguration config, IUserEntryLogService userEntryLogService, ILog log)
        {
            try
            {
                // Check that we have the correct secret
                string authSecret = config["Secrets:Authentication"];
                if (key != authSecret)
                {
                    log.Info($"AUTH: A user tried to authenticate, but did not supply the correct key!");
                    return Utils.CreateResult(new AuthResult(AuthenticationResultCode.Failed_Parameters, "Bad Request (Invalid Request Origin)", null), 400);
                }

                // Check that we have data at all
                if (data == null)
                {
                    log.Info($"AUTH: A user tried to authenticate, but did not send any data!");
                    return Utils.CreateResult(new AuthResult(AuthenticationResultCode.Failed_Parameters, "Bad Request (No Data)", null), 400);
                }

                MvLAuthRequest? request;
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                string? json = null;
                try
                {
                    // Decrypt data from base64
                    data = data.Replace("\"", "");
                    json = encryptionService.DecryptFromBase64(data);
                    request = JsonConvert.DeserializeObject<MvLAuthRequest>(json);

                    if (request == null)
                    {
                        throw new Exception();
                    }
                }
                catch (Exception)
                {
                    /*
                    // Return error if we fail to decrypt the data
                    log.Info($"AUTH: A user tried to authenticate, but sent malformed data! ({data})");
                    return Utils.CreateResult(new AuthResult(AuthenticationResultCode.Failed_Parameters, "Bad Request (Invalid Data)", null), 400);
                    */
                    request = new()
                    {
                        Timestamp = now
                    };
                }

                // *** Request has valid data

                Guid? userId = request.UserId;
                string? token = request.Token;
                string? ip = request.IpAddress?.Split(',')[0]; // Sometimes commas get introduced, not sure why.

                // Check that the request is still in-date
                int authRequestValidTime = config.GetValue<int>("AuthRequestValidTime");
                if (now - request.Timestamp > authRequestValidTime)
                {
                    log.Info($"AUTH: '{userId}' ({ip}) tried to authenticate, but their request was expired! (req: {request.Timestamp}, now: {now})");
                    return Utils.CreateResult(new AuthResult(AuthenticationResultCode.Failed_Parameters, "Bad Request (Request Expired)", null), 400);
                }

                // Check that they aren't banned (IP)
                uint? ipAsNumberNullable = Utils.IpToNumber(ip);
                if (ipAsNumberNullable is uint ipAsNumber)
                {
                    var ban = await database.Ban.AsNoTracking()
                        .Where(b => b.IpRangeMin <= ipAsNumber && ipAsNumber <= b.IpRangeMax)
                        .Where(b => b.Expiration == null || b.Expiration > now)
                        .FirstOrDefaultAsync();

                    if (ban != null)
                    {
                        log.Info($"AUTH: '{userId}' ({ip}) tried to authenticate, but is IP banned! ({ban.Id})");
                        return Utils.CreateResult(new AuthResult(AuthenticationResultCode.Rejected, "Rejected (User is Banned from MvL)", null), 403);
                    }
                }

                var vpnExceptions = configFileService.VpnExceptions;
                UserEntry? userEntry = null;
                bool generateNewUserId = true;
                bool includeToken = false;
                if (userId != null)
                {
                    // *** They already have a userId
                    // Check that they aren't banned (ID)
                    var ban = await database.Ban.AsNoTracking()
                        .Where(b => b.UserId == userId.Value)
                        .Where(b => b.Expiration == null || b.Expiration > now)
                        .FirstOrDefaultAsync();

                    if (ban != null)
                    {
                        log.Info($"AUTH: '{userId}' ({ip}) tried to authenticate, but is UserID banned!");
                        return Utils.CreateResult(new AuthResult(AuthenticationResultCode.Rejected, "Rejected (User is Banned from MvL)", null), 403);
                    }

                    userEntry = database.UserEntry.FirstOrDefault(ue => ue.UserId == userId.Value);
                        
                    if (userEntry != null)
                    {
                        // Check that the token is
                        string hashedToken = Utils.HashSHA256ToHexString(token);
                        if (hashedToken == userEntry.HashedToken)
                        {
                            if (!vpnExceptions.Contains(userId.Value.ToString()) && !vpnExceptions.Any(exceptionIp => Utils.IsIpInRange(ip, exceptionIp)))
                            {
                                // Check for VPNs
                                try
                                {
                                    ProxyCheckResult result = await Utils.IsIpProxy(ip);
                                    IPAddress address = IPAddress.Parse(ip);
                                    if (result.Status == StatusResult.OK && result.Results[address].IsProxy)
                                    {
                                        log.Info($"AUTH: '{userId}' ({ip}) tried to authenticate, but is using a VPN!");
                                        return Utils.CreateResult(new AuthResult(AuthenticationResultCode.Rejected, "Rejected (VPNs are Banned from MvL)", null), 403);
                                    }
                                }
                                catch (Exception e)
                                {
                                    log.Error($"ProxyCheck Exception! '{ip}' may not be a valid IP. Stacktrace: \n{e}");
                                }
                            }

                            // Correct token was supplied, so don't generate a new userId/token pair.
                            generateNewUserId = false;

                            int duplicateTokenCount = await database.UserEntry.Where(ue => ue.HashedToken == hashedToken).CountAsync();
                            if (duplicateTokenCount > 1)
                            {
                                // Bug fix; some users had duplicate tokens. return a new one.
                                userEntry.HashedToken = Utils.HashSHA256ToHexString(token = Utils.CreateToken());
                                includeToken = true;
                            }

                            userEntry.LastSeen = now;
                            userEntry.TimesConnected++;
                            _ = await database.SaveChangesAsync();
                        }
                    }
                }

                // *** The user has successfully authenticated.
                if (generateNewUserId)
                {
                    // Check for VPNs
                    if (!vpnExceptions.Any(exceptionIp => Utils.IsIpInRange(ip, exceptionIp)))
                    {
                        try
                        {
                            ProxyCheckResult result = await Utils.IsIpProxy(ip);
                            IPAddress address = IPAddress.Parse(ip);
                            if (result.Status == StatusResult.OK && result.Results[address].IsProxy)
                            {
                                log.Info($"AUTH: '{userId}' ({ip}) tried to authenticate, but is using a VPN!");
                                return Utils.CreateResult(new AuthResult(AuthenticationResultCode.Rejected, "Rejected (VPNs are Banned from MvL)", null), 403);
                            }
                        }
                        catch
                        {
                            log.Error($"ProxyCheck Exception! '{ip}' may not be a valid IP.");
                        }
                    }

                    // Create a new userId and token.
                    userId = Guid.NewGuid();
                    if (string.IsNullOrEmpty(token))
                    {
                        token = Utils.CreateToken();
                        includeToken = true;
                    }

                    userEntry = new()
                    {
                        UserId = userId.Value,
                        HashedToken = Utils.HashSHA256ToHexString(token)!,
                        FirstPlayed = now,
                        LastSeen = now,
                        TimesConnected = 1,
                    };

                    await database.UserEntry.AddAsync(userEntry);
                    _ = await database.SaveChangesAsync();
                }

                if (!includeToken)
                {
                    token = null;
                }

                // And add this IP to the list...
                await userEntryLogService.SavePlayerInfo(userEntry.Id, Utils.IpToNumber(ip), null);

                // Log
                var nicknames = await database.NicknameLog.AsNoTracking()
                    .Where(log => log.UserEntryId == userEntry.Id)
                    .OrderByDescending(log => log.LastUsed)
                    .Take(11)
                    .Select(nl => nl.Nickname)
                    .ToListAsync();

                string nicknamesStr = string.Join(',', nicknames.Take(10));
                if (nicknames.Count > 10)
                {
                    nicknamesStr += $"... and {nicknames.Count - 10} more";
                }
                log.Info($"AUTH: '{userId}' ({ip}) authenticated! Nicknames: [{nicknamesStr}]");

                // Save the IP for future logging purposes.
                PunWebhookEndpoints.CurrentIPs[userId!.Value] = ip;

                Dictionary<string, object> authCookie = new();
                authCookie["ip"] = ip;
                return Utils.CreateResult(new AuthResult(AuthenticationResultCode.Success, null, userId!.Value, new AuthResultData(token), authCookie), 200);
            }
            catch (Exception e)
            {
                log.Error(e);
                throw;
            }
        }

        public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var authenticationGroup = endpoints.MapGroup("/auth");

            authenticationGroup.MapGet("/init", Init);
            authenticationGroup.MapGet("/game", Game);

            return endpoints;
        }
    }
}
