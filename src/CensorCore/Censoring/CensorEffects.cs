using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace CensorCore.Censoring {
    public static class CensorEffects {
        public static Action<IImageProcessingContext> GetMaskedBlurEffect(Image inputImage, Classification result, int padding, int level) {
            var cropRect = result.Box.ToRectangle();
            var mask = new PathEffectMask(cropRect, result.SourceAngle.GetValueOrDefault(), padding);
            var extract = inputImage.Clone(x =>
            {
                x.Crop((Rectangle)mask.GetBounds(inputImage));
                x.GaussianBlur(Math.Max(level, 10) * Math.Max(2.5F, (Math.Min(cropRect.Width, cropRect.Height) / 100)));
            });
            return mask.GetMutation(extract);
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