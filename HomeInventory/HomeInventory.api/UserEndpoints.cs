using HomeInventory.api.Dbcontext;
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
    }
}
