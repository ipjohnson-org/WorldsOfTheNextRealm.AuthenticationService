using Amazon.CDK;
using WorldsOfTheNextRealm.BackendCommon.Cdk;

var app = new App();
var config = Config.ResolveEnvironment(app);

new LambdaApiStack(app, $"{config.Prefix}-AuthenticationService", new LambdaApiStackProps
{
    Env = config.Env,
    Prefix = config.Prefix,
    ServiceName = "WorldsOfTheNextRealm.AuthenticationService",
    PublishPath = "../src/WorldsOfTheNextRealm.AuthenticationService/bin/Release/net8.0/publish"
});

app.Synth();