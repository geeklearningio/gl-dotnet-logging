using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GeekLearning.Logging.Azure
{
    public class AzureLoggerSettings
    {
        public AzureLoggerSettings()
        {

        }

        public AzureLoggerSettings(IConfigurationSection section)
        {
            this.ConnectionString = section["ConnectionString"];
            this.Table = section["Table"];
            foreach (var switchConfig in section.GetSection("LogLevel").GetChildren())
            {
                Switches.Add(switchConfig.Key, (LogLevel)Enum.Parse(typeof(LogLevel), switchConfig.Value));
            }
        }

        public string ConnectionString { get; set; }

        public string Table { get; set; }

        public IDictionary<string, LogLevel> Switches { get; set; } = new Dictionary<string, LogLevel>();

        public bool TryGetSwitch(string name, out LogLevel level)
        {
            return Switches.TryGetValue(name, out level);
        }
    }
}
