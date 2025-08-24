using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HomeInventory.shared.Models;

[PrimaryKey("UserId","InventoryId")]
public class InventoryMembers
{
    [Required]
    public Guid InventoryId { get; set; }

    [Required]
    public required string UserId { get; set; }
    public DateTimeOffset MemberSince { get; init; }
}