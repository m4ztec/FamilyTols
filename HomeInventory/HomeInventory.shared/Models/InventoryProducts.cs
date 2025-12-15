using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeInventory.shared.Models;

[PrimaryKey(nameof(InventoryId),nameof(ProductId))]
public class InventoryProducts
{
    [ForeignKey(nameof(Inventory))]
    public Guid InventoryId { get; set; }

    [ForeignKey(nameof(Product))]
    public Guid ProductId { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "Only positive number allowed")]
    public double ExistingAmont { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "Only positive number allowed")]
    public double DesiredAmont { get; set; }

    public Inventory Inventory { get; set; } = null!;
    public Product Product { get; set; } = null!;
}