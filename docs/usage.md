---
title: 'Developer Guide'
---

## Installing

Just install the required packages into your project with `dotnet add package`. You can find the packages available [on the NuGet Gallery](https://www.nuget.org/packages?q=censorcore). Most of the time, you will want at least `CensorCore` and `Censor.ModelLoader`, but if you want to include the REST API, you may also want `CensorCore.Web`.

> Check out the source for `CensorCore.Console` to see a rough implementation of what a CensorCore consumer looks like.

## Build

If you're feeling adventurous and want to build this for yourself, make sure you have the .NET 6 SDK installed and ready, then just run the following:

```bash
dotnet tool restore
dotnet cake
```

Note that the `Publish` target is the more "complete" build task, but may require you to include the correct model file in the repository to build the CLI.