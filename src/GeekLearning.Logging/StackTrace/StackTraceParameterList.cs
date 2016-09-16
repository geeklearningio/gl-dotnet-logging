using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeekLearning.Logging.StackTrace
{
    public class StackTraceParameterList
    {
        public StackTraceToken List { get; internal set; }
        public IEnumerable<StackTraceParameter> Parameters { get; internal set; }
    }
}
