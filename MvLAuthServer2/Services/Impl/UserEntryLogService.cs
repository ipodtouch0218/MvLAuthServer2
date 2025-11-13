using log4net;
using Microsoft.EntityFrameworkCore;
using MvLAuthServer2.Models.Database;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MvLAuthServer2.Services.Impl
{
    public class UserEntryLogService(MvLDBContext database, ILog log) : IUserEntryLogService
    {
        public async Task SavePlayerInfo(int userEntryId, uint? ipNullable, string? nickname)
        {
            if (!string.IsNullOrEmpty(nickname))
            {
                try
                {
                    NicknameLog? nicknameLog = await database.NicknameLog
                        .Where(nl => nl.UserEntryId == userEntryId && nl.Nickname == nickname)
                        .FirstOrDefaultAsync();
                    
                    if (nicknameLog == null)
                    {
                        nicknameLog = new NicknameLog
                        {
                            UserEntryId = userEntryId,
                            Nickname = nickname,
                        };
                        database.NicknameLog.Add(nicknameLog);
                    }
                    nicknameLog.LastUsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    nicknameLog.TimesUsed++;
                    
                }
                catch (Exception e)
                {
                    log.Info(e);
                }
            }

            if (ipNullable is uint ip)
            {
                // And add this IP to the list...
                try
                {
                    IPLog? ipLog = await database.IPLog
                        .Where(il => il.UserEntryId == userEntryId && il.IPAddress == ipNullable.Value)
                        .FirstOrDefaultAsync(); 
                    
                    if (ipLog == null)
                    {
                        ipLog = new IPLog
                        {
                            UserEntryId = userEntryId,
                            IPAddress = ipNullable.Value,
                        };
                        database.IPLog.Add(ipLog);
                    }
                    ipLog.LastUsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    ipLog.TimesUsed++;
                }
                catch (Exception e)
                {
                    log.Info(e);
                }
            }

            database.Database.ExecuteSqlRaw("PRAGMA busy_timeout=5000;");
            await database.SaveChangesAsync();
        }

        public async Task SavePlayerInfoGuid(Guid userId, uint? ip, string? nickname)
        {
            try
            {
                int id = await database.UserEntry.AsNoTracking()
                    .Where(ue => ue.UserId == userId)
                    .Select(ue => ue.Id)
                    .FirstAsync();

                await SavePlayerInfo(id, ip, nickname);
            }
            catch
            {

            }
        }
    }
}
