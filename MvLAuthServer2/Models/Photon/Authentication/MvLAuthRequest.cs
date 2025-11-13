using System;

namespace MvLAuthServer2.Models.Photon.Authentication
{
    public class MvLAuthRequest
    {
        public Guid? UserId;
        public string? Token;
        public string IpAddress;
        public int? Args;
        public long Timestamp;
    }
}