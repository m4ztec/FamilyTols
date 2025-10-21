namespace HomeInventory.shared.Models;

public record InventoryProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public double ProductPrice { get; set; }
    public int ExistingAmount { get; set; }
    public int DesiredAmount { get; set; }
}
