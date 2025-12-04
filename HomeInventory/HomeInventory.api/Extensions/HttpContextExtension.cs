using System.Security.Claims;

namespace HomeInventory.api.Extensions;

public static class HttpContextExtension
{
    extension(HttpContext http)
    {
        public string? GetUserId() =>
            http.Request.RouteValues["userid"]?.ToString()
            ?? http.User?.FindFirst("sub")?.Value
            ?? http.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value?.ToString();
    }
}
