namespace WorldsOfTheNextRealm.AuthenticationService.Endpoints;

public static class EndpointExtensions
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/register", RegisterEndpoint.Handle);
        app.MapPost("/auth/login", LoginEndpoint.Handle);
        app.MapPost("/auth/token/refresh", RefreshEndpoint.Handle);
        app.MapPost("/auth/token/revoke", RevokeEndpoint.Handle);
        app.MapGet("/auth/.well-known/jwks.json", JwksEndpoint.Handle);

        return app;
    }
}
