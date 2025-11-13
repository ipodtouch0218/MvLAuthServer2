using MvLAuthServer2.Models.Photon.Webhook.Quantum;
using System.Collections.Generic;

namespace MvLAuthServer2.Services
{
    public interface IConfigFileService
    {
        IReadOnlyCollection<string> VpnExceptions { get; }
        IReadOnlyDictionary<HashSet<string>, string> DiscordRoles { get; }
        SessionConfig SessionConfig { get; }
    }
}
