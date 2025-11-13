using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MvLAuthServer2.Models.Database;
using MvLAuthServer2.Models.Photon.Webhook;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MvLAuthServer2.Services.Impl
{
    public class BanService(ILog log, MvLDBContext database) : IBanService
    {
        public async Task<IResult?> CheckForBansAsync(string action, Guid userId, string ip)
        {
            // Check that they aren't banned (ID)
            Ban? ban = await FindUserIdBan(userId);
            if (ban != null)
            {
                log.Info($"GAME: '{userId}' ({ip}) tried to {action}, but is UserID banned! ({ban.Id})");
                return Utils.CreateResult(new WebhookError
                {
                    Status = 400,
                    Error = "PlayerBanned",
                    Message = "User is banned from MvL"
                }, 400);
            }

            // Check that they aren't banned (IP)
            ban = await FindIpBan(ip);
            if (ban != null)
            {
                log.Info($"GAME: '{userId}' ({ip}) tried to {action}, but is IP banned! ({ban.Id})");
                return Utils.CreateResult(new WebhookError
                {
                    Status = 400,
                    Error = "PlayerBanned",
                    Message = "User is banned from MvL"
                }, 400);
            }

            return null;
        }

        private async Task<Ban?> FindUserIdBan(Guid userId)
        {
            // Check that they aren't banned (ID)
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return await database.Ban.AsNoTracking()
                .Where(b => b.UserId == userId)
                .Where(b => b.Expiration == null || b.Expiration > now)
                .FirstOrDefaultAsync();
        }

        private async Task<Ban?> FindIpBan(string ip)
        {
            uint? ipAsNumberNullable = Utils.IpToNumber(ip);
            if (ipAsNumberNullable is not uint ipAsNumber)
            {
                return null;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return await database.Ban.AsNoTracking()
                .Where(b => b.IpRangeMin <= ipAsNumber && ipAsNumber <= b.IpRangeMax)
                .Where(b => b.Expiration == null || b.Expiration > now)
                .FirstOrDefaultAsync();
        }
    }
}
