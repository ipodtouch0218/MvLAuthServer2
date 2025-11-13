using log4net;
using MvLAuthServer2.Models.Photon.Webhook.Quantum;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MvLAuthServer2.Services.Impl
{
    public class ConfigFileService : IConfigFileService, IDisposable
    {
        private readonly ILog log;

        private readonly string _vpnExceptionsPath = Path.Combine(AppContext.BaseDirectory, "vpnexceptions.txt");
        private readonly string _discordRolesPath = Path.Combine(AppContext.BaseDirectory, "discordroles.txt");
        private readonly string _sessionConfigPath = Path.Combine(AppContext.BaseDirectory, "sessionconfig.json");

        private readonly FileSystemWatcher _vpnWatcher;
        private readonly FileSystemWatcher _rolesWatcher;
        private readonly FileSystemWatcher _sessionWatcher;

        private List<string> _vpnExceptions = new();
        private Dictionary<HashSet<string>, string> _discordRoles = new(HashSet<string>.CreateSetComparer());
        private SessionConfig _sessionConfig = new();

        private readonly object _lock = new();

        public ConfigFileService(ILog log)
        {
            this.log = log;

            // Initial load
            LoadVpnExceptions();
            LoadDiscordRoles();
            LoadSessionConfig();

            // Setup watchers
            _vpnWatcher = CreateWatcher(_vpnExceptionsPath, LoadVpnExceptions);
            _rolesWatcher = CreateWatcher(_discordRolesPath, LoadDiscordRoles);
            _sessionWatcher = CreateWatcher(_sessionConfigPath, LoadSessionConfig);
        }

        public IReadOnlyCollection<string> VpnExceptions => _vpnExceptions;
        public IReadOnlyDictionary<HashSet<string>, string> DiscordRoles => _discordRoles;
        public SessionConfig SessionConfig => _sessionConfig;

        private FileSystemWatcher CreateWatcher(string path, Action reloadAction)
        {
            var watcher = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path));
            watcher.Filter = Path.GetFileName(path);
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            watcher.Changed += (_, _) => reloadAction();
            watcher.Created += (_, _) => reloadAction();
            watcher.Renamed += (_, _) => reloadAction();
            watcher.EnableRaisingEvents = true;
            return watcher;
        }

        private void LoadVpnExceptions()
        {
            lock (_lock)
            {
                try
                {
                    _vpnExceptions = File.ReadAllLines(_vpnExceptionsPath)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();
                    log.Info($"Reloaded vpnexceptions.txt ({_vpnExceptions.Count} entries)");
                }
                catch
                {

                }
            }
        }

        private void LoadDiscordRoles()
        {
            lock (_lock)
            {
                _discordRoles.Clear();
                try
                {
                    foreach (string id in File.ReadAllLines(_discordRolesPath))
                    {
                        if (string.IsNullOrWhiteSpace(id))
                        {
                            continue;
                        }

                        string[] split = id.Split(';');
                        string[] roleSnowflakes = split[0].Split(',');
                        string color = split[1];

                        HashSet<string> newSet = new(roleSnowflakes.Length);
                        foreach (var snowflake in roleSnowflakes)
                        {
                            newSet.Add(snowflake);
                        }
                        _discordRoles[newSet] = color;
                    }
                    log.Info($"Reloaded discordroles.txt ({_discordRoles.Count} entries)");
                }
                catch
                {

                }
            }
        }

        private void LoadSessionConfig()
        {
            lock (_lock)
            {
                try
                {
                    var json = File.ReadAllText(_sessionConfigPath);
                    _sessionConfig = JsonConvert.DeserializeObject<SessionConfig>(json) ?? new SessionConfig();
                    log.Info("Reloaded sessionconfig.json");
                }
                catch
                {

                }
            }
        }

        public void Dispose()
        {
            _vpnWatcher?.Dispose();
            _rolesWatcher?.Dispose();
            _sessionWatcher?.Dispose();
        }
    }
}
