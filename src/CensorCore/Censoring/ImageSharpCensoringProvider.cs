using System.Diagnostics;
using CensorCore.Censoring;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;



namespace CensorCore.Censoring
{
    public interface ICensoringProvider {
        Task<CensoredImage> CensorImage(ImageResult image, IResultParser? parser = null);
    }

    public class ImageSharpCensoringProvider : ICensoringProvider {
        private readonly IEnumerable<IResultsTransformer> _transformers;
        private readonly GlobalCensorOptions _options;
        private readonly IEnumerable<ICensorTypeProvider> _providers;
        private readonly IEnumerable<ICensoringMiddleware> _middlewares;
        private readonly IResultParser? _parser;

        public ImageSharpCensoringProvider(IEnumerable<ICensorTypeProvider> providers, IResultParser parser, GlobalCensorOptions? options = null, IEnumerable<IResultsTransformer>? transformers = null, IEnumerable<ICensoringMiddleware>? middlewares = null) : this(providers, options, transformers, middlewares) {
            _parser = parser;
        }

        public ImageSharpCensoringProvider(IEnumerable<ICensorTypeProvider> providers, GlobalCensorOptions? options = null, IEnumerable<IResultsTransformer>? transformers = null, IEnumerable<ICensoringMiddleware>? middlewares = null) {
            _transformers = transformers ?? new List<IResultsTransformer>();
            _options = options ?? new GlobalCensorOptions();
            _providers = providers;
            _middlewares = middlewares ?? new List<ICensoringMiddleware>();
        }

        public async Task<CensoredImage> CensorImage(ImageResult image, IResultParser? parser = null) {
            parser = parser ?? this._parser;
            var img = image.ImageData.SourceImage;
            var censorEffects = new Dictionary<int, List<Action<IImageProcessingContext>>>();
            // var matches = image.Results.GroupBy(r => r.Label).ToList();
            IEnumerable<Classification> transformedMatches = image.Results;

            foreach (var middleware in _middlewares)
            {
                await middleware.Prepare();
            }
            if (_transformers.Any() && (_options.AllowTransformers ?? true))
            {
                foreach (var transformer in _transformers)
                {
                    transformedMatches = transformer.TransformResults(transformedMatches, parser);
                }
            }
            if (_middlewares.Any()) {
                foreach (var middleware in _middlewares)
                {
                    try {
                        var additionalResults = await middleware.OnBeforeCensoring(image, parser, (i, ctx) => AddCensor(i, null, ctx));
                        if (additionalResults != null) {
                            transformedMatches = transformedMatches.Concat(additionalResults);
                        }
                    } catch {
                        //ignored
                    }
                }
            }
            var timer = new Stopwatch();
            foreach (var match in transformedMatches.OrderBy(r => r.Box.Width * r.Box.Height))
            {
                var options = parser?.GetOptions(match, image) ?? new ImageCensorOptions(nameof(BlurProvider)) { Level = 10 };
                var provider = this._providers.FirstOrDefault(p => p.Supports(options.CensorType ?? string.Empty));
                if (provider != null)
                {
                    timer.Restart();
                    var censorMutation = await provider.CensorImage(img, match, options.CensorType, options.Level ?? 10);
                    if (censorMutation != null)
                    {
                        AddCensor(provider.Layer, options, censorMutation);
                    }
                    timer.Stop();
                    if (timer.Elapsed.TotalSeconds > 1D) {
                        Console.WriteLine($"WARN: Censoring for {provider.GetType().Name} on {match.Label} took {timer.Elapsed.TotalSeconds}s!");
                    }
                }
            }
            if (censorEffects.Any()) {
                img.Mutate(x =>
                {
                    foreach (var (layer, effects) in censorEffects.OrderBy(k => k.Key))
                    {
                        foreach (var effect in effects)
                        {
                            effect?.Invoke(x);
                        }
                    }
                });
            }
            foreach (var middleware in _middlewares)
            {
                try {
                    await middleware.OnAfterCensoring(img, parser);
                } catch {
                    //ignored
                }
            }
            using (var ms = new MemoryStream())
            {
                if (image.ImageData.Format != null) {
                    var format = image.ImageData.Format;
                    img.Save(ms, format);
                    return new CensoredImage(ms.ToArray(), format.DefaultMimeType, img.ToBase64String(format));
                } else {
                    img.Save(ms, PngFormat.Instance);
                    return new CensoredImage(ms.ToArray(), "image/png", img.ToBase64String(PngFormat.Instance));
                }
                
            }
            void AddCensor(int layer, ImageCensorOptions? censor, Action<IImageProcessingContext> mutation) {
                if (censorEffects != null) {
                    if (censor?.CensorType != null && _options.LayerModifier.TryGetValue(censor.CensorType, out var mod)) {
                        layer += mod;
                    }
                    if (!censorEffects.ContainsKey(layer)) {
                        censorEffects[layer] = new List<Action<IImageProcessingContext>>();
                    }
                    censorEffects[layer].Add(mutation);
                }
            }
        }
    }




}
