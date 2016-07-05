namespace GeekLearning.Logging
{
    using Microsoft.AspNetCore.Http.Features;

    public interface IIdentifierProvider
    {
        IHttpRequestIdentifierFeature GetHttpRequestIdentifierFeature();
    }
}
