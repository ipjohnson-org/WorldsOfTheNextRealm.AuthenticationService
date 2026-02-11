namespace WorldsOfTheNextRealm.AuthenticationService.Services;

public interface IPasswordValidator
{
    (bool IsValid, string? ErrorCode) Validate(string? password);
}
