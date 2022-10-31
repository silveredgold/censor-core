using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace CensorCore.Censoring {
    public static class CensorEffects {
        public static Action<IImageProcessingContext> GetMaskedBlurEffect(Image inputImage, Classification result, int padding, int level, float sizeFactor = 1F, float minimumLevel = 1F) {
            var cropRect = result.Box.ToRectangle();
            var mask = new PathEffectMask(cropRect, result.SourceAngle.GetValueOrDefault(), padding);
            var extract = inputImage.Clone(x =>
            {
                x.Crop((Rectangle)mask.GetBounds(inputImage));
                x.GaussianBlur((Math.Max(minimumLevel, level) * Math.Max(2.5F, (Math.Min(cropRect.Width, cropRect.Height) / 100)))*sizeFactor);
            });
            return mask.GetMutation(extract);
        }

        public static Action<IImageProcessingContext> GetMaskedPixelEffect(Image inputImage, Classification result, int padding, int level, float sizeFactor = 1F) {
            var cropRect = result.Box.ToRectangle();
            var mask = new PathEffectMask(cropRect, result.SourceAngle.GetValueOrDefault(), padding);
            var extract = inputImage.Clone(x => {
                x.Crop((Rectangle)mask.GetBounds(inputImage));
                x.Pixelate(GetPixelSize(Math.Max(inputImage.Height, inputImage.Width), level, sizeFactor));
            });
            return mask.GetMutation(extract);
        }

        private static int GetPixelSize(int dimension, int level, float scale, int minimumSize = 5) {
            var inverted = 21-level;
            return Math.Max(minimumSize, Convert.ToInt32(Math.Round((dimension/3F)/(Math.Max(inverted, 5))*0.75)*scale));
        }

        private static int GetPixelSize(int dimension, BoundingBox box, int level, int minimumSize = 5) {
            var inverted = 21-level;
            var scaledSize = Convert.ToInt32(Math.Round((Math.Min(box.Height, box.Width))/1F)/(Math.Max(inverted, 5))/2F);
            var imageSize = Math.Max(minimumSize, Convert.ToInt32(Math.Round(((dimension/3F)/(Math.Max(inverted, 5))*0.75)*1)));
            var ratioDiff = 1F-((float)Math.Min(box.Height, box.Width)/(float)Math.Max(box.Height, box.Width));
            var factor = 1/(1+(ratioDiff*(float)minimumSize));
            var final = Convert.ToInt32(Math.Round(imageSize * factor));
            // Console.WriteLine($"Normalizing size of {imageSize} using {factor}/{ratioDiff} to {final}");
            return final;
        }

        public static Action<IImageProcessingContext> GetBlackBarEffect(Image inputImage, Classification result, int level) {
            var rect = new RectangularPolygon(result.Box.X, result.Box.Y, result.Box.Width, result.Box.Height);
            var blackBrush = Brushes.Solid(Color.Black);
            var adjustFactor = (-(10 - (float)level) * 2) / 100;
            var adjBox = result.Box.ScaleBy(result.Box.Width * adjustFactor, result.Box.Height * adjustFactor);
            var drawOpts = result.SourceAngle != null
                ? new DrawingOptions() { Transform = Matrix3x2Extensions.CreateRotationDegrees(result.SourceAngle.Value, result.Box.GetCenter()) }
                : new DrawingOptions();
            return (x =>
                x.FillPolygon(drawOpts, blackBrush,
                    new PointF(result.Box.X, result.Box.Y),
                    new PointF(result.Box.X + result.Box.Width, result.Box.Y),
                    new PointF(result.Box.X + result.Box.Width, result.Box.Y + result.Box.Height),
                    new PointF(result.Box.X, result.Box.Y + result.Box.Height))
                );
        }
    }
}