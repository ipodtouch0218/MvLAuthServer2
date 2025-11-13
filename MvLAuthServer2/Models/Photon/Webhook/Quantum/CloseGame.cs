using System;

namespace MvLAuthServer2.Models.Photon.Webhook.Quantum
{
    [Serializable]
    public class CloseGameRequest
    {
        public string AppId;
        public string GameId;
        public CloseReason CloseReason;
    }

    public enum CloseReason : int
    {
        Ok = 0,
        FailedOnCreate = 1,
    }
}