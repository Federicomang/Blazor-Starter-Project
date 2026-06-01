using BlazorFeatures.Abstractions;
using BlazorFeatures.Abstractions.Attributes;
using BlazorFeatures.Abstractions.Server;
using BlazorFeatures.Abstractions.Tools;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using StarterProject.Extensions;
using StarterProject.Infrastructure;
using System.Reflection;
using System.Text.Json;

namespace StarterProject.Features
{
    public class ServerFeatureService(
        IServiceProvider serviceProvider,
        IAuthorizationService authorizationService,
        CustomAuthStateProvider authProvider,
        FeatureApplicationOptions featureApplicationOptions,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ServerFeatureService> logger) : IServerFeatureService
    {
        public async Task<FeatureResponse<Response>> HandleServer<Response>(IFeatureHandler<Response> handler, Type requestType, IBaseFeatureRequest<Response> request, IFeatureContext? featureContext, CancellationToken cancellationToken = default) where Response : class
        {
            var httpContext = httpContextAccessor.HttpContext!;
            var isSocketConnection = httpContext.IsSocketConnection();
            featureContext ??= new FeatureContext();

            try
            {
                if(isSocketConnection)
                {
                    if(request is IBaseFeatureAuthorization auth)
                    {
                        var principal = await authProvider.ForceRefreshAsync();
                        if(principal == null)
                        {
                            return FeatureResponse<Response>.AsFailure();
                        }

                        var builder = new AuthorizationPolicyBuilder();
                        auth.BuildPolicy(builder);
                        var policy = builder.Build();
                        var authResult = await authorizationService.AuthorizeAsync(principal, policy);
                        if(!authResult.Succeeded)
                        {
                            return FeatureResponse<Response>.AsFailure();
                        }
                    }
                }
                else
                {
                    httpContext.Items["FeatureRequest"] = request;
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
                                var validationRes = FeatureResponse<Response>.AsFailure(validationErrors: validationResult.ToDictionary());
                                httpContext.SetFeatureApiResponse(Results.BadRequest(validationRes));
                                return validationRes;
                            }
                        }
                    }
                }

                return await handler.Handle(featureContext);
            }
            catch(Exception e)
            {
                if(isSocketConnection)
                {
                    if (logger.IsEnabled(LogLevel.Error) == true)
                    {
                        var featureName = request.GetType().FullName;
                        var payload = JsonSerializer.Serialize(request, request.GetType(), featureApplicationOptions.JsonSerializerOptions);
                        logger.LogError(e, "An error as occurred in server - Feature: {featureName} - Request Payload: {payload}", featureName, payload);
                    }
                    return FeatureResponse<Response>.AsFailure(messages: ["Internal server error"]);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
