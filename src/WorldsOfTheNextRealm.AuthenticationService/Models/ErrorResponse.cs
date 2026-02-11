namespace WorldsOfTheNextRealm.AuthenticationService.Models;

public record ErrorDetail(string Code, string Message);

public record ErrorResponse(ErrorDetail Error)
{
    public static ErrorResponse Create(string code, string message) =>
        new(new ErrorDetail(code, message));
}
