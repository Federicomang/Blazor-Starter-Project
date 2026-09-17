using BlazorFeatures.Abstractions;
using BlazorFeatures.Base.Server;
using BlazorFeatures.Base.Server.Attributes;
using BlazorFeatures.Base.Server.Extensions;
using BlazorFeatures.Base.Server.Tools;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using StarterProject.Database.Entities;
using StarterProject.Extensions;
using StarterProject.OpenApi;
using StarterProject.Tools;
using System.Security.Claims;
using System.Text.Json;
using static OpenIddict.Abstractions.OpenIddictConstants;
using ClientDoLogin = StarterProject.Client.Features.Identity.DoLogin;

namespace StarterProject.Features.Identity
{
    public class DoLogin(
        IServiceProvider sp,
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        IOpenIddictApplicationManager applicationManager,
        ILogger<DoLogin> logger
    ) : ClientDoLogin(sp), IBaseFeatureEndpoint
    {
        public override async Task<FeatureResponse<Response>> HandleServer(Request featureRequest, IFeatureContext featureContext, CancellationToken cancellationToken = default)
        {
            if (featureContext is not IHttpFeatureContext httpFeatureContext)
            {
                var callerContext = featureContext.CallerContext;
                var jsResponse = await callerContext.JSRuntime!.DoRequest(callerContext.BaseUri!.TrimEnd('/') + ApiPath, new
                {
                    method = "POST",
                    headers = new Dictionary<string, string> {
                        { "Content-Type", "application/x-www-form-urlencoded" }
                    },
                    body = HttpTools.ToQueryString(featureRequest)
                });
                if(jsResponse.StatusCode == StatusCodes.Status200OK)
                {
                    var responseData = string.IsNullOrEmpty(jsResponse.Result)
                        ? new Response()
                        : JsonSerializer.Deserialize<Response>(jsResponse.Result);
                    return FeatureResponse<Response>.Create(true, responseData!);
                }
                else if(!string.IsNullOrEmpty(jsResponse.Result))
                {
                    var errorRes = JsonSerializer.Deserialize<ErrorResponse>(jsResponse.Result);
                    return FeatureResponse<Response>.Create(false, null, errorRes?.ErrorDescription == null ? [] : [errorRes.ErrorDescription]);
                }
                else
                {
                    return FeatureResponse<Response>.Create(false, null);
                }
            }
            else if(httpFeatureContext.HttpContext.Request.Method == "POST")
            {
                var response = await ManageLogin(httpFeatureContext.HttpContext, featureContext.OperationId);
                httpFeatureContext.SetHttpResult(response.Data!);
                return FeatureResponse<Response>.Create(
                    response.Success,
                    new Response(),
                    response.Messages,
                    statusCode: response.StatusCode); // La risposta HTTP effettiva è l'IResult custom.
            }

            httpFeatureContext.SetHttpResult(Results.BadRequest());
            return FeatureResponse<Response>.AsFailure(statusCode: System.Net.HttpStatusCode.BadRequest);
        }

        private async Task<User?> GetUser(string mailOrUsername)
        {
            var user = await userManager.FindByEmailAsync(mailOrUsername);
            user ??= await userManager.FindByNameAsync(mailOrUsername);
            return user;
        }

        private async Task<FeatureResponse<IResult>> ManageLogin(HttpContext httpContext, Guid operationId)
        {
            var request = httpContext.GetOpenIddictServerRequest();
            ClaimsPrincipal? principal = null;
            try
            {
                if (request == null)
                {
                    return FeatureResponse<IResult>.AsFailure(Results.BadRequest(new ErrorResponse { Error = Errors.RequestNotSupported, ErrorDescription = "Invalid request" }), ["Invalid request"], statusCode: System.Net.HttpStatusCode.BadRequest);
                }

                if (request.GrantType == GrantTypes.Password)
                {
                    var user = await GetUser(request.Username!);
                    if (user == null || !await userManager.CheckPasswordAsync(user, request.Password!))
                    {
                        var properties = new AuthenticationProperties(
                            new Dictionary<string, string?>
                            {
                                [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                                    Errors.AccessDenied,

                                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                                    "Credentials not correct"
                            });
                        return FeatureResponse<IResult>.AsFailure(Results.Forbid(properties, [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]), ["Credentials not correct"], statusCode: System.Net.HttpStatusCode.Forbidden);
                    }

                    principal = await signInManager.CreateUserPrincipalAsync(user);

                    // Rimuovi gli scope OIDC, imposta solo quelli API
                    principal.SetScopes("api", Scopes.OfflineAccess); // OfflineAccess = abilita refresh token
                    principal.SetClaim(CustomClaims.OidGrantType, request.GrantType);
                    principal.SetClaim(Claims.Subject, user.Id);
                }
                else if (request.GrantType == GrantTypes.RefreshToken)
                {
                    // Rilegge e valida il refresh token ricevuto
                    var result = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                    if (result is null || result.Principal is null)
                        return FeatureResponse<IResult>.AsFailure(Results.BadRequest(new ErrorResponse { Error = Errors.InvalidToken, ErrorDescription = "Invalid refresh token" }), ["Invalid refresh token"], statusCode: System.Net.HttpStatusCode.BadRequest);

                    principal = result.Principal;

                    // (facoltativo) puoi aggiornare claim, ruoli, ecc.
                    // es: principal.SetClaim("last_refreshed", DateTime.UtcNow.ToString("o"));

                    // Reimposta gli scope
                    //principal.SetScopes("api", Scopes.OfflineAccess);
                }
                else if (request.GrantType == GrantTypes.ClientCredentials)
                {
                    // Recupera i dati del client autenticato
                    var application = await applicationManager.FindByClientIdAsync(request.ClientId!)
                        ?? throw new InvalidOperationException("Unknown client.");

                    // CONTROLLO ORGANIZZAZIONE
                    /* var appId = await applicationManager.GetIdAsync(application);

                    Project? project = await dbContext.Projects.Include(p => p.Organizzation)
                        .FirstOrDefaultAsync(p => p.OpenIddictApplicationId == appId);

                    if (project == null)
                    {
                        return FeatureResponse<IResult>.AsFailure(Results.BadRequest(new ErrorResponse { Error = "Project not found" }), ["Project not found"]);
                    }
                    else if (project.Organizzation.DeactivationDate != null && project.Organizzation.DeactivationDate <= DateTime.Now)
                    {
                        return FeatureResponse<IResult>.AsFailure(Results.BadRequest(new ErrorResponse { Error = "Organization no more active" }), ["Organization no more active"]);
                    }
                    else if (project.DeactivationDate != null && project.DeactivationDate <= DateTime.Now)
                    {
                        return FeatureResponse<IResult>.AsFailure(Results.BadRequest(new ErrorResponse { Error = "Project no more active" }), ["Project no more active"]);
                    }*/


                    // Crea il principal (il "subject" in questo caso è l’ID del client)
                    var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                    identity.AddClaim(Claims.Subject, (await applicationManager.GetClientIdAsync(application))!);
                    identity.AddClaim(Claims.Name, (await applicationManager.GetDisplayNameAsync(application))!);

                    principal = new ClaimsPrincipal(identity);
                    principal.SetClaim(CustomClaims.OidGrantType, request.GrantType);
                    principal.SetScopes(request.GetScopes());
                }

                if (principal == null)
                {
                    return FeatureResponse<IResult>.AsFailure(Results.BadRequest(new ErrorResponse { Error = Errors.UnsupportedGrantType, ErrorDescription = "Unsupported grant type" }), ["Unsupported grant type"], statusCode: System.Net.HttpStatusCode.BadRequest);
                }
                else
                {
                    var isCookie = (bool)request["is_cookie"].GetValueOrDefault(false);
                    if(isCookie)
                    {
                        var isPersistent = (bool)request["is_persistent"].GetValueOrDefault(false);
                        var properties = new AuthenticationProperties()
                        {
                            IsPersistent = isPersistent
                        };
                        return FeatureResponse<IResult>.AsSuccess(Results.SignIn(principal, properties, IdentityConstants.ApplicationScheme));
                    }
                    else
                    {
                        principal.Claims.First(x => x.Type == CustomClaims.OidGrantType).SetDestinations(Destinations.AccessToken);
                        return FeatureResponse<IResult>.AsSuccess(Results.SignIn(principal, null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Login failed - Grant type: {GrantType} - Client: {ClientId} - Operation: {OperationId}",
                    request?.GrantType,
                    request?.ClientId,
                    operationId);
                return FeatureResponse<IResult>.AsFailure(Results.InternalServerError(new ErrorResponse { Error = Errors.ServerError, ErrorDescription = "Internal server error" }), ["Internal server error"]);
            }
        }

        public static void MapEndpoints(IEndpointRouteBuilder builder)
        {
            builder.MapPost(ApiPath, async (HttpContext context, FormBound<Request> request) =>
            {
                await context.RunFeature(request.Value);
            }).WithTags(OpenApiDocumentGroups.Identity)
                .WithMetadata(new ExplicitOpenApiRequestAttribute(new(typeof(Request), "application/x-www-form-urlencoded")))
                .WithMetadata(new ExplicitOpenApiResponseAttribute(StatusCodes.Status200OK, [new(typeof(Response))]))
                .WithMetadata(new ExplicitOpenApiResponseAttribute(StatusCodes.Status400BadRequest, [new(typeof(ErrorResponse))]))
                .DisableAntiforgery();

            //builder.MapGet("/api/identity/connect/authorize", Authorize);
            //builder.MapPost("/api/identity/connect/authorize", Authorize);
        }
    }
}
