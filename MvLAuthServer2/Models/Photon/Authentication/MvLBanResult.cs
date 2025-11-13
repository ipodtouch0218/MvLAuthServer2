using MvLAuthServer2.Models.Database;
using System;

namespace MvLAuthServer2.Models.Photon.Authentication
{
    [Serializable]
    public class MvLBanResult
    {
        public int Id;
        public string? Message;
        public long? Expiration;

        public MvLBanResult(Ban ban)
        {
            Id = ban.Id;
            Message = ban.Message;
            Expiration = ban.Expiration;
        }
    }
}