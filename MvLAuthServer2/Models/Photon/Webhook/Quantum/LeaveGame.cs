using System;
using System.Collections.Generic;

namespace MvLAuthServer2.Models.Photon.Webhook.Quantum
{
    [Serializable]
    public class LeaveGameRequest
    {
        public string AppId;
        public string GameId;
        public string UserId;
        public int ActorNr;
        public Dictionary<string, object> AuthCookie;
        public bool IsInactive;
    }
}