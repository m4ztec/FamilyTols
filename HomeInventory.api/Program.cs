using HomeInventory.api;
using HomeInventory.api.Dbcontext;
using HomeInventory.api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
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
    options.TokenValidationParameters = new()
    {
        ValidateAudience = true,
        ValidateIssuer = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
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

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

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

app.Run();

internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
        {
            // Add the security scheme at the document level
            var securitySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "JWT",
                    Description = "Please provide a valid token",
                    Name = "Authorization",
                }
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = securitySchemes;

            // Apply it as a requirement for all operations
            foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations!))
            {
                operation.Value.Security ??= [];
                operation.Value.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            }
        }
    }
}