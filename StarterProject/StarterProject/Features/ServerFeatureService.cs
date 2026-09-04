using BlazorFeatures.Abstractions;
using BlazorFeatures.Abstractions.Tools;
using BlazorFeatures.Base.Attributes;
using BlazorFeatures.Base.Server;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using StarterProject.Infrastructure;
using System.Net;
using System.Reflection;

namespace StarterProject.Features
{
    public class ServerFeatureService(
        IServiceProvider serviceProvider,
        IAuthorizationService authorizationService,
        CustomAuthStateProvider authProvider,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ServerFeatureService> logger) : IServerFeatureService
    {
        public async Task<FeatureResponse<Response>> HandleServer<Response>(IBaseFeature feature, IFeatureHandler<Response> handler, Type requestType, IBaseFeatureRequest<Response> request, IFeatureContext featureContext, CancellationToken cancellationToken = default) where Response : class
        {
            var httpContext = featureContext is IHttpFeatureContext httpFeatureContext
                ? httpFeatureContext.HttpContext
                : httpContextAccessor.HttpContext;

            try
            {
                if(feature is IBaseFeatureAuthorization authorization)
                {
                    var principal = featureContext is IHttpFeatureContext
                        ? httpContext?.User
                        : await authProvider.ForceRefreshAsync();

                    if(principal?.Identities.Any(identity => identity.IsAuthenticated) != true)
                    {
                        return FeatureResponse<Response>.AsFailure(statusCode: HttpStatusCode.Unauthorized);
                    }

                    var builder = new AuthorizationPolicyBuilder();
                    authorization.BuildPolicy(builder);
                    var policy = builder.Build();
                    var authResult = await authorizationService.AuthorizeAsync(principal, policy);
                    if(!authResult.Succeeded)
                    {
                        return FeatureResponse<Response>.AsFailure(statusCode: HttpStatusCode.Forbidden);
                    }
                }

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
                                return FeatureResponse<Response>.AsFailure(
                                    validationErrors: validationResult.ToDictionary(),
                                    statusCode: HttpStatusCode.BadRequest);
                            }
                        }
                    }
                }

                return await handler.Handle(featureContext);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch(Exception e)
            {
                if(featureContext is not IHttpFeatureContext)
                {
                    if (logger.IsEnabled(LogLevel.Error) == true)
                    {
                        var featureName = request.GetType().FullName;
                        logger.LogError(e, "An error has occurred in server - Feature: {FeatureName} - Operation: {OperationId}", featureName, featureContext.OperationId);
                    }
                    return FeatureResponse<Response>.AsFailure(
                        messages: ["Internal server error"],
                        statusCode: HttpStatusCode.InternalServerError);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
