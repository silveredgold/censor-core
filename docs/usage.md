---
title: 'Developer Guide'
---

### Build

If you're feeling adventurous and want to build this for yourself, make sure you have the .NET 6 SDK installed and ready, then just run the following:

```bash
dotnet tool restore
dotnet cake
```

Note that the `Publish` target is the more "complete" build task, but requires you include the correct model file in the repository to build the CLI.