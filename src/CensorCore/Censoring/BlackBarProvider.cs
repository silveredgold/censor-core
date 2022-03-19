using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;



namespace CensorCore.Censoring {

    public class BlackBarProvider : ICensorTypeProvider {
        public Task<Action<IImageProcessingContext>?> CensorImage(Image<Rgba32> inputImage, Classification result, string method, int level) {
            return Task.FromResult<Action<IImageProcessingContext>?>(CensorEffects.GetBlackBarEffect(inputImage, result, level));
        }

        public int Layer => 10;

        public bool Supports(string censorType) => censorType.Contains("bars") || censorType == "bb" || censorType.Contains("blackb");
    }
}
