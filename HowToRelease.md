# How to Release

Run `git tag -a vX.Y.Z`, where X and Y match what is in `CleanDotNetSdks\version.json`.

Run `git push origin vX.Y.Z`.

Bump version in `CleanDotNetSdks\version.json`.

## TODO

Figure out how to get the patch version into or from the Git tag. Currently NBGV generates it's own
patch version based on Git commit height.
