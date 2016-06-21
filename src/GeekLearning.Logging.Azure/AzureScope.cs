namespace GeekLearning.Logging.Azure
{
    using System;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Linq.Expressions;
#if NET451
    using System.Runtime.Remoting;
    using System.Runtime.Remoting.Messaging;
#else
    using System.Threading;
#endif

    public abstract class AzureScope
    {
        protected readonly string _name;
        protected readonly object _state;
        protected IDictionary<string, string> values;

        internal AzureScope(string name, object state)
        {
            _name = name;
            _state = state;
        }

        public AzureScope Parent { get; private set; }

#if NET451
        private static string FieldKey = typeof(AzureScope).FullName + ".Value";
        public static AzureScope Current
        {
            get
            {
                var handle = CallContext.LogicalGetData(FieldKey) as ObjectHandle;
                if (handle == null)
                {
                    return default(AzureScope);
                }

                return (AzureScope)handle.Unwrap();
            }
            set
            {
                CallContext.LogicalSetData(FieldKey, new ObjectHandle(value));
            }
        }
#else
        private static AsyncLocal<AzureScope> _value = new AsyncLocal<AzureScope>();
        public static AzureScope Current
        {
            set
            {
                _value.Value = value;
            }
            get
            {
                return _value.Value;
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
            return _state?.ToString();
        }

        private class DisposableScope : IDisposable
        {
            public void Dispose()
            {
                Current = Current.Parent;
            }
        }

        public abstract IEnumerable<KeyValuePair<string, object>> Values { get; }
        public abstract string StateName { get; }
    }


    public class AzureScope<TState> : AzureScope
    {
        private TState tState;
        private static readonly string stateName;
        //private static readonly Func<TState, Dictionary<string, string>> valuesReader;
        //private static readonly string valuesReaderDebug;
        static AzureScope()
        {
            stateName = typeof(TState).Name;
        }
        //    var props = typeof(TState).GetRuntimeProperties();

            //    var dictType = typeof(Dictionary<string, string>);

            //    var addMethod = dictType.GetRuntimeMethod("Add", new Type[] { typeof(string), typeof(string) });
            //    var toStringMethod = typeof(object).GetRuntimeMethod("ToString", new Type[] { });


            //    LabelTarget returnTarget = Expression.Label(typeof(Dictionary<string, string>));

            //    var result = Expression.Variable(typeof(Dictionary<string, string>), "result");
            //    var stateParameter = Expression.Parameter(typeof(TState));
            //    List<Expression> ops = new List<Expression>();
            //    ops.Add(Expression.Assign(result, Expression.New(typeof(Dictionary<string, string>))));

            //    foreach (var prop in props)
            //    {
            //        ops.Add(Expression.Call(result, addMethod,
            //            Expression.Constant(prop.Name, typeof(string)),
            //            Expression.Call(Expression.PropertyOrField(stateParameter, prop.Name), toStringMethod)
            //           // Expression.PropertyOrField(stateParameter, prop.Name)
            //         ));
            //    }

            //    ops.Add(Expression.Label(returnTarget, result));

            //    var finalExpression = Expression.Lambda<Func<TState, Dictionary<string, string>>>(
            //            Expression.Block(new ParameterExpression[] { result }, ops)
            //            , stateParameter);

            //    valuesReaderDebug = finalExpression.ToString();

            //    valuesReader = finalExpression.Compile();
            //}


        public AzureScope(string name, TState state) : base(name, state)
        {
            //this.tState = state;
        }

        public override IEnumerable<KeyValuePair<string, object>> Values
        {
            get
            {
                return this._state as IReadOnlyList<KeyValuePair<string, object>>;
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
