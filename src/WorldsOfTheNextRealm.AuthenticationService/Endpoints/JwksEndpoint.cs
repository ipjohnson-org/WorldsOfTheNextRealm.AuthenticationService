using System.Text.Json;
using Microsoft.Extensions.Logging;
using WorldsOfTheNextRealm.AuthenticationService.Services;

namespace WorldsOfTheNextRealm.AuthenticationService.Endpoints;

public static class JwksEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext httpContext,
        ISigningKeyService signingKeyService,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(typeof(JwksEndpoint).FullName!);
        logger.LogDebug("JWKS requested");
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

        logger.LogDebug("JWKS returning {KeyCount} keys", keys.Count);
        return Results.Json(new { keys }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = null
        });
    }
}
