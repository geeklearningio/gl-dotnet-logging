namespace GeekLearning.Logging
{
    using Microsoft.AspNetCore.Http;
    using System.Threading.Tasks;

    public class AppendRequestIdHeaderMiddleware
    {
        private const string RequestIdHeaderName = "X-Request-Id";
        private readonly RequestDelegate next;

        public AppendRequestIdHeaderMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(RequestIdHeaderName))
                {
                    context.Response.Headers[RequestIdHeaderName] = context.TraceIdentifier;
                }

                return Task.FromResult(0);
            });
            await this.next.Invoke(context);
        }
    }
}
