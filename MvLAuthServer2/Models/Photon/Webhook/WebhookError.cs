using System;

namespace MvLAuthServer2.Models.Photon.Webhook
{
    [Serializable]
    public class WebhookError
    {
        public int Status;
        public string? Error;
        public string? Message;
    }
}