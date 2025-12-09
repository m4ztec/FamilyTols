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
    options.RequireHttpsMetadata = requireHttps;
    options.MetadataAddress = $"{keycloakAuthority}/.well-known/openid-configuration";
    
    options.TokenValidationParameters = new()
    {
        ValidateAudience = false,
        ValidateIssuer = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = keycloakAuthority,
        ValidateTokenReplay = false,
        ClockSkew = TimeSpan.FromMinutes(1), // Allow 1 minute clock skew
    };
    
    options.SaveToken = true;
    
    // Add event handler for debugging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"[AUTH FAILED] Exception: {context.Exception?.Message}");
            Console.WriteLine($"[AUTH FAILED] InnerException: {context.Exception?.InnerException?.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var claims = context.Principal?.Claims.Select(c => $"{c.Type}={c.Value}");
            Console.WriteLine($"[TOKEN VALIDATED] User: {context.Principal?.Identity?.Name}");
            Console.WriteLine($"[TOKEN VALIDATED] Claims: {string.Join(", ", claims ?? [])}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"[CHALLENGE] Error: {context.Error}");
            Console.WriteLine($"[CHALLENGE] Description: {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
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

app.UseHttpsRedirection();
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

app.UseStaticFiles(new StaticFileOptions {ServeUnknownFileTypes = true});
app.MapFallbackToFile("index.html");

app.Run();
