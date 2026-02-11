namespace WorldsOfTheNextRealm.AuthenticationService.Services;

public interface IEmailValidator
{
    (bool IsValid, string? NormalizedEmail, string? ErrorCode) Validate(string? email);
}
