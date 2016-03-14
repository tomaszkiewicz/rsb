using System;
using System.Configuration;

namespace RSB.Transports.RabbitMQ.Settings
{
    public class RabbitMqTransportSettings : ConfigurationElement
    {
        public static RabbitMqTransportSettings FromConfigurationFile(string name = "")
        {
            var connections = ((RabbitMqTransportSettingsSection)ConfigurationManager.GetSection("rabbitMqTransport")).Connections;

            if (string.IsNullOrWhiteSpace(name) && connections.Count == 1)
                return connections[0];

            for (var i = 0; i < connections.Count; i++)
                if (connections[i].Name == name)
                    return connections[i];

            throw new InvalidOperationException("RabbitMqTransportSettings with specified name was not found.");
        }

        [ConfigurationProperty("name", DefaultValue = "", IsKey = true, IsRequired = false)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("heartbeat", IsRequired = false, DefaultValue = (ushort)5)]
        public ushort Heartbeat
        {
            get { return (ushort)this["heartbeat"]; }
            set { this["heartbeat"] = value; }
        }

        [ConfigurationProperty("hostname", IsRequired = true)]
        public string Hostname
        {
            get { return (string)this["hostname"]; }
            set { this["hostname"] = value; }
        }

        [ConfigurationProperty("username", IsRequired = false, DefaultValue = "guest")]
        public string Username
        {
            get { return (string)this["username"]; }
            set { this["username"] = value; }
        }

        [ConfigurationProperty("password", IsRequired = false, DefaultValue = "guest")]
        public string Password
        {
            get { return (string)this["password"]; }
            set { this["password"] = value; }
        }
        
        [ConfigurationProperty("virtualHost", IsRequired = false, DefaultValue = "/")]
        public string VirtualHost
        {
            get { return (string)this["virtualHost"]; }
            set { this["virtualHost"] = value; }
        }

        [ConfigurationProperty("useDurableExchanges", IsRequired = false, DefaultValue = true)]
        public bool UseDurableExchanges
        {
            get { return (bool)this["useDurableExchanges"]; }
            set { this["useDurableExchanges"] = value; }
        }
    }
}
