using DependencyModules.xUnit.Attributes;
using WorldsOfTheNextRealm.AuthenticationService.Services;

namespace WorldsOfTheNextRealm.AuthenticationService.Tests.Services;

public class PasswordValidatorTests
{
    [ModuleTest]
    public void Validate_ValidPassword_ReturnsValid(IPasswordValidator validator)
    {
        var (isValid, error) = validator.Validate("StrongPass1");

        Assert.True(isValid);
        Assert.Null(error);
    }

    [ModuleTest]
    public void Validate_TooShort_ReturnsWeakPassword(IPasswordValidator validator)
    {
        var (isValid, error) = validator.Validate("Ab1");

        Assert.False(isValid);
        Assert.Equal("weak_password", error);
    }

    [ModuleTest]
    public void Validate_TooLong_ReturnsWeakPassword(IPasswordValidator validator)
    {
        var password = new string('A', 127) + "a1";

        var (isValid, error) = validator.Validate(password);

        Assert.False(isValid);
        Assert.Equal("weak_password", error);
    }

    [ModuleTest]
    public void Validate_NoUppercase_ReturnsWeakPassword(IPasswordValidator validator)
    {
        var (isValid, error) = validator.Validate("lowercase1");

        Assert.False(isValid);
        Assert.Equal("weak_password", error);
    }

    [ModuleTest]
    public void Validate_NoLowercase_ReturnsWeakPassword(IPasswordValidator validator)
    {
        var (isValid, error) = validator.Validate("UPPERCASE1");

        Assert.False(isValid);
        Assert.Equal("weak_password", error);
    }

    [ModuleTest]
    public void Validate_NoDigit_ReturnsWeakPassword(IPasswordValidator validator)
    {
        var (isValid, error) = validator.Validate("NoDigitsHere");

        Assert.False(isValid);
        Assert.Equal("weak_password", error);
    }

    [ModuleTest]
    public void Validate_Null_ReturnsWeakPassword(IPasswordValidator validator)
    {
        var (isValid, error) = validator.Validate(null);

        Assert.False(isValid);
        Assert.Equal("weak_password", error);
    }

    [ModuleTest]
    public void Validate_ExactMinLength_ReturnsValid(IPasswordValidator validator)
    {
        var (isValid, _) = validator.Validate("Abcdef1x");

        Assert.True(isValid);
    }
}
