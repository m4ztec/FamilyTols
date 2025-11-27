using HomeInventory.api.Dbcontext;
using HomeInventory.api.Services;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.api;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/users").WithTags("Users").RequireAuthorization();
        
        group.MapGet("/{userid}/inventories", async (string userid, HomeInventoryapiContext db) =>
        {
            var hi = await db.InventoryMembers
                .Where(model => model.UserId == userid && model.Inventory != null)
                .Select(a => a.Inventory!)
                .ToArrayAsync();
            
            return TypedResults.Ok(hi ?? []);
        })
        .WithName("GetUserInventories");

        group.MapGet("/{userid}/profile", (Func<HttpContext, Task<IResult>>)(async http =>
        {
            var userid = http.Request.RouteValues["userid"]?.ToString();
            if (string.IsNullOrEmpty(userid))
                return TypedResults.BadRequest("userid missing");

            if (http.RequestServices.GetService(typeof(IIdentityProviderClient)) is not IIdentityProviderClient idp)
                return TypedResults.Problem("Identity provider client not available");

            var prof = await idp.GetUserProfileAsync(userid);
            if (prof is null)
                return TypedResults.NotFound();

            if (!string.Equals(prof.UserId, userid, StringComparison.OrdinalIgnoreCase))
                return TypedResults.Forbid();

            return TypedResults.Ok(new { prof.UserId, prof.DisplayName });
        }))
        .WithName("GetUserProfile");

        group.MapGet("/", (Func<HttpContext, Task<IResult>>)(async http =>
        {
            if (http.RequestServices.GetService(typeof(IIdentityProviderClient)) is not IIdentityProviderClient idp)
                return TypedResults.Problem("Identity provider client not available");

            var users = await idp.GetAllUsersAsync();
            if (users is null)
                return TypedResults.Ok(Array.Empty<object>());

            return TypedResults.Ok(users.Select(u => new { u.UserId, u.DisplayName }).ToList());
        }))
        .WithName("GetAllUsers");
    }
}
