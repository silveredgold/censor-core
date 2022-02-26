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
            foreach (var match in image.Results)
            {
                var options = parser?.GetOptions(match, image) ?? this._parser?.GetOptions(match) ?? new ImageCensorOptions(nameof(BlurProvider)) { Level = 10 };
                var provider = this._providers.FirstOrDefault(p => p.Supports(options.CensorType ?? string.Empty));
                if (provider != null)
                {
                    var additionalLayer = await provider.CensorImage(img, match, options.CensorType, options.Level ?? 10);
                    if (additionalLayer != null)
                    {
                        img.Mutate(x =>
                        {
                            x.DrawImage(additionalLayer, match.Box.ToPoint(), 1);
                        });
                    }
                }
            }
            // await img.SaveAsPngAsync("./censored-result-2.png");
            using (var ms = new MemoryStream())
            {
                img.Save(ms, PngFormat.Instance);
                return new CensoredImage(ms.ToArray(), "image/png", img.ToBase64String(PngFormat.Instance));
            }
        }
    }




}
