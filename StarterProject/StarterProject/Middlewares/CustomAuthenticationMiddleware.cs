using StarterProject.Infrastructure;

namespace StarterProject.Middlewares
{
    public class CustomAuthenticationMiddleware(ClaimsEnricher claimsEnricher) : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            await claimsEnricher.RefreshIdentity(context.User);
            await next(context);
        }
    }
}
