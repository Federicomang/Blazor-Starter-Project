using BlazorFeatures.Abstractions;
using BlazorFeatures.Abstractions.Tools;
using BlazorFeatures.Base.Attributes;
using BlazorFeatures.Base.Server;
using FluentValidation;
using System.Net;
using System.Reflection;

namespace StarterProject.Features
{
    public sealed class FluentValidationFeatureBehavior(
        IServiceProvider serviceProvider) : IServerFeatureBehavior
    {
        public int Order => 1_000;

        public async Task<FeatureResponse<TResponse>> HandleAsync<TResponse>(
            ServerFeatureExecutionContext<TResponse> context,
            ServerFeatureDelegate<TResponse> next,
            CancellationToken cancellationToken = default)
            where TResponse : class
        {
            var requestType = context.Request.GetType();
            if (requestType.GetCustomAttribute<DisableServerFluentValidationAttribute>() != null)
                return await next();

            var validatorType = ReflectionTools.GetGenericType(
                typeof(IValidator<>),
                requestType);
            if (serviceProvider.GetService(validatorType) is not IValidator validator)
                return await next();

            var validationContextType = ReflectionTools.GetGenericType(
                typeof(ValidationContext<>),
                requestType);
            if (ReflectionTools.CreateInstance(
                validationContextType,
                context.Request) is not IValidationContext validationContext)
            {
                return await next();
            }

            var result = await validator.ValidateAsync(
                validationContext,
                cancellationToken);
            return result.IsValid
                ? await next()
                : FeatureResponse<TResponse>.AsFailure(
                    validationErrors: result.ToDictionary(),
                    statusCode: HttpStatusCode.BadRequest);
        }
    }
}
