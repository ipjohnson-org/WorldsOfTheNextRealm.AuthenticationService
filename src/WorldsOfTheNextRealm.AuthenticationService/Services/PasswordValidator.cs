using DependencyModules.Runtime.Attributes;

namespace WorldsOfTheNextRealm.AuthenticationService.Services;

[SingletonService]
public class PasswordValidator : IPasswordValidator
{
    public (bool IsValid, string? ErrorCode) Validate(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return (false, "weak_password");
        }

        if (password.Length < 8 || password.Length > 128)
        {
            return (false, "weak_password");
        }

        var hasUpper = false;
        var hasLower = false;
        var hasDigit = false;

        foreach (var c in password)
        {
            if (char.IsUpper(c)) hasUpper = true;
            else if (char.IsLower(c)) hasLower = true;
            else if (char.IsDigit(c)) hasDigit = true;
        }

        if (!hasUpper || !hasLower || !hasDigit)
        {
            return (false, "weak_password");
        }

        return (true, null);
    }
}
