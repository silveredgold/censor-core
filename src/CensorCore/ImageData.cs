using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace CensorCore
{
    public class ImageData {
        public ImageData(Image<Rgba32> srcImage)
        {
            SourceImage = srcImage;
        }
        internal Image<Rgba32> SourceImage { get; set; }
        internal Image<Rgba32>? SampledImage {get;set;}
        internal IImageFormat? Format {get;set;}
        public float ScaleFactor {get;set;} = 1;
    }
}
