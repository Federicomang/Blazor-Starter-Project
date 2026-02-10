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
                var payload = JsonSerializer.Serialize(httpContext.Items["FeatureRequest"]);
                var logger = httpContext.RequestServices.GetService<ILogger<ExceptionMiddleware>>();
                if (logger?.IsEnabled(LogLevel.Error) == true)
                {
                    var endpoint = httpContext.GetEndpoint()?.DisplayName;
                    logger.LogError(e, "An error as occurred - Endpoint: {endpoint} - Request Payload: {payload}", endpoint, payload);
                }
                if(payload != null)
                {
                    var respFailure = FeatureResponse<object>.AsFailure(messages: ["Internal server error"]);
                    var result = Results.Json(respFailure, statusCode: StatusCodes.Status500InternalServerError);
                    await result.ExecuteAsync(httpContext);
                }
            }
        }
    }
}
