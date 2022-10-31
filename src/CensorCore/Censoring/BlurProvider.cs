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
            var overrideScale = _globalOpts.ClassStrength.GetValueOrDefault(result.Label, 1F);
            var mutation = CensorEffects.GetMaskedBlurEffect(inputImage, result, padding, level, overrideScale);
            return Task.FromResult<Action<IImageProcessingContext>?>(mutation);
        }
    }
}
