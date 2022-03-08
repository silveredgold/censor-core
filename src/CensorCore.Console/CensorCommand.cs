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
            AnsiConsole.MarkupLine("[red]ERROR! [/] Could not load source image file!");
            AnsiConsole.MarkupLine($"Please ensure the '[grey]{imagePath}[/]' file exists and try again!");
            return 404;
        }
        var loader = new ModelLoaderBuilder()
            .AddDefaultPaths()
            // .SearchAssembly(System.Reflection.Assembly.GetEntryAssembly())
            .SearchAssembly(typeof(CensorCommand).Assembly)
            .Build();
        var model = await loader.GetLocalModel(settings.ModelFilePath);
        if (model == null && settings.AllowDownload) {
            AnsiConsole.MarkupLine($"Downloading model files from GitHub! This should only be required once.");
            try {
                var dl = await loader.DownloadModel(true);
                if (dl.HasValue) {
                    AnsiConsole.MarkupLine($"Downloaded [green]'{dl.Value.FileName}'[/] from GitHub, ready to use.");
                    model = dl.Value.ModelData;
                }
            }
            catch (Exception e) {
                AnsiConsole.MarkupLine("[darkred]WARN:[/] Failed to download model files from GitHub!");
                AnsiConsole.MarkupLine($"[grey]Exception: {e.Message} ({e.ToString()})");
            }
        }
        if (model == null) {
            AnsiConsole.MarkupLine("[red]ERROR! [/] Could not load model file!");
            AnsiConsole.MarkupLine("Download the model to a searched location, set [grey]--download[/] or pass [grey]--model-file[/] to specify a model file!");
            return 412;
        }

        var handler = new ImageSharpHandler(1000, 1000);
        AnsiConsole.MarkupLine("Preparing AI service and censoring components");
        var svc = Runtime.AIRuntime.CreateService(model, new ImageSharpHandler(1000,1000), settings.EnableAcceleration);
        if (settings.Verbose) {
            svc.Verbose = true;
        }
        var blur = new BlurProvider();
        var pixel = new PixelationProvider();
        var bars = new BlackBarProvider();
        var stickers = new StickerProvider(new EmptyAssetStore());
        var cens = new ImageSharpCensoringProvider(new ICensorTypeProvider[] { blur, pixel, bars, stickers });

        AnsiConsole.MarkupLine("Invoking model...");
        var result = await svc.RunModel(imagePath);
        if (result != null) {
            AnsiConsole.MarkupLine($"Successfully matched image with [blue]{result.Results.Count}[/] matches found.");
            if (result.Session != null && !svc.Verbose) {
                // if the service is in verbose mode, these timings will have already been output!
                var table = GetTable(result.Session);
                AnsiConsole.Write(table);
                // AnsiConsole.Write(GetChart(result.Session));
            }
            CensoredImage? censoredResult = null;
            IResultParser? parser = null;
            if (settings.Verbose) {
                AnsiConsole.Write(GetTree(result, imagePath));
            }
            if (settings.OverrideFilePath != null) {
                var oParser = new OverrideFileParser(settings.OverrideFilePath);
                var loaded = await oParser.Load();
                if (loaded && oParser._overrides != null) {
                    AnsiConsole.MarkupLine($"Successfully loaded [blue]{oParser._overrides.Keys.Count}[/] overrides from file.");
                    parser = oParser;
                } else {
                    AnsiConsole.MarkupLine($"Failed to load overrides from [grey]'{Path.GetFileName(settings.OverrideFilePath)}'[/]. Continuing with defaults...");
                }
            }
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            AnsiConsole.MarkupLine("Running censoring on image...");
            censoredResult = await cens.CensorImage(result, parser);
            timer.Stop();
            if (censoredResult != null) {
                AnsiConsole.MarkupLine($"Completed censoring image in [blue]{timer.Elapsed.TotalSeconds}[/]s!");
                var outputFilePath = settings.OutputPath
                    ?? Path.Combine(Environment.CurrentDirectory, $"{Path.GetFileNameWithoutExtension(imagePath)}_censored.{censoredResult.MimeType.Split("/").Last()}");
                await File.WriteAllBytesAsync(outputFilePath, censoredResult.ImageContents);
                AnsiConsole.MarkupLine($"Wrote censored image to '[grey]{outputFilePath}[/]'!");
                if (settings.CopyUrlToClipboard && !string.IsNullOrWhiteSpace(censoredResult.ImageDataUrl)) {
                    AnsiConsole.MarkupLine("Copying image data URL to clipboard...");
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

    private Table GetTable(SessionMetadata meta) {
        var totalTimeSpan = meta.ModelRunTime.TotalSeconds + (meta.ImageLoadTime ?? TimeSpan.FromSeconds(0)).TotalSeconds + (meta.TensorLoadTime ?? TimeSpan.FromSeconds(0)).TotalSeconds;
        var table = new Table().Border(TableBorder.Square).BorderColor(Color.Grey);
        table.AddColumn(new TableColumn("Task").LeftAligned().Footer("Total"));
        table.AddColumn(new TableColumn("Time").RightAligned().Footer(Math.Round(totalTimeSpan, 3).ToString() + "s"));
        if (meta.ImageLoadTime != null && meta.ImageLoadTime.Value is var imageLoadTime) {
            table.AddRow("Image Loading", $"{imageLoadTime.TotalSeconds.ToString()}s");
        }
        if (meta.TensorLoadTime != null && meta.TensorLoadTime.Value is var tensorLoadTime) {
            table.AddRow("Tensor Loading", tensorLoadTime.TotalSeconds.ToString() + "s");
        }
        table.AddRow("Model Execution", meta.ModelRunTime.TotalSeconds.ToString() + "s");
        return table;
    }

    private BarChart GetChart(SessionMetadata meta) {
        var totalTimeSpan = meta.ModelRunTime.TotalSeconds + (meta.ImageLoadTime ?? TimeSpan.FromSeconds(0)).TotalSeconds + (meta.TensorLoadTime ?? TimeSpan.FromSeconds(0)).TotalSeconds;
        var chart = new BarChart().Width(80).Label("[blue bold underline]Execution Time[/]")
        .LeftAlignLabel().HideValues();
        
        if (meta.ImageLoadTime != null && meta.ImageLoadTime.Value is var imageLoadTime) {
            var percentOfTime = (imageLoadTime.TotalSeconds / totalTimeSpan)*100;
            // chart.AddItem(new BarChartItem("Image Loading", 80*percentOfTime));
            chart.AddItem("Image Loading", 80*percentOfTime);
        }
        if (meta.TensorLoadTime != null && meta.TensorLoadTime.Value is var tensorLoadTime) {
            var percentOfTime = (tensorLoadTime.TotalSeconds / totalTimeSpan)*100;
            chart.AddItem("Tensor Loading", 80*percentOfTime);
        }
        var modelPercent = (meta.ModelRunTime.TotalSeconds / totalTimeSpan)*100;
        chart.AddItem("Model Execution", 80*modelPercent);
        return chart;
    }

    private Tree GetTree(ImageResult imageResult, string inputName) {
        var tree = new Tree(inputName);
        var grouped = imageResult.Results.GroupBy(r => r.Label);
        foreach (var group in grouped)
        {
            var groupNode = tree.AddNode(group.Key);
            foreach (var match in group.ToList())
            {
                groupNode.AddNode($"{match.Box.ToSize()} ({(match.Confidence*100).ToString("N2")}%)");
            }
        }
        return tree;
    }

    // private Spectre.Console.Rendering.IRenderable GetReport(SessionMetadata meta) {
    //     var table = new Table().Border(TableBorder.DoubleEdge).BorderColor(Color.Grey);
    //     var timers = GetTable(meta);
    //     table.AddColumn(new TableColumn("Execution Summary"));
    //     table.AddRow(GetTable(meta));
    //     var bar = new BarChart()
    //     .Width(timers.Width ?? 60)
    //     table.AddRow()
    // }

    static string CalculateMD5(byte[] model)
{
    using (var md5 = System.Security.Cryptography.MD5.Create())
    {
        var hash = md5.ComputeHash(model);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
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


