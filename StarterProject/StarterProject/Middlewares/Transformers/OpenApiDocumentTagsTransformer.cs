using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StarterProject.OpenApi;

namespace StarterProject.Middlewares.Transformers
{
    public sealed class OpenApiDocumentTagsTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            var metadata = context.Description.ActionDescriptor.EndpointMetadata
                .OfType<OpenApiDocumentTagsMetadata>()
                .FirstOrDefault(tagsMetadata => string.Equals(
                    tagsMetadata.DocumentName,
                    context.DocumentName,
                    StringComparison.OrdinalIgnoreCase));

            if (metadata is null || metadata.Tags.Count == 0)
            {
                return Task.CompletedTask;
            }

            operation.Tags = metadata.Tags
                .Select(tag => new OpenApiTagReference(tag, context.Document))
                .ToHashSet();

            return Task.CompletedTask;
        }
    }
}
