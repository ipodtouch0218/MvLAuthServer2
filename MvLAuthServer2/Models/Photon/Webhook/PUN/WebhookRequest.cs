using System.Collections.Generic;

namespace MvLAuthServer2.Models.Photon.Webhook.PUN
{
    public class WebhookData
    {
        public string? Nickname;
        public string? UserId;
        public string? GameId;
        public string? Region;
        public string? AppVersion;
        public Dictionary<string, object> AuthCookie;
    }
}