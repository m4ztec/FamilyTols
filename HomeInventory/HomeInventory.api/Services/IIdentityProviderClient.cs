namespace HomeInventory.api.Services;

public interface IIdentityProviderClient
{
    Task<UserProfile?> GetUserProfileAsync(string userId, string? bearerToken = null);
    Task<List<UserProfile>> GetAllUsersAsync();
}

public class UserProfile
{
    public string? UserId { get; set; }
    public string? DisplayName { get; set; }
}
