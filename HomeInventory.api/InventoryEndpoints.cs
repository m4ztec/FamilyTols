using HomeInventory.api.dbContext;
using HomeInventory.api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.api;

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Inventory").WithTags(nameof(Inventory));

        group.MapGet("/{id}", async Task<Results<Ok<Inventory>, NotFound>> (Guid id, HomeInventoryapiContext db) =>
        {
            return await db.Inventory.AsNoTracking()
                .FirstOrDefaultAsync(model => model.Id == id)
                is Inventory model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound();
        })
        .WithName("GetInventoryById")
        .WithOpenApi();

        group.MapPut("/", async Task<Results<Ok, NotFound>> (Inventory inventory, HomeInventoryapiContext db) =>
        {
            var affected = await db.Inventory
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.Id, inventory.Id)
                    .SetProperty(m => m.Name, inventory.Name)
                    .SetProperty(m => m.Description, inventory.Description)
                    .SetProperty(m => m.Onwer, inventory.Onwer)
                    );
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("UpdateInventory")
        .WithOpenApi();

        group.MapPost("/", async (Inventory inventory, HomeInventoryapiContext db) =>
        {
            db.Inventory.Add(inventory);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/Inventory/{inventory.Id}", inventory);
        })
        .WithName("CreateInventory")
        .WithOpenApi();

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound>> (Guid id, HomeInventoryapiContext db) =>
        {
            var affected = await db.Inventory
                .Where(model => model.Id == id)
                .ExecuteDeleteAsync();
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("DeleteInventory")
        .WithOpenApi();
    }

    public static void MapInventoryMembersEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/InventoryMembers").WithTags(nameof(InventoryMembers));

        group.MapGet("/users/{userid}", async (string userid, HomeInventoryapiContext db) =>
        {
            return await db.InventoryMembers.Where(model => model.UserId == userid).ToListAsync();
        })
        .WithName("GetUserInventories")
        .WithOpenApi();

        group.MapGet("/inventory/{InventoryId}", async (Guid InventoryId, HomeInventoryapiContext db) =>
        {
            return await db.InventoryMembers.Where(model => model.InventoryId == InventoryId).ToListAsync();
        })
        .WithName("GetInventoryUsers")
        .WithOpenApi();

        group.MapPost("/", async (InventoryMembers inventoryMembers, HomeInventoryapiContext db) =>
        {
            db.InventoryMembers.Add(inventoryMembers);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/InventoryMembers/{inventoryMembers.UserId}", inventoryMembers);
        })
        .WithName("CreateInventoryMembers")
        .WithOpenApi();

        group.MapDelete("/", async Task<Results<Ok, NotFound>> ([FromBody] InventoryMembers inventoryMembers, HomeInventoryapiContext db) =>
        {
            var affected = await db.InventoryMembers
                .Where(model => model.UserId == inventoryMembers.UserId)
                .ExecuteDeleteAsync();
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("DeleteInventoryMembers")
        .WithOpenApi();
    }

    public static void MapInventoryProductsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/InventoryProducts").WithTags(nameof(InventoryProducts));

        group.MapGet("/", async (HomeInventoryapiContext db) =>
        {
            return await db.InventoryProducts.ToListAsync();
        })
        .WithName("GetAllInventoryProducts")
        .WithOpenApi();

        group.MapGet("/{id}", async Task<Results<Ok<InventoryProducts>, NotFound>> (Guid inventoryid, HomeInventoryapiContext db) =>
        {
            return await db.InventoryProducts.AsNoTracking()
                .FirstOrDefaultAsync(model => model.InventoryId == inventoryid)
                is InventoryProducts model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound();
        })
        .WithName("GetInventoryProductsById")
        .WithOpenApi();

        group.MapPut("/{id}", async Task<Results<Ok, NotFound>> (Guid inventoryid, InventoryProducts inventoryProducts, HomeInventoryapiContext db) =>
        {
            var affected = await db.InventoryProducts
                .Where(model => model.InventoryId == inventoryid)
                .ExecuteUpdateAsync(setters => setters
                  .SetProperty(m => m.InventoryId, inventoryProducts.InventoryId)
                  .SetProperty(m => m.ProductName, inventoryProducts.ProductName)
                  .SetProperty(m => m.ExistingAmont, inventoryProducts.ExistingAmont)
                  .SetProperty(m => m.DesiredAmont, inventoryProducts.DesiredAmont)
                  );
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("UpdateInventoryProducts")
        .WithOpenApi();

        group.MapPost("/", async (InventoryProducts inventoryProducts, HomeInventoryapiContext db) =>
        {
            db.InventoryProducts.Add(inventoryProducts);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/InventoryProducts/{inventoryProducts.InventoryId}", inventoryProducts);
        })
        .WithName("CreateInventoryProducts")
        .WithOpenApi();

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound>> (Guid inventoryid, HomeInventoryapiContext db) =>
        {
            var affected = await db.InventoryProducts
                .Where(model => model.InventoryId == inventoryid)
                .ExecuteDeleteAsync();
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("DeleteInventoryProducts")
        .WithOpenApi();
    }
    public static void MapProductEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Product").WithTags(nameof(Product));

        group.MapGet("/", async (HomeInventoryapiContext db) =>
        {
            return await db.Product.ToListAsync();
        })
        .WithName("GetAllProducts")
        .WithOpenApi();

        group.MapGet("/{id}", async Task<Results<Ok<Product>, NotFound>> (string name, HomeInventoryapiContext db) =>
        {
            return await db.Product.AsNoTracking()
                .FirstOrDefaultAsync(model => model.Name == name)
                is Product model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound();
        })
        .WithName("GetProductById")
        .WithOpenApi();

        group.MapPut("/{id}", async Task<Results<Ok, NotFound>> (string name, Product product, HomeInventoryapiContext db) =>
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
        .WithName("UpdateProduct")
        .WithOpenApi();

        group.MapPost("/", async (Product product, HomeInventoryapiContext db) =>
        {
            db.Product.Add(product);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/Product/{product.Name}", product);
        })
        .WithName("CreateProduct")
        .WithOpenApi();

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound>> (string name, HomeInventoryapiContext db) =>
        {
            var affected = await db.Product
                .Where(model => model.Name == name)
                .ExecuteDeleteAsync();
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("DeleteProduct")
        .WithOpenApi();
    }
}
