using Microsoft.AspNetCore.Antiforgery;
using StarterProject.Tools.Antiforgery;

namespace StarterProject.Middlewares
{
    public class CustomAntiforgeryValidation(IAntiforgery antiforgery, RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();

            var metadata = endpoint?.Metadata.GetMetadata<IgnoreAntiforgeryMetadata>();

            if (metadata != null && metadata.IgnoreForJwtOnly)
            {
                var authHeader = context.Request.Headers.Authorization.ToString();

                // Se NON è JWT, valida l'antiforgery token
                if (string.IsNullOrEmpty(authHeader) ||
                    !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        await antiforgery.ValidateRequestAsync(context);
                        context.Features.Set(new AntiforgeryValidationFeature(false, null));
                    }
                    catch (AntiforgeryValidationException e)
                    {
                        context.Features.Set<IAntiforgeryValidationFeature>(new AntiforgeryValidationFeature(false, e));
                    }
                }
            }

            await next(context);
        }
    }
}
