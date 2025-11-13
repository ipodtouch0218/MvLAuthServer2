using System;
using System.Collections.Generic;

namespace MvLAuthServer2.Models.Photon.Authentication
{
    public class AuthResult
    {
        public AuthenticationResultCode ResultCode;
        public string? Message;
        public Guid? UserID;
        public AuthResultData? Data;
        public Dictionary<string, object>? AuthCookie;

        public AuthResult(AuthenticationResultCode code, string? message, Guid? userId, AuthResultData? data = null, Dictionary<string, object>? authCookie = null)
        {
            ResultCode = code;
            Message = message;
            UserID = userId;
            Data = data;
            AuthCookie = authCookie;
        }
    }

    public class AuthResultData
    {
        public string? Token;

        public AuthResultData(string? token)
        {
            Token = token;
        }
    }

    public enum AuthenticationResultCode : int
    {
        Incomplete = 0,
        Success = 1,
        Failed_Credentials = 2,
        Failed_Parameters = 3,
        Rejected = 4,
    }
}