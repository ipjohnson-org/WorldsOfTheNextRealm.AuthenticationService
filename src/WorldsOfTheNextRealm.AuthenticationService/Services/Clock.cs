using DependencyModules.Runtime.Attributes;

namespace WorldsOfTheNextRealm.AuthenticationService.Services;

[SingletonService]
public class Clock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
