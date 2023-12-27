# .NET SDK Cleaner

WARNING: this program might delete everything you hold dear, run at your own risk!

This program uninstalls .NET SDKs and runtimes on unix-like systems. It is most useful on macOS
where there is no package manager to uninstall old versions of SDKs when upgrading to new versions.

This program supports uninstalling .NET Core 3.0 and later SDKs and runtimes.

The policy for deciding which SDKs and runtimes to keep is simple: the latest version of each major
version is kept and all others are deleted. The dependency of SDK on runtime is also taken into account
when deciding which versions of runtimes to keep.

## TODO

* Consider invoking `dotnet workloads clean` after cleaning SDKs
* Unify the definitions of paths for detecting installed versions and deleting installed version
* Add more asserts to the `VersionMap` class that ensure this program's assumptions about the mappings
  between different versions of components is correct. Specifically that there is a one-to-one relationship
  between runtime versions and ASP.NET versions.
* Docs
* Detect that the current user does not have permission to delete files instead of just crashing.
  Perhaps automatically attempt to elevate permissions using `sudo`?
* Publish prebuilt binaries, maybe using an automated GitHub Actions build?

## Old version for Windows for early .NET Core SDKs

Versions of the .NET Core framework prior to version 3 had their own unique challenges. Specifcally
they contained a `NugetFallbackFolder` directory that would only ever grow in size.

Given that the last version of the SDK with this problem are out of support as of 2019, this program
does not currently support cleaning up these versions. For the Windows-specific version of this
program that cleaned up that mess, see the
[netcore2 branch](https://github.com/AustinWise/CleanNetCoreSdks/tree/netcore2).
