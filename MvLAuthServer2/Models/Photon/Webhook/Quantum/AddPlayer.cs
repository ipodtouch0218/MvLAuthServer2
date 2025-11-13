using System;
using System.Collections.Generic;

namespace MvLAuthServer2.Models.Photon.Webhook.Quantum
{
    [Serializable]
    public class AddPlayerRequest
    {
        public string AppId;
        public string GameId;
        public string UserId;
        public int PlayerSlot;
        public Dictionary<string, object> RuntimePlayer;
        public Dictionary<string, object> AuthCookie;
    }

    [Serializable]
    public class AddPlayerResponse
    {
        public Dictionary<string, object> RuntimePlayer;
    }
}