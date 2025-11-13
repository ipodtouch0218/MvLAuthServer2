using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.OpenSsl;
using ProxyCheckUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MvLAuthServer2
{
    public static class Utils {


        public static string ToBase64(byte[] input)
        {
            string base64 = Convert.ToBase64String(input);
            base64 = base64.Replace('+', '-').Replace('/', '_').Replace("=", "");

            return base64;
        }

        public static byte[] FromBase64(string input)
        {
            input = input.Replace("-", "+").Replace("_", "/");
            int paddings = input.Length % 4;
            if (paddings > 0)
            {
                input += new string('=', 4 - paddings);
            }

            return Convert.FromBase64String(input);
        }

        public static string FormatTimeSpan(TimeSpan span)
        {
            StringBuilder builder = new();

            if (span.TotalDays > 0)
            {
                builder.Append("in ");
            }

            const int precision = 2;
            int count = 0;
            if (((int) span.TotalDays != 0 || count > 0) && count < precision)
            {
                builder.Append(Math.Abs((int) span.TotalDays)).Append("d ");
                count++;
            }
            if ((span.Hours != 0 || count > 0) && count < precision)
            {
                builder.Append(Math.Abs(span.Hours)).Append("h ");
                count++;
            }
            if ((span.Minutes != 0 || count > 0) && count < precision)
            {
                builder.Append(Math.Abs(span.Minutes)).Append("m ");
                count++;
            }
            if ((span.Seconds != 0 || count > 0) && count < precision)
            {
                builder.Append(Math.Abs(span.Seconds)).Append("s ");
                count++;
            }

            if (span.TotalDays <= 0)
            {
                builder.Append("ago");
            }

            return builder.ToString().TrimEnd();
        }

        public static string NumberToIp(uint ipAddress)
        {
            StringBuilder builder = new();
            for (int i = 3; i >= 0; i--)
            {
                uint b = ipAddress >> (i * 8);
                b &= 255;
                builder.Append(b);

                if (i != 0)
                {
                    builder.Append('.');
                }
            }
            return builder.ToString();
        }

        public static uint? IpToNumber(string? ipAddress)
        {
            if (ipAddress == null)
            {
                return null;
            }

            uint result = 0;
            string[] ipAddressInArray = ipAddress.Split(".");

            for (int i = 3; i >= 0; i--)
            {
                if (!byte.TryParse(ipAddressInArray[3 - i], out byte component))
                {
                    return null;
                }
                result |= (uint) (component << (i * 8));
            }

            return result;
        }

        public static bool IsIpInRange(string ip, string subnet)
        {
            if (ip == null)
            {
                return false;
            }

            if (!subnet.Contains('/'))
            {
                return ip == subnet;
            }

            try
            {
                string[] split = subnet.Split('/');
                long range = IpToNumber(split[0])!.Value;
                int maskBits = 32 - Convert.ToInt32(split[1]);

                long mask = (range >> maskBits) << maskBits;
                long ipNumber = IpToNumber(ip)!.Value;

                long maskedIp = (ipNumber >> maskBits) << maskBits;
                return maskedIp == mask;
            }
            catch { }
            return false;
        }

        public static string? HashSHA256ToBase64(string? input)
        {
            if (input == null)
            {
                return null;
            }

            byte[] result = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return ToBase64(result);
        }

        public static string? HashSHA256ToHexString(string? input)
        {
            if (input == null)
            {
                return null;
            }

            byte[] result = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            StringBuilder builder = new();
            foreach (byte b in result)
            {
                string hex = Convert.ToString(b, 16);
                if (hex.Length <= 1)
                {
                    hex = '0' + hex;
                }

                builder.Append(hex);
            }
            return builder.ToString();
        }

        private static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();
        private static readonly byte[] tokenBuffer = new byte[64];
        public static string CreateToken()
        {
            rng.GetBytes(tokenBuffer);
            return ToBase64(tokenBuffer);
        }

        public class ProxyCache : IProxyChceckCacheProvider
        {
            Dictionary<IPAddress, ProxyCheckResult.IpResult> cache = new();
            string filename;

            public ProxyCache(string filename)
            {
                this.filename = filename;
                try
                {
                    string result = File.ReadAllText(filename);
                    cache = JsonConvert.DeserializeObject<Dictionary<IPAddress, ProxyCheckResult.IpResult>>(result);
                }
                catch
                {
                    cache = new();
                }
            }


            public ProxyCheckResult.IpResult GetCacheRecord(IPAddress ip, ProxyCheckRequestOptions options)
            {
                if (cache.TryGetValue(ip, out var value))
                {
                    return value;
                }
                return null;
            }

            public IDictionary<IPAddress, ProxyCheckResult.IpResult> GetCacheRecords(IPAddress[] ipAddress, ProxyCheckRequestOptions options)
            {
                Dictionary<IPAddress, ProxyCheckResult.IpResult> results = new();
                foreach (IPAddress address in ipAddress)
                {
                    if (cache.TryGetValue(address, out var value))
                    {
                        results[address] = value;
                    }
                }
                return results;
            }

            public void SetCacheRecord(IDictionary<IPAddress, ProxyCheckResult.IpResult> results, ProxyCheckRequestOptions options)
            {
                foreach ((var ip, var result) in results)
                {
                    cache[ip] = result;
                }
                File.WriteAllText(filename, JsonConvert.SerializeObject(cache));
            }
        }

        public static IResult CreateResult(object result, int code = 200)
        {
            return Results.Text(JsonConvert.SerializeObject(result, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
            }), "application/json", statusCode: code);
        }


        public static ProxyCache cache = new("vpncache.json");
        private static ProxyCheck? checker;
        public static Task<ProxyCheckResult> IsIpProxy(string ipAddress)
        {
            checker ??= new("7x1o34-50036z-7t3100-420552", cache)
                {
                    IncludeVPN = true
                };

            return checker.QueryAsync(ipAddress);
        }
    }
}
