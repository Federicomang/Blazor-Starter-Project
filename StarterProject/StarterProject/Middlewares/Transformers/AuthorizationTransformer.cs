using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace StarterProject.Middlewares.Transformers
{
    public class AuthorizationTransformer : IOpenApiOperationTransformer, IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            var metadata = context.Description.ActionDescriptor.EndpointMetadata;

            var allowsAnonymous =
                metadata.Any(m => m.GetType().Name.Contains("AllowAnonymous", StringComparison.OrdinalIgnoreCase));

            if (allowsAnonymous)
            {
                // override: endpoint pubblico (anche se per caso ci fosse security globale)
                operation.Security = [];
                return Task.CompletedTask;
            }

            var requiresAuth =
                metadata.Any(m => m.GetType().Name.Contains("Authorize", StringComparison.OrdinalIgnoreCase));

            if (!requiresAuth)
                return Task.CompletedTask;

            operation.Security ??= [];

            // IMPORTANTISSIMO:
            // - NIENTE "{}" (quello rende auth optional)
            // - Qui definiamo "required", ma con OR: Bearer OPPURE Cookie
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
            });
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("CookieAuth", context.Document)] = []
            });

            return Task.CompletedTask;
        }

        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

            // Bearer (JWT)
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Description = "JWT in header: Authorization: Bearer {token}"
            };

            // Cookie auth (in OpenAPI è apiKey in:cookie)
            document.Components.SecuritySchemes["CookieAuth"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Cookie,
                Name = ".AspNetCore.Identity.Application",
                Description = "Autenticazione via cookie"
            };

            return Task.CompletedTask;
        }
    }
}
