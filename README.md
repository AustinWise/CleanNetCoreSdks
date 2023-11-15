# .NET SDK Cleaner

WARNING: this program might delete everything you hold dear, run at your own risk!

WARNING: this is a work in progress

TODO:

* Use `Microsoft.Deployment.DotNet.Releases` to get the mappings between SDKs and Runtime
* Delete all Windows specific logic
* Delete all version band logic, since Visual Studio for the Mac is going away
* Invoke `dotnet workloads clean` after cleaning SDKs
* Unify the definitions of paths for detecting installed versions and deleting installed version

## Old version for Windows for early .NET Core SDKs

In .NET Core 3 and later, the Windows installers model automatically uninstalls old SDKs
and there is no `NugetFallbackFolder`, so this program should not be needed on Windows.

For the Windows-specific version of this program that cleaned up the mess that was `NugetFallbackFolder`,
see the
[netcore2 branch](https://github.com/AustinWise/CleanNetCoreSdks/tree/netcore2).
