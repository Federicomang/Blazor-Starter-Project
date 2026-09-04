using BlazorFeatures.Abstractions;
using BlazorFeatures.Base.Server;

namespace StarterProject.Middlewares
{
    public class ExceptionMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await next(httpContext);
            }
            catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
            {
                // The client disconnected: do not log or attempt to write a response.
            }
            catch (Exception e)
            {
                var featureContext = httpContext.Features.Get<IHttpFeatureContext>();
                var request = featureContext?.FeatureChain.LastOrDefault();
                var logger = httpContext.RequestServices.GetService<ILogger<ExceptionMiddleware>>();
                if (logger?.IsEnabled(LogLevel.Error) == true)
                {
                    var endpoint = httpContext.GetEndpoint()?.DisplayName;
                    logger.LogError(e,
                        "An error has occurred - Endpoint: {Endpoint} - Feature: {Feature} - Operation: {OperationId}",
                        endpoint,
                        request?.GetType().FullName,
                        featureContext?.OperationId);
                }
                IResult result;
                if (request == null)
                {
                    if (e is BadHttpRequestException ex)
                    {
                        result = Results.Content(ex.Message, statusCode: ex.StatusCode);
                    }
                    else
                    {
                        result = Results.InternalServerError();
                    }
                }
                else
                {
                    var respFailure = FeatureResponse<object>.AsFailure(messages: ["Internal server error"]);
                    result = Results.Json(respFailure, statusCode: StatusCodes.Status500InternalServerError);
                }
                await result.ExecuteAsync(httpContext);
            }
        }
    }
}
