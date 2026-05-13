using Microsoft.AspNetCore.OpenApi;
using StarterProject.Middlewares.Transformers;
using StarterProject.OpenApi;

namespace StarterProject.Extensions
{
    public static class OpenApiEndpointExtensions
    {
        public static TBuilder WithOpenApiDocuments<TBuilder>(
            this TBuilder builder,
            params string[] documentNames)
            where TBuilder : IEndpointConventionBuilder
        {
            builder.WithMetadata(new OpenApiDocumentMetadata(documentNames));
            return builder;
        }

        public static TBuilder WithOpenApiTagsForDocument<TBuilder>(
            this TBuilder builder,
            string documentName,
            params string[] tags)
            where TBuilder : IEndpointConventionBuilder
        {
            builder.WithMetadata(new OpenApiDocumentTagsMetadata(documentName, tags));
            return builder;
        }

        public static void ConfigureStarterProjectOpenApi(this OpenApiOptions options, string documentName)
        {
            options.ShouldInclude = apiDescription =>
            {
                if (documentName == OpenApiDocumentNames.All)
                {
                    return true;
                }

                var metadata = apiDescription.ActionDescriptor.EndpointMetadata
                    .OfType<OpenApiDocumentMetadata>()
                    .FirstOrDefault();

                return metadata?.DocumentNames.Contains(documentName) == true;
            };

            options.AddDocumentTransformer<AuthorizationTransformer>();
            options.AddOperationTransformer<AuthorizationTransformer>();
            options.AddOperationTransformer<OpenApiDocumentTagsTransformer>();
            options.AddOperationTransformer<ExplicitRequestBodyTransformer>();
            options.AddOperationTransformer<FeatureResponseBodyTransformer>();
            options.AddOperationTransformer<ExplicitResponseBodyTransformer>();
            options.CreateSchemaReferenceId = info =>
            {
                if (info.Type != null && info.Type.FullName!.EndsWith("+" + info.Type.Name))
                {
                    return info.Type.FullName.Replace("+", "-");
                }

                return OpenApiOptions.CreateDefaultSchemaReferenceId(info);
            };
        }
    }
}
