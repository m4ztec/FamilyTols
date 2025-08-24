using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HomeInventory.shared.Models;

[PrimaryKey(nameof(InventoryId),nameof(ProductName))]
public class InventoryProducts
{
    public Guid InventoryId { get; set; }
    public required string ProductName { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Only positive number allowed")]
    public int ExistingAmont { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Only positive number allowed")]
    public int DesiredAmont { get; set; }
}