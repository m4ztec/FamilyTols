using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeInventory.shared.Models;

public class Inventory
{
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();
    public string? Name { get; set; }
    public string? Description { get; set; }

    [Required]
    public required string Onwer { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTimeOffset CreatedAt
    {
        get;
        init
        {
            field = DateTimeOffset.UtcNow;
        }
    }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTimeOffset LastModifiedAt { get; set; }
}