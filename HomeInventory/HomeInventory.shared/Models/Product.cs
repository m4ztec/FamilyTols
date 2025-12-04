using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory.shared.Models;

[Index(nameof(Name), IsUnique = true)]
public class Product
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(255)]
    public required string Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public double SupposedPrice { get; set; }

    public ICollection<InventoryProducts> InventoryProducts { get; set; } = [];

}