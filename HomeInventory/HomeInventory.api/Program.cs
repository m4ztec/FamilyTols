using HomeInventory.api;
using HomeInventory.api.Dbcontext;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<HomeInventoryapiContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("HomeInventoryapiContext")
        ?? throw new InvalidOperationException("Connection string 'HomeInventoryapiContext' not found."))
        .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<HomeInventoryapiContext>();

builder.Services.AddIdentityServer()
    .AddApiAuthorization<IdentityUser, HomeInventoryapiContext>();

builder.Services.AddAuthorization();

builder.Services.AddCors();
builder.Services.AddRazorPages();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpContextAccessor();

builder.Services.AddOpenApi();

var app = builder.Build();

// Apply database migrations automatically
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<HomeInventoryapiContext>();
    try
    {
        Console.WriteLine("Applying database migrations...");
        dbContext.Database.Migrate();
        Console.WriteLine("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error applying migrations: {ex.Message}");
        throw;
    }
}

// Only redirect to HTTPS in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

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
app.UseIdentityServer();
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
