using System.ComponentModel.DataAnnotations;

namespace HomeInventory.shared.Models;

public class Inventory : IAuditable
{
    [Key]
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }

    [Required]
    public required string Onwer { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset LastModifiedAt { get; set; }
    
    public ICollection<InventoryProducts> InventoryProducts { get; set; } = [];
}