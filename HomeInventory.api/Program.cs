using HomeInventory.api;
using HomeInventory.api.dbContext;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.Authority = "https://dev-iaan6kbpwcjhk4me.us.auth0.com/";
    options.Audience = "https://localhost:7003";
});

builder.Services.AddAuthorization();

builder.Services.AddDbContext<HomeInventoryapiContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("HomeInventoryapiContext")
        ?? throw new InvalidOperationException("Connection string 'HomeInventoryapiContext' not found.")));

builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new()
    {
        In = ParameterLocation.Header,
        Description = "Please provide a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapInventoryEndpoints();
app.MapInventoryMembersEndpoints();
app.MapInventoryProductsEndpoints();
app.MapProductEndpoints();

app.MapGet("/hi", () =>
{
    return TypedResults.Ok("authorized BABY!!!");
})
.WithName("test_01")
.WithOpenApi()
.RequireAuthorization();

app.Run();