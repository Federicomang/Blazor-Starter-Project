using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StarterProject.Attributes;

namespace StarterProject.Middlewares.Transformers
{
    public class ExplicitResponseBodyTransformer : IOpenApiOperationTransformer
    {
        public async Task TransformAsync(
            OpenApiOperation operation,
            OpenApiOperationTransformerContext context,
            CancellationToken cancellationToken)
        {
            var attributes = context.Description
                .ActionDescriptor
                .EndpointMetadata
                .OfType<ExplicitOpenApiResponseAttribute>()
                .ToList();

            if (attributes.Count == 0)
                return;

            var responses = operation.Responses ?? [];

            foreach (var attr in attributes)
            {
                var content = new Dictionary<string, OpenApiMediaType>();
                foreach (var cnt in attr.Contents)
                {
                    var schema = await context.GetOrCreateSchemaAsync(cnt.RequestType, cancellationToken: cancellationToken);
                    if (content.TryGetValue(cnt.ContentType, out OpenApiMediaType? value) && value != null)
                    {
                        if (value.Schema!.OneOf == null)
                        {
                            content[cnt.ContentType] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    OneOf = [value.Schema, schema]
                                }
                            };
                        }
                        else
                        {
                            value.Schema.OneOf.Add(schema);
                        }
                    }
                    else
                    {
                        content[cnt.ContentType] = new OpenApiMediaType { Schema = schema };
                    }
                }
                if(content.Count > 0)
                {
                    responses[attr.StatusCode.ToString()] = new OpenApiResponse
                    {
                        Description = attr.Description ?? "No description",
                        Content = content
                    };
                }
            }

            operation.Responses = responses;
        }
    }
}
