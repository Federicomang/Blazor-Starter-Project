using StarterProject.Database.Entities;

namespace StarterProject.Extensions
{
    public static class HttpContextExtensions
    {
        public class HttpContextItems(HttpContext context)
        {
            public const string DATA_PREFIX = "DATA#";

            private T? GetItem<T>(string key) => (T?)context.Items[DATA_PREFIX + key];
            public HttpContext GetHttpContext() => context;

            public string? AuthenticationScheme => GetItem<string>(nameof(AuthenticationScheme));

            public User? User => GetItem<User>(nameof(User));

            public string? ApplicationId => GetItem<string>(nameof(ApplicationId));
        }

        public static HttpContextItems GetItems(this HttpContext httpContext)
        {
            return new HttpContextItems(httpContext);
        }

        public static void SetFeatureApiResponse(this HttpContext context, IResult resultResponse)
        {
            if (!context.IsSocketConnection())
            {
                context.Items["ApiResult"] = resultResponse;
            }
        }

        public static async Task ApplyApiFeatureResponse(this HttpContext context)
        {
            if (context.Items.TryGetValue("ApiResult", out var apiResult))
            {
                await ((IResult)apiResult!).ExecuteAsync(context);
            }
        }

        public static bool IsBrowserRequest(this HttpContext context)
        {
            return context.Request.GetTypedHeaders().Accept.Any(h =>
                h.MediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase)
            ) == true;
        }

        public static bool IsSocketConnection(this HttpContext context)
        {
            return context.Request.Method == "CONNECT" || context.WebSockets.IsWebSocketRequest;
        }
    }
}
