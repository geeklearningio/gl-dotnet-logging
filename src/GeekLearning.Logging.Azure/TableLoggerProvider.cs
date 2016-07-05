namespace GeekLearning.Logging.Azure
{
    using Microsoft.Extensions.Logging;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    public class TableLoggerProvider : ILoggerProvider
    {
        private string azureConnectionString;
        private string logTable;
        private Func<string, Microsoft.Extensions.Logging.LogLevel, bool> filter;
        private CloudTableClient client;
        private CloudTable table;
        private TransformBlock<DynamicTableEntity, DynamicTableEntity> pipeline;
        private BatchBlock<DynamicTableEntity> batchBlock;
        private ITargetBlock<DynamicTableEntity[]> pipelineEnd;

        public TableLoggerProvider(string azureConnectionString, string logTable, Func<string, Microsoft.Extensions.Logging.LogLevel, bool> filter)
        {
            this.azureConnectionString = azureConnectionString;
            this.logTable = logTable;
            this.filter = filter;

            var cloud = CloudStorageAccount.Parse(azureConnectionString);
            this.client = cloud.CreateCloudTableClient();
            this.table = client.GetTableReference(logTable);

            this.batchBlock = new BatchBlock<DynamicTableEntity>(25);

            Timer triggerBatchTimer = new Timer((_) => this.batchBlock.TriggerBatch(), null, Timeout.Infinite, Timeout.Infinite);

            pipeline = new TransformBlock<DynamicTableEntity, DynamicTableEntity>((value) =>
            {
                triggerBatchTimer.Change(5000, Timeout.Infinite);

                return value;
            });

            pipelineEnd = new ActionBlock<DynamicTableEntity[]>(WriteBatch);
            pipeline.LinkTo(batchBlock);
            batchBlock.LinkTo(pipelineEnd);
        }

        public ILogger CreateLogger(string name)
        {
            return new AzureLogger(name, azureConnectionString, logTable, filter, true, pipeline);
        }

        private async Task WriteBatch(DynamicTableEntity[] rows)
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

        #region IDisposable Support
        // To detect redundant calls
        private bool disposedValue = false;

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
                this.batchBlock.TriggerBatch();
                pipeline.Complete();
                pipeline.Completion.Wait();
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
        private Func<string, Microsoft.Extensions.Logging.LogLevel, bool> filter;
        private D64.TimebasedId timeBasedId = new D64.TimebasedId(true);
        private ITargetBlock<DynamicTableEntity> sink;

        public AzureLogger(string name, string azureConnectionString, string logTable, Func<string, Microsoft.Extensions.Logging.LogLevel, bool> filter, bool includeScopes, ITargetBlock<DynamicTableEntity> sink)
        {
            this.sink = sink;
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Filter = filter ?? ((category, logLevel) => true);
        }

        private string Name { get; }

        public bool IncludeScopes { get; set; }

        public Func<string, Microsoft.Extensions.Logging.LogLevel, bool> Filter
        {
            get { return filter; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                filter = value;
            }
        }

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return Filter(Name, logLevel);
        }

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);
            string exceptionText = exception?.ToString();

            var properties = new Dictionary<string, EntityProperty>
            {
                ["LogLevel"] = EntityProperty.GeneratePropertyForString(logLevel.ToString()),
                ["Message"] = EntityProperty.GeneratePropertyForString(message),
                ["ExceptionText"] = EntityProperty.GeneratePropertyForString(exceptionText),
                ["EventId"] = EntityProperty.GeneratePropertyForInt(eventId.Id),
                ["EventName"] = EntityProperty.GeneratePropertyForString(eventId.Name),
                ["Date"] = EntityProperty.GeneratePropertyForDateTimeOffset(DateTimeOffset.Now),
                ["LoggerName"] = EntityProperty.GeneratePropertyForString(this.Name),
            };

            string requestId = null;
            var current = AzureScope.Current;
            while (current != null)
            {
                foreach (var item in current.Values)
                {
                    properties.Add($"Scope{current.StateName}{item.Key}", EntityProperty.CreateEntityPropertyFromObject(item.Value));
                    if (item.Key == "RequestId")
                    {
                        requestId = (string)item.Value;
                    }
                }

                current = current.Parent;
            }

            properties["RequestId"] = EntityProperty.GeneratePropertyForString(requestId);

            var id = timeBasedId.NewId();
            var partition = (requestId ?? id).Substring(0, 5);

            if (message != null || exceptionText != null)
            {
                var entity = new DynamicTableEntity(
                    partition,
                    id, null, properties);

                this.sink.Post(entity);
            }
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return AzureScope.Push(Name, state);
        }
    }

    public class LogRow : TableEntity
    {
        public string LogLevel { get; set; }

        public string Message { get; set; }

        public string ExceptionText { get; set; }

        public int EventId { get; set; }

        public string EventName { get; set; }

        public DateTimeOffset Date { get; set; }

        public string LoggerName { get; set; }

        public string RequestId { get; set; }
    }
}