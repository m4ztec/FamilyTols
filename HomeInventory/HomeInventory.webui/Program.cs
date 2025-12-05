using HomeInventory.webui;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// Use relative URL to the current host when deployed
// In development: http://localhost:5196 (local API)
// In production: uses the same host as the Web UI
const string ApiUri = "";

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient("ExternalApi",
      client => client.BaseAddress = new Uri(new Uri(builder.HostEnvironment.BaseAddress), ApiUri ?? "/"))
      .AddHttpMessageHandler(sp =>
      {
          var handler = sp.GetRequiredService<AuthorizationMessageHandler>()
          .ConfigureHandler([new Uri(new Uri(builder.HostEnvironment.BaseAddress), ApiUri ?? "/").ToString()]);
          return handler;
      });
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ExternalApi"));

builder.Services.AddOidcAuthentication(options =>
{
    options.ProviderOptions.Authority = "https://auth.m4ztec.com/realms/BlazorTest1";
    options.ProviderOptions.ClientId = "blazor-client";
    options.ProviderOptions.ResponseType = "code";
    options.ProviderOptions.DefaultScopes.Add("blazor-api-scope");
});

await builder.Build().RunAsync();

