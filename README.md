# .NET Core SDK Cleaner

WARNING: this program might delete everything you hold dear, run at your own risk!

In .NET Core 3 and later, the update model automatically uninstalls old SDKs
and there is no NugetFallbackFolder, so this program should not be needed.

Each time you install a new version of the .NET Core SDK 2, the old versions are left behind.
These can take up several gigabytes of space. This program removes SDKs which are no longer needed.
By default it leaves the latest version in each SDK version. It also does not delete any version
or version band installed by Visual Studio (2017 and higher). The are command line options
to control these behaviors.
