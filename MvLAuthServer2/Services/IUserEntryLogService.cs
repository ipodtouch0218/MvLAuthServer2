using System;
using System.Threading.Tasks;

namespace MvLAuthServer2.Services
{
    public interface IUserEntryLogService
    {
        Task SavePlayerInfoGuid(Guid userId, uint? ipNullable, string? nickname);
        Task SavePlayerInfo(int userEntryId, uint? ipNullable, string? nickname);
    }
}
