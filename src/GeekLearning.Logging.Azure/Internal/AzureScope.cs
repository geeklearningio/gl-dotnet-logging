namespace GeekLearning.Logging.Azure.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
#if NET452
    using System.Runtime.Remoting;
    using System.Runtime.Remoting.Messaging;
#else
    using System.Threading;
#endif

    public abstract class AzureScope
    {
        protected readonly string name;
        protected readonly object state;
        protected IDictionary<string, string> values;

        internal AzureScope(string name, object state)
        {
            this.name = name;
            this.state = state;
        }

        public AzureScope Parent { get; private set; }

        public abstract IEnumerable<KeyValuePair<string, object>> Values { get; }

        public abstract string StateName { get; }

#if NET452
        private static string fieldKey = typeof(AzureScope).FullName + ".Value";
        public static AzureScope Current
        {
            get
            {
                var handle = CallContext.LogicalGetData(fieldKey) as ObjectHandle;
                if (handle == null)
                {
                    return default(AzureScope);
                }

                return (AzureScope)handle.Unwrap();
            }
        
            set
            {
                CallContext.LogicalSetData(fieldKey, new ObjectHandle(value));
            }
        }
#else
        private static AsyncLocal<AzureScope> asyncLocalScope = new AsyncLocal<AzureScope>();
        public static AzureScope Current
        {
            set
            {
                asyncLocalScope.Value = value;
            }

            get
            {
                return asyncLocalScope.Value;
            }
        }
#endif

        public static IDisposable Push<TState>(string name, TState state)
        {
            var temp = Current;
            Current = new AzureScope<TState>(name, state);
            Current.Parent = temp;

            return new DisposableScope();
        }

        public override string ToString()
        {
            return state?.ToString();
        }

        private class DisposableScope : IDisposable
        {
            public void Dispose()
            {
                Current = Current.Parent;
            }
        }
    }

    public class AzureScope<TState> : AzureScope
    {
        private static readonly string stateName;

        static AzureScope()
        {
            stateName = typeof(TState).Name;
        }

        public AzureScope(string name, TState state) : base(name, state)
        {
        }

        public override IEnumerable<KeyValuePair<string, object>> Values
        {
            get
            {
                return this.state as IReadOnlyList<KeyValuePair<string, object>>?? Enumerable.Empty<KeyValuePair<string, object>>();
            }
        }

        public override string StateName
        {
            get
            {
                return stateName;
            }
        }
    }
}
