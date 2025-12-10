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

// Load Keycloak settings from configuration (environment variables)
var keycloakAuthority = builder.Configuration["Keycloak:Authority"]
    ?? throw new InvalidOperationException("Connection string 'Keycloak:Authority' not found."); ;
var keycloakClientId = builder.Configuration["Keycloak:ClientId"]
    ?? throw new InvalidOperationException("Connection string 'Keycloak:ClientId' not found.");
var keycloakScope = builder.Configuration["Keycloak:Scope"]
    ?? throw new InvalidOperationException("Connection string 'Keycloak:Scope' not found.");

// Add OIDC Authentication
builder.Services.AddOidcAuthentication(options =>
{
    options.ProviderOptions.Authority = keycloakAuthority;
    options.ProviderOptions.ClientId = keycloakClientId;
    options.ProviderOptions.ResponseType = "code";
    options.ProviderOptions.DefaultScopes.Add(keycloakScope);
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
        authorizedUrls: new[] { apiAddress },
        scopes: new[] { keycloakScope }
    );
    return handler;
});

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ExternalApi"));

await builder.Build().RunAsync();