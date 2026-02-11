namespace WorldsOfTheNextRealm.AuthenticationService.Models;

public record AuthResponse(string AccessToken, string RefreshToken, int ExpiresIn);
