using DependencyModules.xUnit.Attributes.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorldsOfTheNextRealm.AuthenticationService.Configuration;
using Xunit.v3;

namespace WorldsOfTheNextRealm.AuthenticationService.Tests.TestHelpers;

public class TestAuthSettingsAttribute : Attribute, ITestStartupAttribute
{
    // 32-byte base64-encoded key for AES-256-GCM
    public const string TestMasterKey = "dGVzdC1tYXN0ZXIta2V5LTMyYnl0ZXMh";

    public void SetupServiceCollection(IXunitTestMethod testMethod, IServiceCollection serviceCollection)
    {
        var settings = new AuthSettings
        {
            MainTableName = AuthDynamoTestAttribute.MainTable,
            CredentialsTableName = AuthDynamoTestAttribute.CredentialsTable,
            SigningKeysTableName = AuthDynamoTestAttribute.SigningKeysTable,
            MasterEncryptionKey = TestMasterKey,
            AccessTokenLifetimeSeconds = 21600,
            RefreshTokenLifetimeSeconds = 5_184_000
        };

        serviceCollection.AddSingleton(settings);
        serviceCollection.AddLogging();
    }

    public Task StartupAsync(IXunitTestMethod testMethod, IServiceProvider serviceProvider)
    {
        return Task.CompletedTask;
    }
}
