using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeekLearning.Logging.Azure
{
    public static class AzureLoggerExtensions
    {
        public static ILoggerFactory AddAzureTable(this ILoggerFactory loggerFactory, string azureConnectionString, string logTable, LogLevel minLevel)
        {
            loggerFactory.AddProvider(new TableLoggerProvider(azureConnectionString, logTable, (category, logLevel) => logLevel >= minLevel));
            return loggerFactory;
        } 
    }
}
