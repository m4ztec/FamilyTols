using HomeInventory.shared.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.api.Dbcontext
{
    public class HomeInventoryapiContext(DbContextOptions<HomeInventoryapiContext> options) : DbContext(options)
    {
        public DbSet<Inventory> Inventory { get; set; } = default!;
        public DbSet<InventoryMembers> InventoryMembers { get; set; } = default!;
        public DbSet<InventoryProducts> InventoryProducts { get; set; } = default!;
        public DbSet<Product> Product { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<Product>()
                .Property(x => x.Unit)
                .HasConversion<string>(); // readable enum
        }

        public override int SaveChanges()
        {
            ApplyAuditInfo();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditInfo();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAuditInfo()
        {
            var utcNow = DateTimeOffset.UtcNow;

            foreach (var entry in ChangeTracker.Entries<IAuditable>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.LastModifiedAt = utcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Entity.LastModifiedAt = utcNow;
                }
            }
        }
    }
}