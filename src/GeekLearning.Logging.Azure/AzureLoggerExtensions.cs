namespace GeekLearning.Logging.Azure
{
    using Microsoft.Extensions.Logging;

    public static class AzureLoggerExtensions
    {
        public static ILoggerFactory AddAzureTable(this ILoggerFactory loggerFactory, string azureConnectionString, string logTable, LogLevel minLevel)
        {
            loggerFactory.AddProvider(new TableLoggerProvider(azureConnectionString, logTable, (category, logLevel) => logLevel >= minLevel));
            return loggerFactory;
        }
    }
}
