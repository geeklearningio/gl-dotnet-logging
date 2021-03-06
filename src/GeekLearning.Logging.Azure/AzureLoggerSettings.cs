﻿namespace GeekLearning.Logging.Azure
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;

    public class AzureLoggerSettings
    {
        public AzureLoggerSettings()
        {

        }

        public AzureLoggerSettings(IConfigurationSection section)
        {
            var thresholdAsString = section["OverflowThreshold"];
            int thresholdAsInt;
            if (!string.IsNullOrEmpty(thresholdAsString) 
                && int.TryParse(thresholdAsString, out thresholdAsInt))
            {
                this.OverflowThreshold = thresholdAsInt;
            }

            this.OverflowContainer = section["OverflowContainer"];
            this.ConnectionString = section["ConnectionString"];
            this.Table = section["Table"];
            foreach (var switchConfig in section.GetSection("LogLevel").GetChildren())
            {
                Switches.Add(switchConfig.Key, (LogLevel)Enum.Parse(typeof(LogLevel), switchConfig.Value));
            }
        }

        public string ConnectionString { get; set; }

        public string Table { get; set; }

        public string OverflowContainer { get; set; }

        public int? OverflowThreshold { get; set; }

        public IDictionary<string, LogLevel> Switches { get; set; } = new Dictionary<string, LogLevel>();

        public bool TryGetSwitch(string name, out LogLevel level)
        {
            return Switches.TryGetValue(name, out level);
        }
    }
}
