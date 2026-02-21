using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using StarterProject.Client.Features;
using System.Reflection;

namespace StarterProject.Middlewares.Transformers
{
    public class FeatureResponseBodyTransformer : IOpenApiOperationTransformer
    {
        public async Task TransformAsync(
            OpenApiOperation operation,
            OpenApiOperationTransformerContext context,
            CancellationToken cancellationToken)
        {
            var methodInfo = context.Description.ActionDescriptor
                .EndpointMetadata
                .OfType<MethodInfo>()
                .FirstOrDefault();

            if (methodInfo == null) return;

            Type? responseType = null;

            foreach (var p in methodInfo.GetParameters())
            {
                var type = p.ParameterType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBaseFeatureRequest<>));
                if(type != null)
                {
                    responseType = type.GetGenericArguments().FirstOrDefault();
                    break;
                }
            }

            if(responseType != null)
            {
                var featureResponseType = typeof(FeatureResponse<>).MakeGenericType(responseType);
                var schema = await context.GetOrCreateSchemaAsync(featureResponseType, cancellationToken: cancellationToken);
                operation.Responses = new OpenApiResponses()
                {
                    ["200"] = new OpenApiResponse()
                    {
                        Description = "No description",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType()
                            {
                                Schema = schema
                            }
                        }
                    }
                };
            }
        }
    }
}
