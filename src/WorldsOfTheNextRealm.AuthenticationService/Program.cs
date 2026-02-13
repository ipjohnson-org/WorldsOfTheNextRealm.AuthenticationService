using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using DependencyModules.Runtime;
using WorldsOfTheNextRealm.AuthenticationService;
using WorldsOfTheNextRealm.AuthenticationService.Configuration;
using WorldsOfTheNextRealm.AuthenticationService.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
});
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Logging.AddFilter("WorldsOfTheNextRealm", LogLevel.Debug);

builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register AuthSettings from configuration
var authSettings = builder.Configuration.GetSection("AuthSettings").Get<AuthSettings>() ?? new AuthSettings();

// Fetch master encryption key from Secrets Manager if configured
if (!string.IsNullOrEmpty(authSettings.MasterEncryptionKeySecretId))
{
    using var smClient = new AmazonSecretsManagerClient();
    var response = await smClient.GetSecretValueAsync(new GetSecretValueRequest
    {
        SecretId = authSettings.MasterEncryptionKeySecretId
    });
    authSettings = authSettings with { MasterEncryptionKey = response.SecretString };
}

builder.Services.AddSingleton(authSettings);

// Register DependencyModules
builder.Services.AddModule<AuthenticationServiceModule>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapAuthEndpoints();

app.Run();
