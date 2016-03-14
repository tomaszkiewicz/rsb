using System.Configuration;

namespace RSB.Transports.RabbitMQ.Settings
{
    public class RabbitMqTransportSettingsSection : ConfigurationSection
    {
        [ConfigurationProperty("connections")]
        public RabbitMqTransportSettingsCollection Connections
        {
            get { return (RabbitMqTransportSettingsCollection)this["connections"]; }
            set { this["connections"] = value; }
        }
    }
}