using HomeInventory.webui;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Get base address for API
var baseAddress = builder.HostEnvironment.BaseAddress;

// In development, use configured API endpoint from environment; otherwise use same origin
var apiAddress = builder.Configuration["ApiAddress"] ?? baseAddress;

// Load auth settings from configuration (environment variables)
var authAuthority = builder.Configuration["Auth:Authority"]
    ?? throw new InvalidOperationException("Connection string 'Auth:Authority' not found."); ;
var authClientId = builder.Configuration["Auth:ClientId"]
    ?? throw new InvalidOperationException("Connection string 'Auth:ClientId' not found.");
var apiAuthScope = builder.Configuration["Auth:ApiScope"]
    ?? throw new InvalidOperationException("Connection string 'Auth:ApiScope' not found.");

// Add OIDC Authentication
builder.Services.AddOidcAuthentication(options =>
{
    options.ProviderOptions.Authority = authAuthority;
    options.ProviderOptions.ClientId = authClientId;
    options.ProviderOptions.ResponseType = "code";
    options.ProviderOptions.DefaultScopes.Add(apiAuthScope);
});

// Add HTTP client with authorization - use OIDC authentication handler
builder.Services.AddHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri(apiAddress);
})
.AddHttpMessageHandler(sp =>
{
    var tokenProvider = sp.GetRequiredService<IAccessTokenProvider>();
    var navigationManager = sp.GetRequiredService<NavigationManager>();

    var handler = new AuthorizationMessageHandler(tokenProvider, navigationManager);
    handler.ConfigureHandler(
        authorizedUrls: [apiAddress],
        scopes: [apiAuthScope]
    );
    return handler;
});

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ExternalApi"));

await builder.Build().RunAsync();