using FluentValidation;
using StarterProject.Client.Attributes;
using StarterProject.Client.Features;
using StarterProject.Client.Tools;
using StarterProject.Extensions;
using System.Reflection;

namespace StarterProject.Features
{
    public class ServerFeatureService(IServiceProvider serviceProvider, IHttpContextAccessor httpContextAccessor) : IServerFeatureService
    {
        public async Task<FeatureResponse<Response>> HandleServer<Response>(IFeatureHandler<Response> handler, Type requestType, IBaseFeatureRequest<Response> request, CancellationToken cancellationToken = default) where Response : class
        {
            var httpContext = httpContextAccessor.HttpContext;
            if(httpContext != null)
            {
                httpContext.Items["FeatureRequest"] = request;

                var objectRequestType = request.GetType();
                var disableValidation = objectRequestType.GetCustomAttribute<DisableServerFluentValidationAttribute>();
                if (disableValidation == null)
                {
                    var validator = (IValidator?)serviceProvider.GetService(ReflectionTools.GetGenericType(typeof(IValidator<>), objectRequestType));
                    if (validator != null)
                    {
                        var validatorContext = (IValidationContext?)ReflectionTools.CreateInstance(ReflectionTools.GetGenericType(typeof(ValidationContext<>), objectRequestType), request);
                        if (validatorContext != null)
                        {
                            var validationResult = await validator.ValidateAsync(validatorContext, cancellationToken);
                            if (!validationResult.IsValid)
                            {
                                var validationRes = FeatureResponse<Response>.AsFailure(validationErrors: validationResult.ToDictionary());
                                httpContext.SetFeatureApiResponse(Results.BadRequest(validationRes));
                                return validationRes;
                            }
                        }
                    }
                }
            }

            return await handler.Handle();
        }
    }
}
