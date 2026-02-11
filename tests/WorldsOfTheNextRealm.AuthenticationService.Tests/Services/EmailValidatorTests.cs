using DependencyModules.xUnit.Attributes;
using WorldsOfTheNextRealm.AuthenticationService.Services;

namespace WorldsOfTheNextRealm.AuthenticationService.Tests.Services;

public class EmailValidatorTests
{
    [ModuleTest]
    public void Validate_ValidEmail_ReturnsNormalized(IEmailValidator validator)
    {
        var (isValid, normalized, error) = validator.Validate("  Player@Example.COM  ");

        Assert.True(isValid);
        Assert.Equal("player@example.com", normalized);
        Assert.Null(error);
    }

    [ModuleTest]
    public void Validate_NullEmail_ReturnsInvalid(IEmailValidator validator)
    {
        var (isValid, _, errorCode) = validator.Validate(null);

        Assert.False(isValid);
        Assert.Equal("invalid_email", errorCode);
    }

    [ModuleTest]
    public void Validate_EmptyEmail_ReturnsInvalid(IEmailValidator validator)
    {
        var (isValid, _, errorCode) = validator.Validate("");

        Assert.False(isValid);
        Assert.Equal("invalid_email", errorCode);
    }

    [ModuleTest]
    public void Validate_NoAtSign_ReturnsInvalid(IEmailValidator validator)
    {
        var (isValid, _, errorCode) = validator.Validate("notanemail");

        Assert.False(isValid);
        Assert.Equal("invalid_email", errorCode);
    }

    [ModuleTest]
    public void Validate_NoDomain_ReturnsInvalid(IEmailValidator validator)
    {
        var (isValid, _, errorCode) = validator.Validate("user@");

        Assert.False(isValid);
        Assert.Equal("invalid_email", errorCode);
    }

    [ModuleTest]
    public void Validate_NoTld_ReturnsInvalid(IEmailValidator validator)
    {
        var (isValid, _, errorCode) = validator.Validate("user@domain");

        Assert.False(isValid);
        Assert.Equal("invalid_email", errorCode);
    }
}
