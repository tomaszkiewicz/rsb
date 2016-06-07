using System;
using System.Configuration;

namespace RSB.Transports.RabbitMQ.Settings
{
    public class RabbitMqTransportSettingsCollection : ConfigurationElementCollection
    {
        internal const string PropertyName = "connection";

        public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.BasicMapAlternate;

        protected override string ElementName => PropertyName;

        protected override bool IsElementName(string elementName)
        {
            return elementName.Equals(PropertyName, StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool IsReadOnly()
        {
            return false;
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new RabbitMqTransportSettings();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((RabbitMqTransportSettings)(element)).Name;
        }

        public RabbitMqTransportSettings this[int idx] => (RabbitMqTransportSettings)BaseGet(idx);
    }
}