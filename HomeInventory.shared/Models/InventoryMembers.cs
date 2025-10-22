using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeInventory.shared.Models;

[PrimaryKey(nameof(UserId), nameof(InventoryId))]
public class InventoryMembers
{
    [Required]
    [ForeignKey(nameof(Inventory))]
    public Guid InventoryId { get; set; }

    [Required]
    public required string UserId { get; set; }
    public DateTimeOffset MemberSince { get; init; }

    public Inventory? Inventory { get; set; }
}