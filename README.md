# CensorCore

A flexible and configurable framework for censoring images based on the NudeNet ML model. CensorCore is designed to be as flexible as possible and should be able to be integrated and adapted for any project that requires classifying or censoring images.

For an overview of the components check the `src/README` file.

> This project is an extremely early preview and should be treated as such! The API is likely change, not all functionality is available and stability is not-exactly-top-notch.

To be honest, this project was just borne out of having a few different ideas of ways to use NudeNet and the idea of redoing all the tedious AI work every time was horrifying.

### Build

If you're feeling adventurous and want to build this for yourself, make sure you have the .NET 6 SDK installed and ready, then just run the following:

```bash
dotnet tool restore
dotnet cake
```

Note that the `Publish` target is the more "complete" build task, but requires you include the correct model file in the repository to build the CLI.

### Credits

Full credit for the model goes to @bedapudi6799 (Bedapudi Praneeth) for the original NudeNet model without which this whole project would not be possible.