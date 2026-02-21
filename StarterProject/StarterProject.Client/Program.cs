using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using StarterProject.Client;
using StarterProject.Client.Extensions;
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

builder.Services.AddSharedServices();

builder.Services.AddHttpClient(Constants.DefaultHttpClientName, client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient(Constants.DefaultHttpClientName));

var app = builder.Build();

var js = app.Services.GetRequiredService<IJSRuntime>();
var culture = await js.InvokeAsync<string>("blazorCulture.get");

if (!string.IsNullOrWhiteSpace(culture))
{
    var ci = new CultureInfo(culture);
    CultureInfo.DefaultThreadCurrentCulture = ci;
    CultureInfo.DefaultThreadCurrentUICulture = ci;
}

await app.RunAsync();
