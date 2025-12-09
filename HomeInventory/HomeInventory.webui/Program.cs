using HomeInventory.webui;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// Use relative URL to the current host when deployed
// In development: http://localhost:8080 (local API)
// In production: uses the same host as the Web UI
const string ApiUri = "";

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Create base address for API (same origin as Web UI)
var baseAddress = builder.HostEnvironment.BaseAddress;

// Add HTTP client with authorization
builder.Services.AddHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri(baseAddress);
})
.AddHttpMessageHandler(sp =>
{
    var handler = sp.GetRequiredService<AuthorizationMessageHandler>();
    handler.ConfigureHandler(
        authorizedUrls: [baseAddress],
        scopes: ["openid", "profile", "email", "blazor-api-scope"]
    );
    return handler;
});

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ExternalApi"));

// Load Keycloak settings from configuration (environment variables)
var keycloakAuthority = builder.Configuration["Keycloak:Authority"] ?? "https://auth.m4ztec.com/realms/BlazorTest1";
var keycloakClientId = builder.Configuration["Keycloak:ClientId"] ?? "blazor-client";
var keycloakScope = builder.Configuration["Keycloak:Scope"] ?? "blazor-api-scope";

builder.Services.AddOidcAuthentication(options =>
{
    options.ProviderOptions.Authority = keycloakAuthority;
    options.ProviderOptions.ClientId = keycloakClientId;
    options.ProviderOptions.ResponseType = "code";
    options.ProviderOptions.DefaultScopes.Add(keycloakScope);
});

await builder.Build().RunAsync();

