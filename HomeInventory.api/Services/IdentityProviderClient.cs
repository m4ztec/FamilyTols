using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace HomeInventory.api.Services;

public class IdentityProviderClient : IIdentityProviderClient
{
    private readonly HttpClient _http;
    private readonly string? _userInfoEndpoint;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public IdentityProviderClient(HttpClient http, IConfiguration config, IHttpContextAccessor httpContextAccessor)
    {
        _http = http;
        _userInfoEndpoint = config["Keycloak:userinfo"];
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<UserProfile?> GetUserProfileAsync(string userId, string? bearerToken = null)
    {
        // if bearer token not provided, try to read from current HttpContext
        if (string.IsNullOrEmpty(bearerToken))
        {
            var header = _httpContextAccessor?.HttpContext?.Request?.Headers["Authorization"].FirstOrDefault();
            bearerToken = string.IsNullOrEmpty(header) ? null : header;
        }

        if (string.IsNullOrEmpty(_userInfoEndpoint) || string.IsNullOrEmpty(bearerToken))
            return null;

        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, _userInfoEndpoint);
            req.Headers.TryAddWithoutValidation("Authorization", bearerToken);

            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
                return null;

            var json = await resp.Content.ReadFromJsonAsync<JsonElement?>();
            if (json is null)
                return null;

            var sub = json.Value.TryGetProperty("sub", out var s) ? s.GetString() : null;
            var name = json.Value.TryGetProperty("name", out var n) ? n.GetString() : null;
            var preferred = json.Value.TryGetProperty("preferred_username", out var p) ? p.GetString() : null;
            var email = json.Value.TryGetProperty("email", out var e) ? e.GetString() : null;

            // ensure the returned subject matches requested userId
            if (sub is null || !string.Equals(sub, userId, StringComparison.OrdinalIgnoreCase))
                return null;

            var display = !string.IsNullOrEmpty(name) ? name : (!string.IsNullOrEmpty(preferred) ? preferred : (email ?? sub));

            return new UserProfile { UserId = sub, DisplayName = display };
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<UserProfile>?> GetAllUsersAsync()
    {
        if (string.IsNullOrEmpty(_userInfoEndpoint))
            return null;

        try
        {
            // Extract base URL from userinfo endpoint to construct admin API endpoint
            // e.g., "https://auth.m4ztec.com/realms/BlazorTest1/protocol/openid-connect/userinfo"
            // becomes "https://auth.m4ztec.com/admin/realms/BlazorTest1/users"
            var uri = new Uri(_userInfoEndpoint);
            var baseUrl = $"{uri.Scheme}://{uri.Host}";
            var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            if (pathSegments.Length < 2 || pathSegments[0] != "realms")
                return null;

            var realmName = pathSegments[1];
            var adminUsersUrl = $"{baseUrl}/admin/realms/{realmName}/users";

            var req = new HttpRequestMessage(HttpMethod.Get, adminUsersUrl);
            
            // Try to add bearer token from context if available
            var header = _httpContextAccessor?.HttpContext?.Request?.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(header))
            {
                req.Headers.TryAddWithoutValidation("Authorization", header);
            }

            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
                return null;

            var json = await resp.Content.ReadFromJsonAsync<JsonElement[]?>();
            if (json is null || json.Length == 0)
                return new List<UserProfile>();

            var users = new List<UserProfile>();
            foreach (var user in json)
            {
                var sub = user.TryGetProperty("id", out var s) ? s.GetString() : null;
                var name = user.TryGetProperty("firstName", out var fn) ? fn.GetString() : null;
                var lastName = user.TryGetProperty("lastName", out var ln) ? ln.GetString() : null;
                var username = user.TryGetProperty("username", out var u) ? u.GetString() : null;

                if (string.IsNullOrEmpty(sub))
                    continue;

                var displayName = !string.IsNullOrEmpty(name)
                    ? (!string.IsNullOrEmpty(lastName) ? $"{name} {lastName}" : name)
                    : (username ?? sub);

                users.Add(new UserProfile { UserId = sub, DisplayName = displayName });
            }

            return users;
        }
        catch
        {
            return null;
        }
    }
}
