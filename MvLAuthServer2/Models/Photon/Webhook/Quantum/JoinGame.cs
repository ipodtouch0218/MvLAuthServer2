using System;
using System.Collections.Generic;

namespace MvLAuthServer2.Models.Photon.Webhook.Quantum
{
    [Serializable]
    public class JoinGameRequest
    {
        public string AppId;
        public string GameId;
        public string UserId;
        public Dictionary<string, object> AuthCookie;
    }

    [Serializable]
    public class JoinGameResponse
    {
        public int MaxPlayerSlots;
    }
}