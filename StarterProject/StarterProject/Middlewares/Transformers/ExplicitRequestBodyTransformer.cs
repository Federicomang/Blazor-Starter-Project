using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StarterProject.Attributes;

namespace StarterProject.Middlewares.Transformers
{
    public class ExplicitRequestBodyTransformer : IOpenApiOperationTransformer
    {
        public async Task TransformAsync(
            OpenApiOperation operation,
            OpenApiOperationTransformerContext context,
            CancellationToken cancellationToken)
        {
            var attributes = context.Description
                .ActionDescriptor
                .EndpointMetadata
                .OfType<ExplicitOpenApiRequestAttribute>()
                .ToList();

            if (attributes.Count == 0)
                return;

            var content = new Dictionary<string, OpenApiMediaType>();

            foreach(var attr in attributes)
            {
                var schema = await context.GetOrCreateSchemaAsync(attr.RequestType, cancellationToken: cancellationToken);
                if(content.TryGetValue(attr.ContentType, out OpenApiMediaType? value) && value != null)
                {
                    if(value.Schema!.OneOf == null)
                    {
                        content[attr.ContentType] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                OneOf = [ value.Schema, schema ]
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
                    content[attr.ContentType] = new OpenApiMediaType { Schema = schema };
                }
            }

            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = content
            };

            // rimuove eventuale inferenza precedente
            operation.Parameters?.Clear();
        }
    }
}
