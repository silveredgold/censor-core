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
            var cropRect = result.Box.ToRectangle();
            var mask = new PathEffectMask(cropRect, result.SourceAngle.GetValueOrDefault(), padding);
            var extract = inputImage.Clone(x => {
                x.Pixelate(GetPixelSize(Math.Max(result.Box.Height, result.Box.Width), level));
                x.Crop((Rectangle)mask.GetBounds(inputImage));
            });
            
            return Task.FromResult<Action<IImageProcessingContext>?>(mask.GetMutation(extract));
        }

        private int GetPixelSize(int dimension, int level, int minimumSize = 5) {
            var inverted = 21-level;
            return Math.Max(minimumSize, Convert.ToInt32(Math.Round((dimension/(Math.Max(inverted, 5))*0.75)*1)));
        }

        public bool Supports(string censorType) => censorType.ToLower().Contains("pixel");
    }
}
