using HomeInventory.api.Dbcontext;
using HomeInventory.shared.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.api;

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this IEndpointRouteBuilder routes)
    {
        var prefix = "/api/inventory";
        var group = routes.MapGroup(prefix).WithTags(nameof(Inventory)).WithOpenApi();

        group.MapGet("/{id}", async Task<Results<Ok<Inventory>, NotFound>> (Guid id, HomeInventoryapiContext db) =>
        {
            return await db.Inventory.AsNoTracking()
                .FirstOrDefaultAsync(model => model.Id == id)
                is Inventory model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound();
        })
        .WithName("GetInventoryById");

        group.MapPut("/", async Task<Results<Ok, NotFound>> (Inventory inventory, HomeInventoryapiContext db) =>
        {
            var affected = await db.Inventory
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.Id, inventory.Id)
                    .SetProperty(m => m.Name, inventory.Name)
                    .SetProperty(m => m.Description, inventory.Description)
                    .SetProperty(m => m.Owner, inventory.Owner)
                    );
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("UpdateInventory");

        group.MapPost("/", async Task<IResult> (Inventory inventory, HomeInventoryapiContext db) =>
        {
            db.Inventory.Add(inventory);

            db.InventoryMembers.Add(new InventoryMembers
            {
                UserId = inventory.Owner,
                InventoryId = inventory.Id,
                MemberSince = DateTimeOffset.UtcNow
            });

            var isCreated = await db.SaveChangesAsync();

            return isCreated > 0
            ? TypedResults.Created($"{prefix}/{inventory.Id}", inventory)
            : TypedResults.BadRequest();
        })
        .WithName("CreateInventory");

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound>> (Guid id, HomeInventoryapiContext db) =>
        {
            var affected = await db.Inventory
                .Where(model => model.Id == id)
                .ExecuteDeleteAsync();
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("DeleteInventory");

        group.MapGet("/{inventoryId}/products", async (Guid inventoryId, HomeInventoryapiContext db) =>
        {
            var inventoryProducts = await db.InventoryProducts
               .Where(ip => ip.InventoryId == inventoryId)
               .Select(ip => new InventoryProductDto
               {
                   ProductName = ip.Product.Name,
                   ProductPrice = ip.Product.SupposedPrice,
                   ExistingAmount = ip.ExistingAmont,
                   DesiredAmount = ip.DesiredAmont
               })
               .ToArrayAsync();

            return TypedResults.Ok(inventoryProducts ?? []);
        })
        .WithName("GetInventoryProductsById");
        
        group.MapPost("/{inventoryId:guid}/products", async (Guid inventoryId, InventoryProductDto dto, HomeInventoryapiContext db) =>
        {
            var inventoryExists = await db.Inventory.AnyAsync(i => i.Id == inventoryId);
            if (!inventoryExists)
                return Results.NotFound($"Inventory {inventoryId} not found.");

            var product = await db.Product.FirstOrDefaultAsync(p => p.Name == dto.ProductName);
            if (product == null)
                return Results.NotFound($"Product '{dto.ProductName}' not found.");

            bool alreadyLinked = await db.InventoryProducts.AnyAsync(ip =>
                ip.InventoryId == inventoryId && ip.ProductId == product.Id);
            if (alreadyLinked)
                return Results.Conflict($"Product '{dto.ProductName}' is already in this inventory.");

            var newItem = new InventoryProducts
            {
                InventoryId = inventoryId,
                ProductId = product.Id,
                ExistingAmont = dto.ExistingAmount,
                DesiredAmont = dto.DesiredAmount
            };

            db.InventoryProducts.Add(newItem);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/api/inventories/{inventoryId}/products/{product.Id}",
                new
                {
                    InventoryId = inventoryId,
                    product.Id,
                    dto.ProductName,
                    dto.ProductPrice,
                    dto.ExistingAmount,
                    dto.DesiredAmount
                });
        })
        .WithName("AddInventoryProduct");

        group.MapDelete("/{inventoryId:guid}/products/{productName}", async (Guid inventoryId, string productName, HomeInventoryapiContext db) =>
        {
            var inventoryExists = await db.Inventory.AnyAsync(i => i.Id == inventoryId);
            if (!inventoryExists)
                return Results.NotFound($"Inventory {inventoryId} not found.");

            var product = await db.Product.FirstOrDefaultAsync(p => p.Name == productName);
            if (product == null)
                return Results.NotFound($"Product '{productName}' not found.");

            var affected = await db.InventoryProducts
                .Where(ip => ip.InventoryId == inventoryId && ip.ProductId == product.Id)
                .ExecuteDeleteAsync();

            return affected == 1 ? Results.Ok() : Results.NotFound();
        })
        .WithName("RemoveInventoryProduct");
    }

    public static void MapInventoryMembersEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/users").WithTags(nameof(InventoryMembers));

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

    public static void MapProductEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Product").WithTags(nameof(Product)).WithOpenApi();

        group.MapGet("/", async (HomeInventoryapiContext db) =>
        {
            return await db.Product.ToListAsync() ?? [];
        })
        .WithName("GetAllProducts");

        group.MapGet("/{name}", async Task<Results<Ok<Product>, NotFound>> (string name, HomeInventoryapiContext db) =>
        {
            return await db.Product.AsNoTracking()
                .FirstOrDefaultAsync(model => model.Name == name)
                is Product model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound();
        })
        .WithName("GetProductById");

        group.MapPut("/{name}", async Task<Results<Ok, NotFound>> (string name, Product product, HomeInventoryapiContext db) =>
        {
            var affected = await db.Product
                .Where(model => model.Name == name)
                .ExecuteUpdateAsync(setters => setters
                  .SetProperty(m => m.Name, product.Name)
                  .SetProperty(m => m.Description, product.Description)
                  .SetProperty(m => m.SupposedPrice, product.SupposedPrice)
                  );
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("UpdateProduct");

        group.MapPost("/", async (Product product, HomeInventoryapiContext db) =>
        {
            db.Product.Add(product);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/Product/{product.Name}", product);
        })
        .WithName("CreateProduct");

        group.MapDelete("/{name}", async Task<Results<Ok, NotFound>> (string name, HomeInventoryapiContext db) =>
        {
            var affected = await db.Product
                .Where(model => model.Name == name)
                .ExecuteDeleteAsync();
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("DeleteProduct");
    }
}
