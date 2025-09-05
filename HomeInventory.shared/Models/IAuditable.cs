namespace HomeInventory.shared.Models;

public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset LastModifiedAt { get; set; }
}