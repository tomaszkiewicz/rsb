using System;

namespace RSB.Diagnostics.Contracts
{
    public class ComponentInfoMessage
    {
        public string ModuleName { get; set; }
        public string InstanceName { get; set; }
        public string RunGuid { get; set; }
        public DateTime RunTime { get; set; }
        public string[] Components { get; set; }
        public string MachineName { get; set; }
        public DateTime BuildTime { get; set; }
        public bool Interactive { get; set; }
        public string Username { get; set; }
        public string DomainName { get; set; }
    }
}
