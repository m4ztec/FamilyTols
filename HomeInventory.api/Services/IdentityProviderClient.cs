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
}
