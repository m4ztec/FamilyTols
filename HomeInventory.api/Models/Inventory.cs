using System.ComponentModel.DataAnnotations;

namespace HomeInventory.api.Models;

public class Inventory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Name { get; set; }
    public string? Description { get; set; }

    [Required]
    public required string Onwer { get; set; }
}