using Newtonsoft.Json;
using System;

namespace MvLAuthServer2.Models.Discord
{
    [Serializable]
    class AccessTokenResponse
    {
        [JsonProperty("access_token")]
        public string? AccessToken;
        [JsonProperty("token_type")]
        public string? TokenType;
        [JsonProperty("expires_in")]
        public long ExpiresIn;
        [JsonProperty("refresh_token")]
        public string? RefreshToken;
        [JsonProperty("scope")]
        public string? Scope;
    }
}