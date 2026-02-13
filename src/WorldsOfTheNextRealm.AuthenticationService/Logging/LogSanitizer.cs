namespace WorldsOfTheNextRealm.AuthenticationService.Logging;

public static class LogSanitizer
{
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return "***";
        }

        var atIndex = email.IndexOf('@');
        if (atIndex < 1)
        {
            return "***";
        }

        var localPart = email[..atIndex];
        var domain = email[atIndex..];
        var visibleChars = Math.Min(2, localPart.Length);

        return $"{localPart[..visibleChars]}***{domain}";
    }
}
