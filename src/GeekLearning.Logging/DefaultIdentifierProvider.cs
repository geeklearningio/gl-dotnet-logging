using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace GeekLearning.Logging
{
    public class DefaultIdentifierProvider : IIdentifierProvider
    {
       
        public IHttpRequestIdentifierFeature GetHttpRequestIdentifierFeature()
        {
            return new TimeBasedHttpRequestIdentifierFeature();
        }

        private class TimeBasedHttpRequestIdentifierFeature : IHttpRequestIdentifierFeature
        {
            private D64.TimebasedId timeBasedId = new D64.TimebasedId(true);

            private string id = null;

            public string TraceIdentifier
            {
                get
                {
                    // Don't incur the cost of generating the request ID until it's asked for
                    if (id == null)
                    {
                        id = timeBasedId.NewId();
                    }
                    return id;
                }
                set
                {
                    id = value;
                }
            }
        }
    }
}
