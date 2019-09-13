# .NET Core SDK Cleaner

Each time you install a new version of the .NET Core SDK, the old versions are left behind.
These can take up several gigabytes of space. This program removes SDKs which are no longer needed.
By default it leaves the latest version in each SDK version. It also does not delete any version
or version band installed by Visual Studio (2017 and higher). The are command line options
to control these behaviors.

## TODO

* Cleanup NuGetFallbackFolder
