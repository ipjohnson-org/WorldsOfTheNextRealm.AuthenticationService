using System.Text.RegularExpressions;
using DependencyModules.Runtime.Attributes;

namespace WorldsOfTheNextRealm.AuthenticationService.Services;

[SingletonService]
public partial class EmailValidator : IEmailValidator
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    public (bool IsValid, string? NormalizedEmail, string? ErrorCode) Validate(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return (false, null, "invalid_email");
        }

        var normalized = email.Trim().ToLowerInvariant();

        if (normalized.Length > 254)
        {
            return (false, null, "invalid_email");
        }

        if (!EmailRegex().IsMatch(normalized))
        {
            return (false, null, "invalid_email");
        }

        return (true, normalized, null);
    }
}
