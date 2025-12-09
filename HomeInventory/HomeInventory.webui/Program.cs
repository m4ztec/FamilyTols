using HomeInventory.webui;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Get base address for API (same origin as Web UI)
var baseAddress = builder.HostEnvironment.BaseAddress;

// Load Keycloak settings from configuration (environment variables)
var keycloakAuthority = builder.Configuration["Keycloak:Authority"] ?? "https://auth.m4ztec.com/realms/BlazorTest1";
var keycloakClientId = builder.Configuration["Keycloak:ClientId"] ?? "blazor-client";
var keycloakScope = builder.Configuration["Keycloak:Scope"] ?? "blazor-api-scope";

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
    client.BaseAddress = new Uri(baseAddress);
})
.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ExternalApi"));

await builder.Build().RunAsync();