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

        public Task<Image<Rgba32>?> CensorImage(Image<Rgba32> inputImage, Classification result, string method, int level)
        {
            var padding = inputImage.GetPadding(_globalOpts);
            var mask = new EffectMask(result.Box, padding);
            var extract = inputImage.Clone(x => {
                var cropRect = result.Box.ToRectangle().GetPadded(padding, inputImage);
                x.Crop(cropRect);
                x.Pixelate(GetPixelSize(Math.Max(result.Box.Height, result.Box.Width), level));
            });
            mask.DrawMaskedEffect(inputImage, extract);
            
            return Task.FromResult<Image<Rgba32>?>(null);
        }

        private int GetPixelSize(int dimension, int level) {
            var inverted = 21-level;
            return Convert.ToInt32(Math.Round((dimension/(Math.Max(inverted, 5))*0.75)*1));
        }

        public bool Supports(string censorType) => censorType.ToLower().Contains("pixel");
    }
}
