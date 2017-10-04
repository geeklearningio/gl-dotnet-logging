namespace GeekLearning.Logging.StackTrace
{
    using System.Collections.Generic;

    public class StackTraceFrame
    {
        public StackTraceToken Frame { get; private set; }

        public StackTraceMethod Method { get; private set; }

        public StackTraceParameterList Parameters { get; private set; }

        public StackTraceSource Source { get; private set; }

        public static IEnumerable<StackTraceFrame> Parse(string stackTrace)
        {
            return Elmah.StackTraceParser.Parse(stackTrace, (idx, len, txt) => new StackTraceToken // the token is the smallest unit, made of:
            {
                Index = idx,      // - the index of the token text start
                Length = len,      // - the length of the token text
                Text = txt,      // - the actual token text
            },
            (type, method) => new StackTraceMethod // the method and its declaring type
            {
                Type = type,
                Method = method,
            },
            (type, name) => new StackTraceParameter   // this is called back for each parameter with:
            {
                Type = type,       // - the parameter type
                Name = name,       // - the parameter name
            },
            (pl, ps) => new StackTraceParameterList      // the parameter list and sequence of parameters
            {
                List = pl,         // - spans all parameters, including parentheses
                Parameters = ps,   // - sequence of individual parameters
            },
            (file, line) => new StackTraceSource  // source file and line info
            {                      // called back if present
                File = file,
                Line = line,
            },
            (f, tm, p, fl) => new StackTraceFrame // finally, put all the components of a frame
            {                      // together! The result of the parsing function
                Frame = f,         // is a sequence of this.
                Method = tm,
                Parameters = p,
                Source = fl
            });
        }
    }
}
