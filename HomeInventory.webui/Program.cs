using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using HomeInventory.webui;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

const string ApiUri = "http://localhost:5196";

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient("ExternalApi",
      client => client.BaseAddress = new Uri(ApiUri ??  throw new Exception("Missing base address!")))
      .AddHttpMessageHandler(sp =>
      {
          var handler = sp.GetRequiredService<AuthorizationMessageHandler>()
          .ConfigureHandler([ApiUri]);
          return handler;
      });
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ExternalApi"));

builder.Services.AddOidcAuthentication(options =>
{
    // Configure your authentication provider options here.
    // For more information, see https://aka.ms/blazor-standalone-auth
    builder.Configuration.Bind("Auth0", options.ProviderOptions);
    options.ProviderOptions.ResponseType = "code";
    options.ProviderOptions.AdditionalProviderParameters.Add("Audience", "https://dev-iaan6kbpwcjhk4me.us.auth0.com/api/v2/");
    options.ProviderOptions.AdditionalProviderParameters.Add("scopes", "read:current_user update:current_user_metadata");
});

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

await builder.Build().RunAsync();