using System.Linq;
using System.Security.Claims;
using HomeInventory.api.Dbcontext;
using HomeInventory.api.Extensions;
using HomeInventory.shared.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.api;

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this IEndpointRouteBuilder routes)
    {
        var prefix = "/api/inventory";
        var group = routes.MapGroup(prefix).WithTags(nameof(Inventory)).RequireAuthorization();

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

        group.MapDelete("/{id}", (Func<HttpContext, Guid, HomeInventoryapiContext, Task<IResult>>)(async (http, id, db) =>
        {
            var userId = http.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return TypedResults.Forbid();

            var inventory = await db.Inventory.FirstOrDefaultAsync(i => i.Id == id);
            if (inventory is null)
                return TypedResults.NotFound();

            if (!string.Equals(inventory.Owner, userId, StringComparison.OrdinalIgnoreCase))
                return TypedResults.Forbid();

            var affected = await db.Inventory
                .Where(model => model.Id == id)
                .ExecuteDeleteAsync();
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        }))
        .WithName("DeleteInventory");

        group.MapGet("/{inventoryId}/products", async (Guid inventoryId, HomeInventoryapiContext db) =>
        {
            var inventoryProducts = await db.InventoryProducts
               .Where(ip => ip.InventoryId == inventoryId)
               .Select(ip => new InventoryProductDto
               {
                   ProductId = ip.ProductId,
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

        group.MapPut("/{inventoryId:guid}/products/{productId:guid}", async (Guid inventoryId, Guid productId, InventoryProductDto dto, HomeInventoryapiContext db) =>
        {
            var inventoryExists = await db.Inventory.AnyAsync(i => i.Id == inventoryId);
            if (!inventoryExists)
                return Results.NotFound($"Inventory {inventoryId} not found.");

            var product = await db.Product.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
                return Results.NotFound($"Product {productId} not found.");

            var inventoryProduct = await db.InventoryProducts.FirstOrDefaultAsync(ip =>
                ip.InventoryId == inventoryId && ip.ProductId == productId);
            if (inventoryProduct == null)
                return Results.NotFound($"Product {productId} is not in this inventory.");

            inventoryProduct.ExistingAmont = dto.ExistingAmount;
            inventoryProduct.DesiredAmont = dto.DesiredAmount;

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                InventoryId = inventoryId,
                product.Id,
                product.Name,
                product.SupposedPrice,
                dto.ExistingAmount,
                dto.DesiredAmount
            });
        })
        .WithName("UpdateInventoryProduct");

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

        group.MapPost("/{inventoryId:guid}/products/bulk-delete", async (Guid inventoryId, Guid[] productIds, HomeInventoryapiContext db) =>
        {
            if (productIds is null || productIds.Length == 0)
                return Results.BadRequest("No product ids provided.");

            var inventoryExists = await db.Inventory.AnyAsync(i => i.Id == inventoryId);
            if (!inventoryExists)
                return Results.NotFound($"Inventory {inventoryId} not found.");

            var products = await db.Product.Where(p => productIds.Contains(p.Id)).ToListAsync();
            var foundIds = products.Select(p => p.Id).ToHashSet();
            var missing = productIds.Where(id => !foundIds.Contains(id)).ToArray();

            var linked = await db.InventoryProducts
                .Where(ip => ip.InventoryId == inventoryId && productIds.Contains(ip.ProductId))
                .Select(ip => ip.ProductId)
                .ToArrayAsync();

            var notLinkedIds = productIds.Except(linked).ToArray();

            var deleted = await db.InventoryProducts
                .Where(ip => ip.InventoryId == inventoryId && productIds.Contains(ip.ProductId))
                .ExecuteDeleteAsync();

            // Map ids back to names for reporting
            var idToName = products.ToDictionary(p => p.Id, p => p.Name);
            var notLinkedNames = notLinkedIds.Where(id => idToName.ContainsKey(id)).Select(id => idToName[id]).ToArray();

            return Results.Ok(new
            {
                deletedCount = deleted,
                missing = missing,
                notLinked = notLinkedNames
            });
        })
        .WithName("BulkRemoveInventoryProducts");
    }

    public static void MapInventoryMembersEndpoints(this IEndpointRouteBuilder routes)
    {
        var inventoryMembersGroup = routes.MapGroup("/api/inventory").WithTags(nameof(InventoryMembers)).RequireAuthorization();

        inventoryMembersGroup.MapGet("/{inventoryId}/members", async (Guid inventoryId, HomeInventoryapiContext db) =>
        {
            var members = await db.InventoryMembers
                .Where(m => m.InventoryId == inventoryId)
                .ToListAsync();

            return TypedResults.Ok(members ?? []);
        })
        .WithName("GetInventoryMembers");

        inventoryMembersGroup.MapPost("/{inventoryId:guid}/members", (Func<HttpContext, Guid, AddMemberRequest, HomeInventoryapiContext, Task<IResult>>)(async (http, inventoryId, AddMemberRequest, db) =>
        {
            var userId = http.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return TypedResults.Forbid();

            if (string.IsNullOrWhiteSpace(AddMemberRequest.UserId))
                return TypedResults.BadRequest("UserId is required.");

            var inventory = await db.Inventory.FirstOrDefaultAsync(i => i.Id == inventoryId);
            if (inventory is null)
                return TypedResults.NotFound($"Inventory {inventoryId} not found.");

            if (!string.Equals(inventory.Owner, userId, StringComparison.OrdinalIgnoreCase))
                return TypedResults.Forbid();

            var alreadyMember = await db.InventoryMembers
                .AnyAsync(m => m.InventoryId == inventoryId && m.UserId == AddMemberRequest.UserId);
            if (alreadyMember)
                return TypedResults.Conflict($"User {AddMemberRequest.UserId} is already a member of this inventory.");

            var newMember = new InventoryMembers
            {
                InventoryId = inventoryId,
                UserId = AddMemberRequest.UserId,
                MemberSince = DateTimeOffset.UtcNow
            };

            db.InventoryMembers.Add(newMember);
            await db.SaveChangesAsync();

            return TypedResults.Created($"/api/inventory/{inventoryId}/members/{AddMemberRequest.UserId}", newMember);
        }))
        .WithName("AddInventoryMember");

        inventoryMembersGroup.MapDelete("/{inventoryId:guid}/members/{userId}", (Func<HttpContext, Guid, string, HomeInventoryapiContext, Task<IResult>>)(async (http, inventoryId, userId, db) =>
        {
            var callerId = http.GetUserId();
            if (string.IsNullOrEmpty(callerId))
                return TypedResults.Forbid();

            var inventory = await db.Inventory.FirstOrDefaultAsync(i => i.Id == inventoryId);
            if (inventory is null)
                return TypedResults.NotFound($"Inventory {inventoryId} not found.");

            // Prevent removing the owner via this endpoint
            if (string.Equals(inventory.Owner, userId, StringComparison.OrdinalIgnoreCase))
            {
                return TypedResults.BadRequest("Cannot remove the owner. Transfer ownership before removing.");
            }

            // Allow the caller to remove themselves (leave), or allow the owner to remove others
            if (!string.Equals(callerId, userId, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(inventory.Owner, callerId, StringComparison.OrdinalIgnoreCase))
            {
                return TypedResults.Forbid();
            }

            var affected = await db.InventoryMembers
                .Where(m => m.InventoryId == inventoryId && m.UserId == userId)
                .ExecuteDeleteAsync();

            return affected > 0 ? TypedResults.Ok() : TypedResults.NotFound();
        }))
        .WithName("RemoveInventoryMember");

        // Transfer ownership endpoint: sets Inventory.Owner to new user and ensures the new owner is a member
        inventoryMembersGroup.MapPut("/{inventoryId:guid}/owner", (Func<HttpContext, Guid, TransferOwnerRequest, HomeInventoryapiContext, Task<IResult>>)(async (http, inventoryId, req, db) =>
        {
            var callerId = http.GetUserId();
            if (string.IsNullOrEmpty(callerId))
                return TypedResults.Forbid();

            if (req is null || string.IsNullOrWhiteSpace(req.UserId))
                return TypedResults.BadRequest("UserId is required.");

            var inventory = await db.Inventory.FirstOrDefaultAsync(i => i.Id == inventoryId);
            if (inventory is null)
                return TypedResults.NotFound($"Inventory {inventoryId} not found.");

            // Only the current owner may transfer ownership
            if (!string.Equals(inventory.Owner, callerId, StringComparison.OrdinalIgnoreCase))
                return TypedResults.Forbid();

            // If the requested user is already owner, nothing to do
            if (string.Equals(inventory.Owner, req.UserId, StringComparison.OrdinalIgnoreCase))
                return TypedResults.Ok(inventory);

            // Ensure the new owner is present in InventoryMembers; if not, add them
            var isMember = await db.InventoryMembers.AnyAsync(m => m.InventoryId == inventoryId && m.UserId == req.UserId);
            if (!isMember)
            {
                db.InventoryMembers.Add(new InventoryMembers
                {
                    InventoryId = inventoryId,
                    UserId = req.UserId,
                    MemberSince = DateTimeOffset.UtcNow
                });
            }

            inventory.Owner = req.UserId;
            await db.SaveChangesAsync();

            return TypedResults.Ok(inventory);
        }))
        .WithName("TransferInventoryOwner");
    }

    public static void MapProductEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Product").WithTags(nameof(Product)).RequireAuthorization();

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

public class AddMemberRequest
{
    public string? UserId { get; set; }
}

public class TransferOwnerRequest
{
    public string? UserId { get; set; }
}
