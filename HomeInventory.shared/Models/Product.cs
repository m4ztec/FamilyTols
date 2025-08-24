using System.ComponentModel.DataAnnotations;

namespace HomeInventory.shared.Models;

public class Product
{
    [Key]
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int? SupposedPrice { get; set; }
}