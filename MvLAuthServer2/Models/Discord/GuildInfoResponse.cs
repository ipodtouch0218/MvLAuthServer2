using Newtonsoft.Json;
using System;

namespace MvLAuthServer2.Models.Discord
{
    [Serializable]
    class GuildInfoResponse
    {
        // Only care about what we want...
        [JsonProperty("roles")]
        public string[] Roles;
    }
}