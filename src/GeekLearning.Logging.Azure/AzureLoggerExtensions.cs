namespace GeekLearning.Logging.Azure
{
    using Internal;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;

    public static class AzureLoggerExtensions
    {
        public static ILoggerFactory AddAzureTable(this ILoggerFactory loggerFactory, string azureConnectionString, string logTable, LogLevel minLevel)
        {
            loggerFactory.AddProvider(new TableLoggerProvider(new AzureLoggerSettings
            {
                ConnectionString = azureConnectionString,
                Table = logTable,
                Switches = new Dictionary<string, LogLevel>
                {
                    ["Default"] = minLevel
                }
            }));

            return loggerFactory;
        }

        public static ILoggerFactory AddAzureTable(this ILoggerFactory loggerFactory, AzureLoggerSettings settings)
        {
            loggerFactory.AddProvider(new TableLoggerProvider(settings));
            return loggerFactory;
        }

        public static ILoggerFactory AddAzureTable(this ILoggerFactory loggerFactory, IConfigurationSection configurationSection)
        {
            loggerFactory.AddProvider(new TableLoggerProvider(new AzureLoggerSettings(configurationSection)));
            return loggerFactory;
        }
    }
}
