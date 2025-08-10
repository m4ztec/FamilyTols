using HomeInventory.api.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.api.Data
{
    public class HomeInventoryapiContext(DbContextOptions<HomeInventoryapiContext> options) : DbContext(options)
    {
        public DbSet<Inventory> Inventory { get; set; } = default!;
        public DbSet<InventoryMembers> InventoryMembers { get; set; } = default!;
        public DbSet<InventoryProducts> InventoryProducts { get; set; } = default!;
        public DbSet<Product> Product { get; set; } = default!;
    }
}