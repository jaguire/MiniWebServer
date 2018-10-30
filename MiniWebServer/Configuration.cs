using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using Newtonsoft.Json;

namespace MiniWebServer
{
    public class Configuration
    {
        private static readonly string ConfigFile = $"{Environment.CurrentDirectory}\\MiniWebServer.config";

        public string WebRoot { get; set; }
        public string Domain { get; set; }
        public int Port { get; set; }

        public static Configuration Create()
        {
            return File.Exists(ConfigFile) ? Read() : Write();
        }

        private static Configuration Read()
        {
            return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(ConfigFile));
        }

        private static Configuration Write()
        {
            var config = new Configuration
            {
                WebRoot = Environment.CurrentDirectory,
                Domain = "localhost",
                Port = GetUnusedPort()
            };
            File.WriteAllText(ConfigFile, JsonConvert.SerializeObject(config, Formatting.Indented));
            return config;
        }

        private static int GetUnusedPort()
        {
            var activePorts = IPGlobalProperties.GetIPGlobalProperties()
                                                .GetActiveTcpConnections()
                                                .Select(x => x.LocalEndPoint.Port)
                                                .ToList();
            const int min = 49152;
            const int max = 65535;
            var random = new Random();
            var port = random.Next(min, max);
            while (activePorts.Contains(port))
                port = random.Next(min, max);
            return port;
        }
    }
}