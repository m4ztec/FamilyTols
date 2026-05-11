using HomeInventory.webui;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var baseAddress = builder.HostEnvironment.BaseAddress;
var apiAddress = builder.Configuration["ApiAddress"] ?? baseAddress;

// Add logging
builder.Services.AddLogging();

// Add HTTP client with authorization - use API authorization handler
builder.Services.AddHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri(apiAddress);
})
.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ExternalApi"));

await builder.Build().RunAsync();