using System.ComponentModel.DataAnnotations;

namespace HomeInventory.shared.Models;

public class Inventory
{
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();
    public string? Name { get; set; }
    public string? Description { get; set; }

    [Required]
    public required string Onwer { get; set; }

    [Required]
    public DateTimeOffset CreatedAt
    {
        get;
        init
        {
            field = DateTimeOffset.UtcNow;
        }
    }

    public DateTimeOffset LastModifiedAt { get; set; }
}