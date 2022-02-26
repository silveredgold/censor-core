using System.ComponentModel;
using CensorCore.Censoring;
using CensorCore.ModelLoader;
using Spectre.Console;
using Spectre.Console.Cli;
using TextCopy;

namespace CensorCore.Console;

public class CensorCommand : AsyncCommand<CensorCommand.CensorCommandSettings> {
    public override async Task<int> ExecuteAsync(CommandContext context, CensorCommandSettings settings) {
        var imagePath = settings.ImagePath == null ? null : Path.IsPathFullyQualified(settings.ImagePath) ? settings.ImagePath : Path.GetFullPath(settings.ImagePath);
        if (imagePath == null || !File.Exists(imagePath)) {
            AnsiConsole.WriteLine("[red]ERROR! [/] Could not load source image file!");
            AnsiConsole.WriteLine($"Please ensure the '[grey]{imagePath}[/]' file exists and try again!");
            return 404;
        }
        var loader = new ModelLoaderBuilder()
            .AddDefaultPaths()
            .SearchAssembly(System.Reflection.Assembly.GetEntryAssembly())
            .Build();
        var model = await loader.GetLocalModel(settings.ModelFilePath);
        if (model == null && settings.AllowDownload) {
            try {
                model = await loader.DownloadModel();
            }
            catch {
                AnsiConsole.WriteLine("[darkred]WARN:[/] Failed to download model files from GitHub!");
            }
        }
        if (model == null) {
            AnsiConsole.WriteLine("[red]ERROR! [/] Could not load model file!");
            AnsiConsole.WriteLine("Download the model to a searched location, set [grey]--allow-download[/] or pass [grey]--model-file[/] to specify a model file!");
            return 412;
        }

        var handler = new ImageSharpHandler(1000, 1000);
        AnsiConsole.WriteLine("Preparing AI service and censoring components");
        var svc = AIService.Create(model, new ImageSharpHandler(1000, 1000), settings.EnableAcceleration);
        var blur = new BlurProvider();
        var pixel = new PixelationProvider();
        var bars = new BlackBarProvider();
        var stickers = new StickerProvider(new EmptyAssetStore());
        var cens = new ImageSharpCensoringProvider(new ICensorTypeProvider[] { blur, pixel, bars, stickers });

        AnsiConsole.WriteLine("Invoking model...");
        var result = await svc.RunModel(imagePath);
        if (result != null) {
            AnsiConsole.WriteLine($"Successfully matched image with [skyblue]{result.Results.Count}[/] matches found.");
            CensoredImage? censoredResult = null;
            IResultParser? parser = null;
            if (settings.OverrideFilePath != null) {
                var oParser = new OverrideFileParser(settings.OverrideFilePath);
                var loaded = await oParser.Load();
                if (loaded && oParser._overrides != null) {
                    AnsiConsole.WriteLine($"Successfully loaded [skyblue]{oParser._overrides.Keys.Count}[/] overrides from file.");
                    parser = oParser;
                } else {
                    AnsiConsole.WriteLine($"Failed to load overrides from [grey]'{Path.GetFileName(settings.OverrideFilePath)}'[/]. Continuing with defaults...");
                }
            }
            AnsiConsole.WriteLine("Running censoring on image...");
            censoredResult = await cens.CensorImage(result, parser);
            if (censoredResult != null) {
                AnsiConsole.WriteLine("Censoring completed on image!");
                var outputFilePath = settings.OutputPath
                    ?? Path.Combine(Environment.CurrentDirectory, $"{Path.GetFileNameWithoutExtension(imagePath)}_censored.{censoredResult.MimeType.Split("/").Last()}");
                await File.WriteAllBytesAsync(outputFilePath, censoredResult.ImageContents);
                AnsiConsole.WriteLine($"Wrote censored image to '[grey]{outputFilePath}[/]'!");
                if (settings.CopyUrlToClipboard && !string.IsNullOrWhiteSpace(censoredResult.ImageDataUrl)) {
                    AnsiConsole.WriteLine("Copying image data URL to clipboard...");
                    await ClipboardService.SetTextAsync(censoredResult.ImageDataUrl);
                }
            }
            return 0;
        }
        return 500;
    }

    public class CensorCommandSettings : CensoringSettings {
        [CommandArgument(0, "[imagePath]")]
        public string? ImagePath { get; set; }

        [CommandOption("-o|--output-file")]
        public string? OutputPath { get; set; }

        [CommandOption("-u|--copy-url")]
        [DefaultValue(false)]
        public bool CopyUrlToClipboard { get; set; }

        [CommandOption("--override")]
        [Description("An override file to control the censoring applied to matches in the source image.")]
        public string? OverrideFilePath { get; set; }
    }

}

public class CensoringSettings : CommandSettings {
    [CommandOption("-m|--model-file")]
    public string? ModelFilePath { get; set; }

    [CommandOption("--verbose")]
    [DefaultValue(false)]
    public bool Verbose { get; set; }

    [CommandOption("--download")]
    [Description("Allow CensorCore to download the AI model if not found locally.")]
    [DefaultValue(false)]
    public bool AllowDownload { get; set; }

    [CommandOption("--enable-acceleration")]
    [Description("Attempts to enable DirectML hardware acceleration. Use with caution.")]
    [DefaultValue(false)]
    public bool EnableAcceleration { get; set; }
}
