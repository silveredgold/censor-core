using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CensorCore.Censoring {
    public class BlurProvider : ICensorTypeProvider {
        private readonly GlobalCensorOptions _globalOpts;

        public BlurProvider(GlobalCensorOptions globalOpts) {
            this._globalOpts = globalOpts;
        }
        public BlurProvider() {
            _globalOpts = new GlobalCensorOptions();
        }
        public Task<Action<IImageProcessingContext>?> CensorImage(Image<Rgba32> inputImage, Classification result, string method, int level) {
            var padding = inputImage.GetPadding(_globalOpts);
            var cropRect = result.Box.ToRectangle();
            var mask = new PathEffectMask(cropRect, result.SourceAngle.GetValueOrDefault(), padding);
            var extract = inputImage.Clone(x =>
            {
                x.GaussianBlur(level * Math.Max(2.5F, (Math.Min(cropRect.Width, cropRect.Height) / 100)));
            });
            extract.Mutate(x => x.Crop((Rectangle)mask.GetBounds()));
            var mutation = mask.GetMutation(extract);
            return Task.FromResult<Action<IImageProcessingContext>?>(mutation);
        }
    }
}
