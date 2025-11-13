using System;
using System.Collections.Generic;

namespace MvLAuthServer2.Models.Photon.Webhook.Quantum
{
    [Serializable]
    public class CreateGameRequest
    {
        public string AppId;
        public string AppVersion;
        public string Region;
        public string Cloud;
        public string UserId;
        public Dictionary<string, object> AuthCookie;
        public string RoomId;
        public string GameId;
        public EnterRoomParams EnterRoomParams;
    }

    [Serializable]
    public class EnterRoomParams
    {
        public RoomOptions RoomOptions;
        public string[] ExpectedUsers;
    }

    [Serializable]
    public class CreateGameResponse
    {
        public RoomOptions? RoomOptions;
        public string[]? ExpectedUsers;
    }

    [Serializable]
    public class RoomOptions
    {
        public bool? IsVisible;
        public bool? IsOpen;
        public byte? MaxPlayers;
        public int? PlayerTtl;
        public int? EmptyRoomTtl;
        public Dictionary<string, object>? CustomRoomProperties;
        public string[]? CustomRoomPropertiesForLobby;
        public bool? SuppressRoomEvents;
        public bool? SuppressPlayerInfo;
        public bool? PublishUserId;
        public bool? DeleteNullProperties;
    }
}