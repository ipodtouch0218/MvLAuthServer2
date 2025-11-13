using Newtonsoft.Json;

namespace MvLAuthServer2.Models.Photon.Webhook
{
    public class WebhookResult
    {
        [JsonProperty("ResultCode")]
        public WebhookResultCode ResultCode;
        [JsonProperty("Message")]
        public string? Message;

        public WebhookResult(WebhookResultCode code, string? message)
        {
            ResultCode = code;
            Message = message;
        }
    }

    public enum WebhookResultCode : int
    {
        Success = 0,
        Failed_Protocol = 1,
        Failed_Application = 2,
    }
}