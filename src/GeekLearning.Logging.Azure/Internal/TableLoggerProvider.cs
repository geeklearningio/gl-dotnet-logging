namespace GeekLearning.Logging.Azure.Internal
{
    using Microsoft.Extensions.Logging;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.WindowsAzure.Storage.Blob;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    public class TableLoggerProvider : ILoggerProvider
    {
        private Func<string, Microsoft.Extensions.Logging.LogLevel, bool> filter;
        private CloudBlobClient blobClient;
        private CloudBlobContainer container;
        private CloudTableClient client;
        private CloudTable table;
        private TransformBlock<DynamicTableEntity, DynamicTableEntity> pipeline;
        private BatchBlock<DynamicTableEntity> batchBlock;
        private ITargetBlock<DynamicTableEntity[]> pipelineEnd;

        private ITargetBlock<KeyValuePair<string, string>> overflowBlock;

        private AzureLoggerSettings settings;
        private bool disposedValue = false;

        public TableLoggerProvider(AzureLoggerSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            this.settings = settings;

            var cloud = CloudStorageAccount.Parse(settings.ConnectionString);
            this.client = cloud.CreateCloudTableClient();
            this.table = client.GetTableReference(settings.Table);

            this.blobClient = cloud.CreateCloudBlobClient();

            if (!string.IsNullOrEmpty(settings.OverflowContainer))
            {
                this.container = this.blobClient.GetContainerReference(settings.OverflowContainer);
            }

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

            this.overflowBlock = new ActionBlock<KeyValuePair<string, string>>(WriteOverflow);
        }


        public ILogger CreateLogger(string name)
        {
            return new TableLogger(name, GetFilter(name, settings), true, this.settings.OverflowThreshold.GetValueOrDefault(8000), pipeline, overflowBlock);
        }

        private Task WriteBatch(DynamicTableEntity[] input)
        {
            return new Task((object data) =>
            {
                var rows = (DynamicTableEntity[])data;
                foreach (var group in rows.GroupBy(x => x.PartitionKey))
                {
                    TableBatchOperation batchOperation = new TableBatchOperation();
                    foreach (var row in group)
                    {
                        batchOperation.Insert(row, false);
                    }

                    table.ExecuteBatchAsync(batchOperation).Wait();
                }
            }, input, TaskCreationOptions.LongRunning);
        }

        private Task WriteOverflow(KeyValuePair<string, string> input)
        {
            return new Task((object data) =>
            {
                var overflowData = (KeyValuePair<string, string>)data;
                if (this.container != null)
                {
                    var reference = this.container.GetBlockBlobReference(overflowData.Key);
                    reference.UploadTextAsync(overflowData.Value).Wait();
                }
            }, input, TaskCreationOptions.LongRunning);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                this.batchBlock.TriggerBatch();
                pipeline.Complete();
                pipeline.Completion.Wait();
                disposedValue = true;
            }
        }

        private Func<string, Microsoft.Extensions.Logging.LogLevel, bool> GetFilter(string name, AzureLoggerSettings settings)
        {
            if (filter != null)
            {
                return filter;
            }

            if (settings != null)
            {
                foreach (var prefix in GetKeyPrefixes(name))
                {
                    Microsoft.Extensions.Logging.LogLevel level;
                    if (settings.TryGetSwitch(prefix, out level))
                    {
                        this.filter = (n, l) => l >= level;
                        return this.filter;
                    }
                }
            }

            this.filter = (n, l) => false;
            return this.filter;
        }

        private IEnumerable<string> GetKeyPrefixes(string name)
        {
            while (!string.IsNullOrEmpty(name))
            {
                yield return name;
                var lastIndexOfDot = name.LastIndexOf('.');
                if (lastIndexOfDot == -1)
                {
                    yield return "Default";
                    break;
                }
                name = name.Substring(0, lastIndexOfDot);
            }
        }
    }
}