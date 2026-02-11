using DependencyModules.xUnit.NSubstitute;
using WorldsOfTheNextRealm.AuthenticationService;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
[assembly: AuthenticationServiceModule]
[assembly: NSubstituteSupport]
