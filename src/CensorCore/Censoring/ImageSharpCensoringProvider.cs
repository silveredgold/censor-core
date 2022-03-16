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
        private readonly IResultParser? _parser;

        public ImageSharpCensoringProvider(IEnumerable<ICensorTypeProvider> providers, IResultParser parser, GlobalCensorOptions? options = null, IEnumerable<IResultsTransformer>? transformers = null) : this(providers, options, transformers) {
            _parser = parser;
        }

        public ImageSharpCensoringProvider(IEnumerable<ICensorTypeProvider> providers, GlobalCensorOptions? options = null, IEnumerable<IResultsTransformer>? transformers = null) {
            _transformers = transformers ?? new List<IResultsTransformer>();
            _options = options ?? new GlobalCensorOptions();
            _providers = providers;
        }

        public async Task<CensoredImage> CensorImage(ImageResult image, IResultParser? parser = null) {
            var img = image.ImageData.SourceImage;
            var censorEffects = new Dictionary<int, List<Action<IImageProcessingContext>>>();
            // var matches = image.Results.GroupBy(r => r.Label).ToList();
            IEnumerable<Classification> transformedMatches = image.Results;
            if (_transformers.Any() && (_options.AllowTransformers ?? true))
            {
                foreach (var transformer in _transformers)
                {
                    transformedMatches = transformer.TransformResults(transformedMatches);
                }
            }
            foreach (var match in transformedMatches.OrderBy(r => r.Box.Width * r.Box.Height))
            {
                var options = parser?.GetOptions(match, image) ?? this._parser?.GetOptions(match) ?? new ImageCensorOptions(nameof(BlurProvider)) { Level = 10 };
                var provider = this._providers.FirstOrDefault(p => p.Supports(options.CensorType ?? string.Empty));
                if (provider != null)
                {
                    
                    var censorMutation = await provider.CensorImage(img, match, options.CensorType, options.Level ?? 10);
                    if (censorMutation != null)
                    {
                        if (!censorEffects.ContainsKey(provider.Layer)) {
                            censorEffects[provider.Layer] = new List<Action<IImageProcessingContext>>();
                        }
                        censorEffects[provider.Layer].Add(censorMutation);
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
            // await img.SaveAsPngAsync("./censored-result-2.png");
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
        }
    }




}
