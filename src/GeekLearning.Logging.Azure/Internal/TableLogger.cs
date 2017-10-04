namespace GeekLearning.Logging.Azure.Internal
{
    using Microsoft.Extensions.Logging;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks.Dataflow;

    public class TableLogger : ILogger
    {
        private Func<string, LogLevel, bool> filter;
        private D64.TimebasedId timeBasedId = new D64.TimebasedId(true);
        private ITargetBlock<DynamicTableEntity> sink;
        private int overflowThreshold;
        private ITargetBlock<KeyValuePair<string, string>> overflowSink;

        public TableLogger(string name, Func<string, LogLevel, bool> filter, bool includeScopes, int overflowThreshold, ITargetBlock<DynamicTableEntity> sink, ITargetBlock<KeyValuePair<string,string>> overflowSink)
        {
            this.sink = sink;
            this.overflowSink = overflowSink;
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            this.overflowThreshold = overflowThreshold;
            this.Name = name;
            this.Filter = filter ?? ((category, logLevel) => true);
        }

        private string Name { get; }

        public bool IncludeScopes { get; set; }

        public Func<string, LogLevel, bool> Filter
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

        public bool IsEnabled(LogLevel logLevel)
        {
            return Filter(Name, logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
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
            bool hasMessageOverflow = message.Length > overflowThreshold;

            var properties = new Dictionary<string, EntityProperty>
            {
                ["LogLevel"] = EntityProperty.GeneratePropertyForString(logLevel.ToString()),
                ["Message"] = EntityProperty.GeneratePropertyForString(hasMessageOverflow ? message.Substring(0, overflowThreshold) : message),
                ["ExceptionText"] = EntityProperty.GeneratePropertyForString(exceptionText),
                ["EventId"] = EntityProperty.GeneratePropertyForInt(eventId.Id),
                ["EventName"] = EntityProperty.GeneratePropertyForString(eventId.Name),
                ["Date"] = EntityProperty.GeneratePropertyForDateTimeOffset(DateTimeOffset.Now),
                ["LoggerName"] = EntityProperty.GeneratePropertyForString(this.Name),
                ["IsMessageOverflowed"] = EntityProperty.GeneratePropertyForBool(hasMessageOverflow),
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
                if (hasMessageOverflow)
                {
                    this.overflowSink.Post(new KeyValuePair<string, string>(id, message));
                }
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
}
