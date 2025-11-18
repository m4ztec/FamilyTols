using HomeInventory.api.Dbcontext;
using HomeInventory.api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace HomeInventory.api;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/users").WithTags("Users").WithOpenApi();

        // Simple profile endpoint. In a real app this would call an identity provider
        // or a user table. For now it returns the id as DisplayName unless a profile
        // mapping is available in the DB (not present in current schema).
            group.MapGet("/{userid}/profile", (Func<HttpContext, Task<IResult>>)(async (HttpContext http) =>
            {
                var userid = http.Request.RouteValues["userid"]?.ToString();
                if (string.IsNullOrEmpty(userid))
                    return TypedResults.BadRequest<string>("userid missing");

                var idp = http.RequestServices.GetService(typeof(IIdentityProviderClient)) as IIdentityProviderClient;
                if (idp is null)
                    return TypedResults.Problem("Identity provider client not available");

                var prof = await idp.GetUserProfileAsync(userid);
                if (prof is null)
                    return TypedResults.NotFound();

                if (!string.Equals(prof.UserId, userid, StringComparison.OrdinalIgnoreCase))
                    return TypedResults.Forbid();

                return TypedResults.Ok(new { UserId = prof.UserId, DisplayName = prof.DisplayName });
            }))
            .WithName("GetUserProfile");
    }
}
