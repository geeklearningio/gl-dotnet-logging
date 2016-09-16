using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeekLearning.Logging.StackTrace
{
    public class StackTraceMethod
    {
        public StackTraceToken Method { get; internal set; }
        public StackTraceToken Type { get; internal set; }
    }
}
