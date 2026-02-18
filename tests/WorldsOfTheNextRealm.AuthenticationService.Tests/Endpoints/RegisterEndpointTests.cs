using Microsoft.IdentityModel.JsonWebTokens;
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

public class RegisterEndpointTests
{
    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public async Task Register_HappyPath_Returns201WithTokens(
        IEmailValidator emailValidator,
        IPasswordValidator passwordValidator,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings)
    {
        var request = new AuthRequest("newplayer@example.com", "StrongPass1");

        var result = await AuthenticationService.Endpoints.RegisterEndpoint.Handle(
            request, emailValidator, passwordValidator, passwordHasher,
            tokenService, dataStore, clock, settings, new NullLoggerFactory());

        var httpResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(201, httpResult.StatusCode);

        // Verify email lookup was stored
        var emailDoc = await dataStore.Get<EmailLookupData>(
            settings.MainTableName, DataKeys.EmailKey("newplayer@example.com"));
        Assert.NotNull(emailDoc);
        Assert.Equal("newplayer@example.com", emailDoc!.Data.NormalizedEmail);
    }

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public async Task Register_DuplicateEmail_Returns409(
        IEmailValidator emailValidator,
        IPasswordValidator passwordValidator,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings)
    {
        var request = new AuthRequest("duplicate@example.com", "StrongPass1");

        // Register first time
        await AuthenticationService.Endpoints.RegisterEndpoint.Handle(
            request, emailValidator, passwordValidator, passwordHasher,
            tokenService, dataStore, clock, settings, new NullLoggerFactory());

        // Register again with same email
        var result = await AuthenticationService.Endpoints.RegisterEndpoint.Handle(
            request, emailValidator, passwordValidator, passwordHasher,
            tokenService, dataStore, clock, settings, new NullLoggerFactory());

        var httpResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(409, httpResult.StatusCode);
    }

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public async Task Register_InvalidEmail_Returns422(
        IEmailValidator emailValidator,
        IPasswordValidator passwordValidator,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings)
    {
        var request = new AuthRequest("notanemail", "StrongPass1");

        var result = await AuthenticationService.Endpoints.RegisterEndpoint.Handle(
            request, emailValidator, passwordValidator, passwordHasher,
            tokenService, dataStore, clock, settings, new NullLoggerFactory());

        var httpResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(422, httpResult.StatusCode);
    }

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public async Task Register_HappyPath_AccessTokenContainsAgentAndSidClaims(
        IEmailValidator emailValidator,
        IPasswordValidator passwordValidator,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings)
    {
        var request = new AuthRequest("claimstest@example.com", "StrongPass1");

        var result = await AuthenticationService.Endpoints.RegisterEndpoint.Handle(
            request, emailValidator, passwordValidator, passwordHasher,
            tokenService, dataStore, clock, settings, new NullLoggerFactory());

        var valueResult = Assert.IsAssignableFrom<IValueHttpResult>(result);
        var authResponse = Assert.IsType<AuthResponse>(valueResult.Value);

        var handler = new JsonWebTokenHandler();
        var jwt = handler.ReadJsonWebToken(authResponse.AccessToken);

        Assert.Equal("none", jwt.Claims.First(c => c.Type == "agent").Value);
        Assert.NotEmpty(jwt.Claims.First(c => c.Type == "sid").Value);
    }

    [ModuleTest]
    [AuthDynamoTest]
    [TestAuthSettings]
    public async Task Register_WeakPassword_Returns422(
        IEmailValidator emailValidator,
        IPasswordValidator passwordValidator,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IDataStore dataStore,
        IClock clock,
        AuthSettings settings)
    {
        var request = new AuthRequest("player@example.com", "weak");

        var result = await AuthenticationService.Endpoints.RegisterEndpoint.Handle(
            request, emailValidator, passwordValidator, passwordHasher,
            tokenService, dataStore, clock, settings, new NullLoggerFactory());

        var httpResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(422, httpResult.StatusCode);
    }
}
