using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace MvLAuthServer2.Services
{
    public interface IBanService
    {
        Task<IResult?> CheckForBansAsync(string action, Guid userId, string ip);
    }
}
