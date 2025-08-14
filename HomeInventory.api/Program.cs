using System.Net.Http.Headers;
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
    options.Audience = "https://dev-iaan6kbpwcjhk4me.us.auth0.com/api/v2/";
});

builder.Services.AddAuthorization();

builder.Services.AddDbContext<HomeInventoryapiContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("HomeInventoryapiContext")
        ?? throw new InvalidOperationException("Connection string 'HomeInventoryapiContext' not found.")));

builder.Services.AddCors();

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

app.UseCors(x => x
                .AllowAnyMethod()
                .AllowAnyHeader()
                .SetIsOriginAllowed(origin => true) // allow any origin
                .AllowCredentials()); // allow credentials

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

app.MapGet("/hi2", async () =>
{
    using var client = new HttpClient();
        
    var url = "https://dev-iaan6kbpwcjhk4me.us.auth0.com/oauth/token";

    var requestData = new[]
    {
        new KeyValuePair<string, string>("grant_type", "client_credentials"),
        new KeyValuePair<string, string>("client_id", "N2cL2ZcijNPbkAiFgP2KCNJdAe4TM68p"),
        new KeyValuePair<string, string>("client_secret", "ZRBAYE5C3UdkOgUznlQ_g9zY9CatJcILgKLDjXUi-M0lvpdgbWf4q3BG1ObKKEkz"),
        new KeyValuePair<string, string>("audience", "https://dev-iaan6kbpwcjhk4me.us.auth0.com/api/v2/")
    };

    var requestContent = new FormUrlEncodedContent(requestData);
    requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

    var response = await client.PostAsync(url, requestContent);
    var responseString = await response.Content.ReadAsStringAsync();

    return TypedResults.Ok(responseString);
})
.WithName("test_02")
.WithOpenApi()
.RequireAuthorization();

app.Run();