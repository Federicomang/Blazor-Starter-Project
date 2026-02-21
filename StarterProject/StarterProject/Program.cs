using FluentValidation;
using Hangfire;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using NLog.Web;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Scalar.AspNetCore;
using StarterProject.Client;
using StarterProject.Client.Extensions;
using StarterProject.Client.Features;
using StarterProject.Client.Features.Identity;
using StarterProject.Database;
using StarterProject.Database.Entities;
using StarterProject.Extensions;
using StarterProject.Features;
using StarterProject.Infrastructure.Hangfire;
using StarterProject.Infrastructure.Localization;
using StarterProject.Middlewares;
using StarterProject.Middlewares.Transformers;
using StarterProject.Tools;
using StarterProject.Web;
using System.Reflection;
using static StarterProject.Extensions.HttpContextExtensions;

var builder = WebApplication.CreateBuilder(args);

var clientAssembly = typeof(Routes).Assembly;
var thisAssembly = typeof(App).Assembly;

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.Configure<IdentityOptions>(options =>
{
    options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
    options.ClaimsIdentity.EmailClaimType = OpenIddictConstants.Claims.Email;
    options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Name;
    options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;
});

builder.Services.AddIdentityCore<User>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders().AddSignInManager();


// Configura NLog leggendo direttamente da appsettings.json
//builder.Logging.ClearProviders();
//builder.Logging.AddNLog(builder.Configuration);
builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Warning); // trace o Warning se vuoi più filtrato
builder.Host.UseNLog(); // carica NLog leggendo appsettings.json

var smartScheme = "SmartScheme";
var proxyEnabled = builder.Configuration.GetValue<bool>("ProxyEnabled");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = smartScheme;
    options.DefaultChallengeScheme = smartScheme;
    options.DefaultSignInScheme = smartScheme;
}).AddPolicyScheme(smartScheme, "Auto select Cookie or Bearer", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        // 1) Se c'è Authorization: Bearer ... → usa Bearer
        var auth = context.Request.Headers.Authorization.ToString();
        string scheme;

        if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            scheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        }
        else
        {
            // 2) Se è una navigazione HTML o se esiste il cookie di Identity → usa Cookie
            var acceptsHtml = context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);

            var hasIdentityCookie = context.Request.Cookies.ContainsKey(".AspNetCore.Identity.Application"); // nome cookie di Identity

            if (acceptsHtml || hasIdentityCookie)
            {
                scheme = IdentityConstants.ApplicationScheme;
            }
            else
            {
                // 3) Fallback: API senza token → Bearer (così non fai redirect per le API)
                scheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            }
        }

        context.Items[HttpContextItems.DATA_PREFIX + nameof(HttpContextItems.AuthenticationScheme)] = scheme;
        return scheme;
    };
}).AddCookie(IdentityConstants.ApplicationScheme, options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/login";
    options.AccessDeniedPath = "/accessDenied";
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.HttpContext.IsBrowserRequest())
        {
            context.Response.Redirect(context.RedirectUri);
        }
        else
        {
            context.Response.Headers.Location = context.RedirectUri;
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.HttpContext.IsBrowserRequest())
        {
            context.Response.Redirect(context.RedirectUri);
        }
        else
        {
            context.Response.Headers.Location = context.RedirectUri;
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        }
        return Task.CompletedTask;
    };
});

// 2️⃣ Configura OpenIddict
builder.Services.AddOpenIddict()
    .AddCore(opt =>
    {
        opt.UseEntityFrameworkCore()
           .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(opt =>
    {
        opt.SetTokenEndpointUris(DoLogin.ApiPath);
        opt.SetAuthorizationEndpointUris("api/identity/authorize");
        opt.SetEndSessionEndpointUris(Logout.ApiPath);

        // === FLUSSI CONSENTITI ===
        opt.AllowPasswordFlow();
        opt.AllowClientCredentialsFlow();
        opt.AllowAuthorizationCodeFlow();
        opt.AllowRefreshTokenFlow();

        // === AGGIUNTA CUSTOM GRANT TYPE ===
        foreach (var field in typeof(CustomGrants).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if(field.FieldType == typeof(string))
            {
                var value = field.IsLiteral
                    ? field.GetRawConstantValue()
                    : field.IsInitOnly
                        ? field.GetValue(null)
                        : null;
                if(value != null && value is string customGrant)
                {
                    opt.AllowCustomFlow(customGrant);
                }
            }
        }

        // === TOKEN FORMATO REFERENCE ===
        opt.UseReferenceAccessTokens();     // <--- token revocabili
        opt.UseReferenceRefreshTokens();    // <--- refresh token referenziati

        // === SCOPES ===
        opt.RegisterScopes("api", OpenIddictConstants.Scopes.OfflineAccess);

        // === CLIENT RISERVATI ===
        opt.AcceptAnonymousClients(); // mantieni se hai SPA native

        // === CERTIFICATI ===
        if (builder.Environment.IsDevelopment())
        {
            opt.AddDevelopmentEncryptionCertificate();
            opt.AddDevelopmentSigningCertificate();
        }
        else
        {
            var certPath = builder.Configuration["OpenIddictConfiguration:CertificatePath"];
            var signCertificate = builder.Configuration["OpenIddictConfiguration:SigningCertificate"]!;
            var encryptCertificate = builder.Configuration["OpenIddictConfiguration:EncryptionCertificate"]!;
            if (string.IsNullOrEmpty(certPath))
            {
                opt.AddEncryptionCertificate(encryptCertificate);
                opt.AddSigningCertificate(signCertificate);
            }
            else
            {
                opt.AddEncryptionCertificate(CertificateTools.FindCertificate(certPath, encryptCertificate));
                opt.AddSigningCertificate(CertificateTools.FindCertificate(certPath, signCertificate));
            }
        }

        // === INTEGRAZIONE ASP.NET CORE ===
        opt.UseAspNetCore()
            .DisableTransportSecurityRequirement()
            .EnableTokenEndpointPassthrough()
            .EnableAuthorizationEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough();
    })
    .AddValidation(opt =>
    {
        opt.UseLocalServer();
        opt.UseSystemNetHttp();
        opt.UseAspNetCore();
    });

builder.Services.AddValidatorsFromAssemblies([thisAssembly, clientAssembly], includeInternalTypes: true);

builder.Services.AddAuthorization();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddSharedServices();

builder.Services.AddScoped<IServerFeatureService, ServerFeatureService>();
builder.Services.AddScoped<CustomAuthenticationMiddleware>();
builder.Services.AddScoped<DbContextSaveChangesInterceptor>();

if (proxyEnabled)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto |
            ForwardedHeaders.XForwardedHost;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

builder.Services.AddMemoryCache();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<AuthorizationTransformer>();
    options.AddOperationTransformer<AuthorizationTransformer>();
    options.AddOperationTransformer<ExplicitRequestBodyTransformer>();
    options.AddOperationTransformer<FeatureResponseBodyTransformer>();
    options.AddOperationTransformer<ExplicitResponseBodyTransformer>();
    options.CreateSchemaReferenceId = info =>
    {
        if (info.Type != null && info.Type.FullName!.EndsWith("+" + info.Type.Name))
        {
            return info.Type.FullName.Replace("+", "-");
        }
        return OpenApiOptions.CreateDefaultSchemaReferenceId(info);
    };
});

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
        .AddInterceptors(sp.GetRequiredService<DbContextSaveChangesInterceptor>());
    options.UseOpenIddict();
});

// Add Hangfire services.
builder.Services.AddHangfireInFeatures(builder.Configuration.GetConnectionString("DefaultConnection"));

var app = builder.Build();

if (proxyEnabled)
{
    app.UseForwardedHeaders();
}

app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

await app.InitDb();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = Constants.SupportedCultures,
    SupportedUICultures = Constants.SupportedCultures,
    RequestCultureProviders = [new HybridRequestCultureProvider()]
});

app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<CustomAuthenticationMiddleware>();
app.UseAuthorization();

app.UseAntiforgery();
app.UseMiddleware<CustomAntiforgeryValidation>();

app.UseHangfireDashboard();

//app.MapGet("/api/addRole", async (RoleManager<IdentityRole> roleManager) =>
//{
//    var role = new IdentityRole();
//    role.Name = "Operatore";
//    role.NormalizedName = "OPERATORE";

//    var result = await roleManager.CreateAsync(role);

//    if (result.Succeeded) return Results.Ok();

//    return Results.BadRequest();
//});


//app.MapGet("/api/addUserRole", async (UserManager<User> userManager) =>
//{
//    //var user = new User();
//    var user = await userManager.FindByEmailAsync("test@test.test");

//    var result = await userManager.AddToRoleAsync(user, "Operatore");

//    if (result.Succeeded) return Results.Ok();

//    return Results.BadRequest();
//});

app.UseFeatureEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(clientAssembly);

app.Run();
