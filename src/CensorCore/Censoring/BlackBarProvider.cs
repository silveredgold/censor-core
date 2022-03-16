using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;



namespace CensorCore.Censoring {

    public class BlackBarProvider : ICensorTypeProvider {
        public async Task<Action<IImageProcessingContext>> CensorImage(Image<Rgba32> inputImage, Classification result, string method, int level) {
            var rect = new SixLabors.ImageSharp.Drawing.RectangularPolygon(result.Box.X, result.Box.Y, result.Box.Width, result.Box.Height);
            var blackBrush = Brushes.Solid(Color.Black);
            var adjustFactor = (-(10 - (float)level) * 2) / 100;
            var adjBox = result.Box.ScaleBy(result.Box.Width * adjustFactor, result.Box.Height * adjustFactor);
            return (x =>
            {
                x.FillPolygon(blackBrush,
                    new PointF(result.Box.X, result.Box.Y),
                    new PointF(result.Box.X + result.Box.Width, result.Box.Y),
                    new PointF(result.Box.X + result.Box.Width, result.Box.Y + result.Box.Height),
                    new PointF(result.Box.X, result.Box.Y + result.Box.Height));
            });
        }

        public int Layer => 10;

        public bool Supports(string censorType) => censorType.Contains("bars") || censorType == "bb" || censorType.Contains("blackb");
    }
}
