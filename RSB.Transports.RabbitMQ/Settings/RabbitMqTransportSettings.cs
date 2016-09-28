using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace RSB.Transports.RabbitMQ.Settings
{
    public class RabbitMqTransportSettings
    {
        public static RabbitMqTransportSettings FromConfigurationFile(string connectionName = "")
        {
            if(!string.IsNullOrWhiteSpace(connectionName))
                throw new NotImplementedException("Not implemented yet in .NET Core version.");

            var settings = new RabbitMqTransportSettings();

            var builder = new ConfigurationBuilder();

            builder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                { "hostname", "localhost" },
                { "username", "guest" },
                { "password", "guest" },
                { "virtualHost", "/" },
                { "useDurableExchanges", "true" },
                { "heartbeat", "30" },
                { "name", "default" }
            });

            builder.AddJsonFile("rsb.json");

            var config = builder.Build();

            settings.Hostname = config["hostname"];
            settings.Name = config["name"];
            settings.Username = config["username"];
            settings.Password = config["password"];
            settings.VirtualHost = config["virtualHost"];
            settings.UseDurableExchanges = bool.Parse(config["useDurableExchanges"]);
            settings.Heartbeat = ushort.Parse(config["heartbeat"]);

            return settings;
        }

        public string Name { get; set; }
        public ushort Heartbeat { get; set; }
        public string Hostname { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string VirtualHost { get; set; }
        public bool UseDurableExchanges { get; set; }
    }
}