namespace HomeInventory.api.Dbcontext;

public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset LastModified { get; set; }
}