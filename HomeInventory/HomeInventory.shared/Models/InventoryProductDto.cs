namespace HomeInventory.shared.Models;

public record InventoryProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public double ProductPrice { get; set; }
    public double ExistingAmount { get; set; }
    public double DesiredAmount { get; set; }
    public PackageUnits Units { get; set; }
}
