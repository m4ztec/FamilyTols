using HomeInventory.api;
using HomeInventory.api.Dbcontext;
using HomeInventory.api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var keycloakSection = builder.Configuration.GetSection("Keycloak");
var keycloakAuthority = keycloakSection["Authority"] ?? string.Empty;
var keycloakAudience = keycloakSection["Audience"] ?? string.Empty;
var requireHttps = true;
if (bool.TryParse(keycloakSection["RequireHttpsMetadata"], out var parsed))
    requireHttps = parsed;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.Authority = keycloakAuthority;
    options.Audience = keycloakAudience;
    //options.RequireHttpsMetadata = requireHttps;
    options.TokenValidationParameters = new()
    {
        ValidateAudience = true,
        ValidateIssuer = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
    };
    options.SaveToken = true;
});

builder.Services.AddAuthorization();

builder.Services.AddDbContext<HomeInventoryapiContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("HomeInventoryapiContext")
        ?? throw new InvalidOperationException("Connection string 'HomeInventoryapiContext' not found.")));

builder.Services.AddCors();

builder.Services.AddEndpointsApiExplorer();

// Register identity provider client (optional configuration via IdentityProvider section)
builder.Services.AddHttpClient<IIdentityProviderClient, IdentityProviderClient>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddOpenApi();

var app = builder.Build();

// Serve static files (Blazor WebAssembly app from wwwroot)
// This must be before the fallback route
app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = new List<string> { "index.html" }
});

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static files for 1 year (they have content hash in filename)
        ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    }
});

//app.UseHttpsRedirection();
app.UseCors(x => x
                .AllowAnyMethod()
                .AllowAnyHeader()
                .SetIsOriginAllowed(origin => true) // allow any origin
                .AllowCredentials()); // allow credentials

app.MapOpenApi();
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapInventoryEndpoints();
app.MapInventoryMembersEndpoints();
app.MapProductEndpoints();
app.MapUserEndpoints();

app.MapGet("/hi", () =>
{
    return TypedResults.Ok("authorized BABY!!!");
})
.WithName("test_01")
.RequireAuthorization();

// Fallback route for Blazor client-side routing
// Must be mapped AFTER all other routes to have the lowest priority
app.MapFallback(async context =>
{
    var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
    
    // Deny requests to known non-UI routes
    if (path.StartsWith("/api/") || 
        path.StartsWith("/swagger") ||
        path.StartsWith("/openapi") ||
        path.StartsWith("/scalar") ||
        path == "/hi")
    {
        context.Response.StatusCode = 404;
        return;
    }
    
    // Serve index.html for all other routes (Blazor client-side routing)
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "index.html"));
});

app.Run();
