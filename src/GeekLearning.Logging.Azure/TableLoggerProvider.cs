namespace GeekLearning.Logging.Azure
{
    using System;
    using System.Linq;
    using System.Threading;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;
    using Microsoft.Extensions.Logging;

    public class TableLoggerProvider : ILoggerProvider
    {
        private string azureConnectionString;
        private Microsoft.Extensions.Logging.LogLevel level;
        private string logTable;

        public TableLoggerProvider(string azureConnectionString, string logTable, Microsoft.Extensions.Logging.LogLevel level)
        {
            this.azureConnectionString = azureConnectionString;
            this.logTable = logTable;
            this.level = level;
        }
        public ILogger CreateLogger(string name)
        {

            return new AzureLogger(name, azureConnectionString, logTable, level);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AzureLoggerProvider() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }

    public class AzureLogger : ILogger
    {
        private CloudTableClient client;
        private Microsoft.Extensions.Logging.LogLevel level;
        private int iLevel;
        private string name;
        private CloudTable table;
        private TransformBlock<LogRow, LogRow> pipeline;
        private BatchBlock<LogRow> batchBlock;
        private ITargetBlock<LogRow[]> pipelineEnd;

        public AzureLogger(string name, string azureConnectionString, string logTable, Microsoft.Extensions.Logging.LogLevel level)
        {
            this.name = name;
            var cloud = CloudStorageAccount.Parse(azureConnectionString);
            this.client = cloud.CreateCloudTableClient();
            this.table = client.GetTableReference(logTable);
            this.level = level;
            this.iLevel = (int)level;

            this.batchBlock = new BatchBlock<LogRow>(25);

            Timer triggerBatchTimer = new Timer((_) => this.batchBlock.TriggerBatch(), null, Timeout.Infinite, Timeout.Infinite);

            pipeline = new TransformBlock<LogRow, LogRow>((value) =>
            {
                triggerBatchTimer.Change(5000, Timeout.Infinite);

                return value;
            });

            pipelineEnd = new ActionBlock<LogRow[]>(WriteBatch);
            pipeline.LinkTo(batchBlock);
            batchBlock.LinkTo(pipelineEnd);
        }

        public async Task WriteBatch(LogRow[] rows)
        {
            foreach (var group in rows.GroupBy(x => x.PartitionKey))
            {
                TableBatchOperation batchOperation = new TableBatchOperation();
                foreach (var row in group)
                {
                    batchOperation.Insert(row, false);
                }
                await table.ExecuteBatchAsync(batchOperation);
            }
        }

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return (int)level >= this.iLevel;
        }

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if ((int)logLevel >= this.iLevel)
            {
                this.pipeline.Post(new LogRow()
                {
                    PartitionKey = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd-HH"),
                    RowKey = Guid.NewGuid().ToString(),
                    LogLevel = logLevel.ToString(),
                    Message = formatter(state, exception),
                    EventId = eventId.Id,
                    EventName = eventId.Name,
                    Date = DateTimeOffset.Now,
                    LoggerName = this.name,
                    RequestId = Services.CorrelationIdMiddlewareHelpers.CorrelationId,
                });
            }
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new CustomLoggerScope();
        }
    }

    public class CustomLoggerScope : IDisposable
    {
        public void Dispose()
        {
        }
    }

    public class LogRow : TableEntity
    {
        public string LogLevel { get; set; }
        public string Message { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; }
        public DateTimeOffset Date { get; set; }
        public string LoggerName { get; set; }
        public string RequestId { get; set; }
    }
}