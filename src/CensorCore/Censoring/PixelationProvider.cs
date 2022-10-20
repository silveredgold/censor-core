using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CensorCore.Censoring
{
    public class PixelationProvider : ICensorTypeProvider
    {
        private readonly GlobalCensorOptions _globalOpts = new GlobalCensorOptions();
        public PixelationProvider() { }
        public PixelationProvider(GlobalCensorOptions globalOpts)
        {
            this._globalOpts = globalOpts;
        }

        public Task<Action<IImageProcessingContext>?> CensorImage(Image<Rgba32> inputImage, Classification result, string method, int level)
        {
            var padding = inputImage.GetPadding(_globalOpts);
            var mutation = CensorEffects.GetMaskedPixelEffect(inputImage, result, padding, level);
            return Task.FromResult<Action<IImageProcessingContext>?>(mutation);
            // var cropRect = result.Box.ToRectangle();
            // var mask = new PathEffectMask(cropRect, result.SourceAngle.GetValueOrDefault(), padding);
            // var extract = inputImage.Clone(x => {
            //     x.Crop((Rectangle)mask.GetBounds(inputImage));
            //     x.Pixelate(GetPixelSize(Math.Max(inputImage.Height, inputImage.Width), level));
            // });
            
            // return Task.FromResult<Action<IImageProcessingContext>?>(mask.GetMutation(extract));
        }

        

        public bool Supports(string censorType) => censorType.ToLower().StartsWith("pixel");
    }
}
