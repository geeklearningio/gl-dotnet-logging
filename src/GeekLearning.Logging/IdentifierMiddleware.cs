namespace GeekLearning.Logging
{
    using Microsoft.AspNetCore.Http;
    using System.Threading.Tasks;

    public class IdentifierMiddleware
    {
        private IIdentifierProvider identifierProvider;
        private readonly RequestDelegate next;

        public IdentifierMiddleware(RequestDelegate next, IIdentifierProvider identifierProvider)
        {
            this.next = next;
            this.identifierProvider = identifierProvider;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Features.Set(identifierProvider.GetHttpRequestIdentifierFeature());
            await this.next.Invoke(context);
        }
    }
}
