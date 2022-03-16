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
        private readonly IEnumerable<ICensorTypeProvider> _providers;
        private readonly IResultParser? _parser;

        public ImageSharpCensoringProvider(IEnumerable<ICensorTypeProvider> providers, IResultParser parser) : this(providers) {
            this._parser = parser;
        }

        public ImageSharpCensoringProvider(IEnumerable<ICensorTypeProvider> providers) {
            this._providers = providers;
        }

        public async Task<CensoredImage> CensorImage(ImageResult image, IResultParser? parser = null) {
            var img = image.ImageData.SourceImage;
            var censorEffects = new Dictionary<int, List<Action<IImageProcessingContext>>>();
            var matches = image.Results.GroupBy(r => r.Label).ToList();
            var transformedMatches = new List<Classification>();
            foreach (var labelGroup in matches)
            {
                if (labelGroup.Count() > 1 && labelGroup.All(r => labelGroup.Any(l => r.Box.ToRectangle().IntersectsWith(l.Box.ToRectangle())))) {
                    foreach (var pair in labelGroup.Chunk(2))
                    {
                        if (pair.Count() < 2) {
                            transformedMatches.Add(pair.First());
                        }
                        var rectA = pair.First().Box.ToRectangle();
                        var rectB = pair.Last().Box.ToRectangle();
                        var rejections = new List<Predicate<Rectangle>>() {
                            r => r.Width > rectA.Width * 2F && r.Width > rectB.Width *2F,
                            r => r.Height > rectA.Height * 2F && r.Height > rectB.Height*2F
                        };
                        var unionRect = Rectangle.Union(rectA, rectB);
                        if (!rejections.All(r => r(unionRect))) {

                            transformedMatches.Add(
                                new Classification(
                                    new BoundingBox(unionRect.X, unionRect.Y, unionRect.X+unionRect.Width, unionRect.Y+unionRect.Height), Math.Max(pair.First().Confidence, pair.Last().Confidence), labelGroup.Key));
                        }
                    }
                } else {
                    transformedMatches.AddRange(labelGroup);
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
