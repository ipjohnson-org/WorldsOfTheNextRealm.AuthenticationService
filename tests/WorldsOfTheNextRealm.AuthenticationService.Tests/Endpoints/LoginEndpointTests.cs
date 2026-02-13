using DependencyModules.xUnit.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WorldsOfTheNextRealm.AuthenticationService.Configuration;
using WorldsOfTheNextRealm.AuthenticationService.Entities;
using WorldsOfTheNextRealm.AuthenticationService.Models;
using WorldsOfTheNextRealm.AuthenticationService.Services;
using WorldsOfTheNextRealm.AuthenticationService.Tests.TestHelpers;
using WorldsOfTheNextRealm.BackendCommon.DataStore;

namespace WorldsOfTheNextRealm.AuthenticationService.Tests.Endpoints;

public class LoginEndpointTests
{
    private static async Task RegisterPlayer(
        string email, string password,
        IEmailValidator emailValidator,
        IPasswordValidator passwordValidator,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings)
    {
        await AuthenticationService.Endpoints.RegisterEndpoint.Handle(
            new AuthRequest(email, password),
            emailValidator, passwordValidator, passwordHasher,
            tokenService, dataStore, clock, settings, new NullLoggerFactory());
    }

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public async Task Login_HappyPath_Returns200WithTokens(
        IEmailValidator emailValidator,
        IPasswordValidator passwordValidator,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IAccountLockoutService lockoutService,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings)
    {
        await RegisterPlayer("login@example.com", "StrongPass1",
            emailValidator, passwordValidator, passwordHasher,
            tokenService, dataStore, clock, settings);

        var result = await AuthenticationService.Endpoints.LoginEndpoint.Handle(
            new AuthRequest("login@example.com", "StrongPass1"),
            emailValidator, passwordHasher, tokenService,
            lockoutService, dataStore, clock, settings, new NullLoggerFactory());

        var httpResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(200, httpResult.StatusCode);
    }

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public async Task Login_WrongPassword_Returns401(
        IEmailValidator emailValidator,
        IPasswordValidator passwordValidator,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IAccountLockoutService lockoutService,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings)
    {
        await RegisterPlayer("wrongpw@example.com", "StrongPass1",
            emailValidator, passwordValidator, passwordHasher,
            tokenService, dataStore, clock, settings);

        var result = await AuthenticationService.Endpoints.LoginEndpoint.Handle(
            new AuthRequest("wrongpw@example.com", "WrongPassword1"),
            emailValidator, passwordHasher, tokenService,
            lockoutService, dataStore, clock, settings, new NullLoggerFactory());

        var httpResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(401, httpResult.StatusCode);
    }

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public async Task Login_NonexistentEmail_Returns401(
        IEmailValidator emailValidator,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IAccountLockoutService lockoutService,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings)
    {
        var result = await AuthenticationService.Endpoints.LoginEndpoint.Handle(
            new AuthRequest("nobody@example.com", "StrongPass1"),
            emailValidator, passwordHasher, tokenService,
            lockoutService, dataStore, clock, settings, new NullLoggerFactory());

        var httpResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(401, httpResult.StatusCode);
    }

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public async Task Login_FiveWrongPasswords_LocksAccount(
        IEmailValidator emailValidator,
        IPasswordValidator passwordValidator,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IAccountLockoutService lockoutService,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings)
    {
        await RegisterPlayer("lockout@example.com", "StrongPass1",
            emailValidator, passwordValidator, passwordHasher,
            tokenService, dataStore, clock, settings);

        // Fail 5 times
        for (var i = 0; i < 5; i++)
        {
            await AuthenticationService.Endpoints.LoginEndpoint.Handle(
                new AuthRequest("lockout@example.com", "WrongPassword1"),
                emailValidator, passwordHasher, tokenService,
                lockoutService, dataStore, clock, settings, new NullLoggerFactory());
        }

        // 6th attempt should be locked
        var result = await AuthenticationService.Endpoints.LoginEndpoint.Handle(
            new AuthRequest("lockout@example.com", "StrongPass1"),
            emailValidator, passwordHasher, tokenService,
            lockoutService, dataStore, clock, settings, new NullLoggerFactory());

        var httpResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(403, httpResult.StatusCode);
    }
}
