using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeekLearning.Logging.StackTrace
{
    public class StackTraceSource
    {
        public StackTraceToken File { get; internal set; }
        public StackTraceToken Line { get; internal set; }
    }
}
