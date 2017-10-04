namespace GeekLearning.Logging.StackTrace
{
    using System.Collections.Generic;

    public class StackTraceParameterList
    {
        public StackTraceToken List { get; internal set; }

        public IEnumerable<StackTraceParameter> Parameters { get; internal set; }
    }
}
