using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeekLearning.Logging
{
    public class AppendRequestIdHeaderMiddleware
    {
        private readonly RequestDelegate next;
        public const string requestIdHeaderName = "X-Request-Id";

        public AppendRequestIdHeaderMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(requestIdHeaderName))
                {
                    context.Response.Headers[requestIdHeaderName] = context.TraceIdentifier;
                }
                return Task.FromResult(0);
            });
            await this.next.Invoke(context);
        }
    }
}
