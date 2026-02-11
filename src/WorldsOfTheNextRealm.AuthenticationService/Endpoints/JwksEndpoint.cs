using System.Text.Json;
using WorldsOfTheNextRealm.AuthenticationService.Services;

namespace WorldsOfTheNextRealm.AuthenticationService.Endpoints;

public static class JwksEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext httpContext,
        ISigningKeyService signingKeyService)
    {
        var jwks = await signingKeyService.GetJwks();

        httpContext.Response.Headers.CacheControl = "public, max-age=3600";

        // Serialize JWKS to standard format
        var keys = jwks.Keys.Select(k => new Dictionary<string, string>
        {
            ["kty"] = k.Kty,
            ["use"] = k.Use ?? "sig",
            ["alg"] = k.Alg ?? "RS256",
            ["kid"] = k.Kid,
            ["n"] = k.N,
            ["e"] = k.E
        }).ToList();

        return Results.Json(new { keys }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = null
        });
    }
}
