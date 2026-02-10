using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using StarterProject.Client;
using StarterProject.Client.Extensions;

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

await builder.Build().RunAsync();
