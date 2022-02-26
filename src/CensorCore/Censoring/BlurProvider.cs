using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CensorCore.Censoring
{
    public class BlurProvider : ICensorTypeProvider
    {
        private readonly GlobalCensorOptions _globalOpts;

        public BlurProvider(GlobalCensorOptions globalOpts)
        {
            this._globalOpts = globalOpts;
        }
        public BlurProvider()
        {
            _globalOpts = new GlobalCensorOptions();
        }
        public Task<Image<Rgba32>?> CensorImage(Image<Rgba32> inputImage, Classification result, string method, int level)
        {
            var padding = inputImage.GetPadding(_globalOpts);
            var mask = new EffectMask(result.Box, padding);
            var extract = inputImage.Clone(x => {
                var cropRect = result.Box.ToRectangle().GetPadded(padding, inputImage);
                // System.Console.WriteLine($"H: {inputImage.Height} || W: {inputImage.Width}");
                // System.Console.WriteLine($"eY: {cropRect.Y+cropRect.Height} || W: {cropRect.X+cropRect.Width}");
                x.Crop(cropRect);
                x.GaussianBlur(level);
            });
            mask.DrawMaskedEffect(inputImage, extract);
            // var maskedCensor = mask.GetMaskedImage(extract);
            // inputImage.Mutate(x => {
            //         x.DrawImage(maskedCensor, new Point(Convert.ToInt32(result.Box.X-10), Convert.ToInt32(result.Box.Y-10)), 1);
            //     });
            return Task.FromResult<Image<Rgba32>?>(null);
        }
    }
}
