using BlazorFeatures.Abstractions;
using StarterProject.Client;
using StarterProject.Client.Features;
using System.Text.Json;
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
            catch (Exception e)
            {
                var feature = httpContext.Items["FeatureRequest"];
                var featureApplicationOptions = httpContext.RequestServices.GetService<FeatureApplicationOptions>();
                var logger = httpContext.RequestServices.GetService<ILogger<ExceptionMiddleware>>();
                var payload = feature == null ? null : JsonSerializer.Serialize(feature, feature.GetType(), featureApplicationOptions?.JsonSerializerOptions);
                if (logger?.IsEnabled(LogLevel.Error) == true)
                {
                    var endpoint = httpContext.GetEndpoint()?.DisplayName;
                    logger.LogError(e, "An error as occurred - Endpoint: {endpoint} - Request Payload: {payload}", endpoint, payload);
                }
                IResult result;
                if (payload == null)
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
