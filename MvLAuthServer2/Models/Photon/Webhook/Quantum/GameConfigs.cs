using System;
using System.Collections.Generic;

namespace MvLAuthServer2.Models.Photon.Webhook.Quantum
{
    [Serializable]
    public class GameConfigsRequest
    {
        public string AppId;
        public string GameId;
        public string UserId;
        public int ActorNr;
        public Dictionary<string, object> RuntimeConfig;
        public SessionConfig SessionConfig;
        public Dictionary<string, object> AuthCookie;
    }

    [Serializable]
    public class GameConfigsResponse
    {
        public Dictionary<string, object> RuntimeConfig;
        public SessionConfig SessionConfig;
    }
}