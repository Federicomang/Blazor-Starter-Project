using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Scalar.AspNetCore;
using StarterProject.Attributes;
using StarterProject.Client.Features;
using StarterProject.Database.Entities;
using StarterProject.Extensions;
using StarterProject.Tools;
using System.Security.Claims;
using System.Text.Json;
using static OpenIddict.Abstractions.OpenIddictConstants;
using ClientDoLogin = StarterProject.Client.Features.Identity.DoLogin;

namespace StarterProject.Features.Identity
{
    public class DoLogin(
        IHttpContextAccessor httpContextAccessor,
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        IOpenIddictApplicationManager applicationManager,
        ILogger<DoLogin> logger,
        IJSRuntime jsRuntime,
        NavigationManager navigationManager
    ) : ClientDoLogin, IBaseFeatureEndpoint
    {
        public class ServerRequest : Request //Non mappo gli altri campi poichè vengono
        {
            [FromForm(Name = "is_persistent")]
            public override bool IsPersistent { get; set; } //Only for cookie
        }

        public override async Task<FeatureResponse<Response>> HandleServer(Request featureRequest, CancellationToken cancellationToken = default)
        {
            var httpContext = httpContextAccessor.HttpContext!;
            if (httpContext.IsSocketConnection()) //è in modalità InteractiveServer (quindi comunica tramite SignalR che è una connessione socket)
            {
                var jsResponse = await jsRuntime.InvokeAsync<BrowserDoRequest<string>>("window.doRequest", navigationManager.BaseUri.TrimEnd('/') + ApiPath, new
                {
                    method = "POST",
                    headers = new Dictionary<string, string> {
                        { "Content-Type", "application/x-www-form-urlencoded" }
                    },
                    body = HttpTools.ToUrlEncodedString(featureRequest)
                }, false);
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
                    return FeatureResponse<Response>.Create(false, null, errorRes?.Error == null ? [] : [errorRes.Error]);
                }
                else
                {
                    return FeatureResponse<Response>.Create(false, null);
                }
            }
            else if(httpContext.Request.Method == "POST")
            {
                var response = await ManageLogin(httpContext);
                httpContext.SetFeatureApiResponse(response.Data!);
                return FeatureResponse<Response>.Create(response.Success, new Response(), response.Messages); //Non utilizzo in questo caso il FeatureResponse
            }
            return FeatureResponse<Response>.AsFailure(null);
        }

        private async Task<User?> GetUser(string mailOrUsername)
        {
            var user = await userManager.FindByEmailAsync(mailOrUsername);
            user ??= await userManager.FindByNameAsync(mailOrUsername);
            return user;
        }

        private async Task<FeatureResponse<IResult>> ManageLogin(HttpContext httpContext)
        {
            var request = httpContext.GetOpenIddictServerRequest();
            ClaimsPrincipal? principal = null;
            try
            {
                if (request == null)
                {
                    return FeatureResponse<IResult>.AsFailure(Results.BadRequest(new ErrorResponse { Error = "Invalid request" }), ["Invalid request"]);
                }

                if(request.GrantType == CustomGrants.Cookie)
                {
                    var user = await GetUser(request.Username!);
                    if(user != null)
                    {
                        var isPersistent = (bool)request["is_persistent"].GetValueOrDefault(false);
                        var signInResult = await signInManager.PasswordSignInAsync(user, request.Password!, isPersistent, false);
                        if (signInResult.Succeeded)
                        {
                            return FeatureResponse<IResult>.AsSuccess(Results.Ok());
                        }
                    }
                    return FeatureResponse<IResult>.AsFailure(Results.BadRequest(new ErrorResponse { Error = "Invalid credentials" }), ["Invalid credentials"]);
                }
                else if (request.GrantType == GrantTypes.Password)
                {
                    var user = await GetUser(request.Username!);
                    if (user == null || !await userManager.CheckPasswordAsync(user, request.Password!))
                        return FeatureResponse<IResult>.AsFailure(Results.Forbid(authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]), ["Credentials not correct"]);

                    principal = await signInManager.CreateUserPrincipalAsync(user);

                    // Rimuovi gli scope OIDC, imposta solo quelli API
                    principal.SetScopes("api", Scopes.OfflineAccess); // OfflineAccess = abilita refresh token
                    principal.SetClaim(Claims.Subject, user.Id);
                }
                else if (request.GrantType == GrantTypes.RefreshToken)
                {
                    // Rilegge e valida il refresh token ricevuto
                    var result = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                    if (result is null || result.Principal is null)
                        return FeatureResponse<IResult>.AsFailure(Results.BadRequest(new ErrorResponse { Error = "Invalid refresh token" }), ["Invalid refresh token"]);

                    principal = result.Principal;

                    // (facoltativo) puoi aggiornare claim, ruoli, ecc.
                    // es: principal.SetClaim("last_refreshed", DateTime.UtcNow.ToString("o"));

                    // Reimposta gli scope
                    principal.SetScopes("api", Scopes.OfflineAccess);
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
                    principal.SetScopes(request.GetScopes());
                }

                if (principal == null)
                {
                    return FeatureResponse<IResult>.AsFailure(Results.BadRequest(new ErrorResponse { Error = "Unsupported grant type" }), ["Unsupported grant type"]);
                }
                else
                {
                    principal.SetClaim(CustomClaims.OidGrantType, request.GrantType);
                    principal.Claims.First(x => x.Type == CustomClaims.OidGrantType).SetDestinations(Destinations.AccessToken);
                    return FeatureResponse<IResult>.AsSuccess(Results.SignIn(principal, null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Request: {@Request}", request);
                return FeatureResponse<IResult>.AsFailure(Results.InternalServerError(new ErrorResponse { Error = "Internal server error" }), ["Internal server error"]);
            }
        }

        public static void MapEndpoints(IEndpointRouteBuilder builder)
        {
            builder.MapPost(ApiPath, async (HttpContext context, [FromForm] ServerRequest request, [FromServices] IFeatureService featureService) =>
            {
                await featureService.Run<Request, Response>(request);
                await context.ApplyApiFeatureResponse();
            }).WithMetadata(new ExplicitOpenApiRequestAttribute(new(typeof(Request), "application/x-www-form-urlencoded")))
                .WithMetadata(new ExplicitOpenApiResponseAttribute(StatusCodes.Status200OK, [new(typeof(Response))]))
                .WithMetadata(new ExplicitOpenApiResponseAttribute(StatusCodes.Status400BadRequest, [new(typeof(ErrorResponse))]))
                .DisableAntiforgery();

            //builder.MapGet("/api/identity/connect/authorize", Authorize);
            //builder.MapPost("/api/identity/connect/authorize", Authorize);
        }
    }
}
