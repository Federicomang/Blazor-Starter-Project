namespace StarterProject.Middlewares
{
    public class QueryParameterTokenMiddleware(RequestDelegate next)
    {
        private static readonly string[] WebSocketPaths = ["/ws"];

        public async Task InvokeAsync(HttpContext context)
        {
            var token = context.Request.Query["access_token"].ToString();

            if (!string.IsNullOrEmpty(token))
            {
                var path = context.Request.Path;
                if (WebSocketPaths.Any(p => path.StartsWithSegments(p)))
                {
                    context.Request.Headers.Authorization = $"Bearer {token}";
                }
            }

            await next(context);
        }
    }
}
