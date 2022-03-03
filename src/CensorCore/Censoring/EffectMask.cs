using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;



namespace CensorCore.Censoring
{
    /// <summary>
    /// A helper class for blending censor methods with the surrounding image. 
    /// </summary>
    public class EffectMask {
        private Rectangle _box;
        private readonly int _padding;
        private readonly GraphicsOptions _options;

        /// <summary>
        /// Create an instance of the Mask helper for the given bounding box, optionally with padding.
        /// </summary>
        /// <param name="box">The bounding box used to create the mask.</param>
        /// <param name="padding">An optional padding to increase the size of the mask.</param>
        public EffectMask(BoundingBox box, int padding = 0)
        {
            this._box = box.ToRectangle();
            this._padding = padding;
            this._options = new GraphicsOptions { AlphaCompositionMode = PixelAlphaCompositionMode.SrcIn };
        }

        private Image<Rgba32> GetMaskBase() {
            return new Image<Rgba32>(this._box.Width + (this._padding*2), this._box.Height + (this._padding*2), Rgba32.ParseHex("#00000000"));
        }

        /// <summary>
        /// Gets the mask image to apply to an image.
        /// </summary>
        /// <returns>An image of the mask. Contains *only* the mask, not the "real" image.</returns>
        public Image<Rgba32> GetMask() {
            var mask = GetMaskBase();
            float threshold = 0.2F;
            Color sourceColor = Color.White;
            Color targetColor = Color.Transparent;
            RecolorBrush brush2 = new RecolorBrush(sourceColor, targetColor, threshold);
            // var topGrad = new RadialGradientBrush(new PointF(mask.Width/2, mask.Height/2), mask.Height/2, GradientRepetitionMode.None, new ColorStop(0.25F, Color.Transparent), new ColorStop(1, Color.White));
            // var maskGrad = new RadialGradientBrush(new PointF(mask.Width/2, mask.Height/2), mask.Height/2, GradientRepetitionMode.None, new ColorStop(0F, Color.Black), new ColorStop(0.8F, Color.Black), new ColorStop(0.9F, Color.ParseHex("#00000080")), new ColorStop(1F, Color.Transparent));
            var landscape = mask.Width > mask.Height;
            var axisEnd = landscape
                ? new PointF(mask.Width, mask.Height/2)
                : new PointF(mask.Width/2, mask.Height);
            var ratio = landscape
                ? (float)mask.Width/mask.Height
                : (float)mask.Height/mask.Width;
            var stops = new ColorStop[] {
                new ColorStop(0F, Color.Black), new ColorStop(0.66F, Color.Black), new ColorStop(1F, Color.Transparent)
            };
            if (ratio < 0.8 || ratio > 1.2) {
            // if (ratio > 0) {
                var grads = GetOverlapGradients(mask, stops);
                mask.Mutate(x => {
                    foreach (var grad in grads)
                    {
                        x.Fill(grad);
                    }
                });
            } else {
                var ellGrad = new EllipticGradientBrush(new PointF(mask.Width/2, mask.Height/2), axisEnd, 1F/ratio, GradientRepetitionMode.None, stops);
                mask.Mutate(x => {
                    x.Fill(ellGrad);
                });
            }
            
            return mask;
        }

        private List<IBrush> GetSquareGradients(Image<Rgba32> mask, ColorStop[] stops) {
            var top = new LinearGradientBrush(mask.GetCenter(), new PointF(mask.Width/2, 0), GradientRepetitionMode.DontFill, stops);
            return new List<IBrush> {top};
            // var left = new LinearGradientBrush(mask.GetCenter(), new PointF(0, mask.Height/2), GradientRepetitionMode.DontFill, stops);
            // var bottom = new LinearGradientBrush(mask.GetCenter(), new PointF(mask.Width/2, mask.Height), GradientRepetitionMode.DontFill, stops);
            // var right = new LinearGradientBrush(mask.GetCenter(), new PointF(mask.Width, mask.Height/2), GradientRepetitionMode.DontFill, stops);
            // return new List<IBrush> {top, left, bottom, right};

        }

        private List<IBrush> GetOverlapGradients(Image<Rgba32> mask, ColorStop[] stops) {
            var topCenter = new PointF(mask.Width/2, (mask.Height/4));
            var topEnd = new PointF(mask.Width, topCenter.Y);
            var topRatio = (topEnd.X-topCenter.X)/(mask.Height/4F);
            var top = new EllipticGradientBrush(topCenter, topEnd, 1/topRatio, GradientRepetitionMode.DontFill, stops);

            var lCenter = new PointF((mask.Width/4F), mask.Height/2F);
            var lEnd = new PointF(lCenter.X, mask.Height);
            var lRatio = (lEnd.Y-lCenter.Y)/(mask.Width/4F);
            var left = new EllipticGradientBrush(lCenter, lEnd, 1/lRatio, GradientRepetitionMode.DontFill, stops);

            var btCenter = new PointF(mask.Width/2, (mask.Height/4)*3);
            var bEnd = new PointF(mask.Width, btCenter.Y);
            var bRatio = (bEnd.X-btCenter.X)/(mask.Height/4F);
            var bottom = new EllipticGradientBrush(btCenter, bEnd, 1/bRatio, GradientRepetitionMode.DontFill, stops);

            var rCenter = new PointF((mask.Width/4F)*3F, mask.Height/2F);
            var rEnd = new PointF(rCenter.X, mask.Height);
            var rRatio = (rEnd.Y-rCenter.Y)/(mask.Width/4F);
            var right = new EllipticGradientBrush(rCenter, rEnd, 1/rRatio, GradientRepetitionMode.DontFill, stops);

            var cCenter = new PointF(mask.Width/2F, (mask.Height/2F));
            var cEnd = new PointF(mask.Width, mask.Height/2F);
            var cRatio = (cEnd.X-cCenter.X)/(mask.Height/2F);
            var center = new EllipticGradientBrush(cCenter, cEnd, 1/cRatio, GradientRepetitionMode.DontFill, stops);

            return new List<IBrush> {top, left, bottom, right, center};
            // var left = new LinearGradientBrush(mask.GetCenter(), new PointF(0, mask.Height/2), GradientRepetitionMode.DontFill, stops);
            // var bottom = new LinearGradientBrush(mask.GetCenter(), new PointF(mask.Width/2, mask.Height), GradientRepetitionMode.DontFill, stops);
            // var right = new LinearGradientBrush(mask.GetCenter(), new PointF(mask.Width, mask.Height/2), GradientRepetitionMode.DontFill, stops);
            // return new List<IBrush> {top, left, bottom, right};

        }

        /// <summary>
        /// Masks the provided image with a basic transparency gradient mask.
        /// </summary>
        /// <param name="image">The image to be masked. Must be smaller than the mask. Will not be modified.</param>
        /// <returns>A new image object with the provided image masked.</returns>
        public Image<Rgba32> GetMaskedImage(Image image) {
            var mask = GetMask();
            var r = mask.Clone(x => x.DrawImage(image, _options));
            return r;
        }

        /// <summary>
        /// Directly draws a partial image on to another image, masking the partial image first.
        /// </summary>
        /// <param name="inputImage">The "base" image.</param>
        /// <param name="censor">The partial image to be drawn on to the main image.</param>
        /// <typeparam name="T">The type of the base image.</typeparam>
        /// <returns>The original base image, mutated to include the partial image, blended with a mask.</returns>
        public T DrawMaskedEffect<T>(T inputImage, Image censor) where T : Image {
            var masked = GetMaskedImage(censor);
            inputImage.Mutate(x => {
                    x.DrawImage(masked, new Point(Convert.ToInt32(this._box.X-this._padding), Convert.ToInt32(this._box.Y-this._padding)), 1);
                });
            return inputImage;
        }

        public Action<IImageProcessingContext> GetMutation(Image censor) {
            var masked = GetMaskedImage(censor);
            return (x => {
                x.DrawImage(masked, new Point(Convert.ToInt32(this._box.X-this._padding), Convert.ToInt32(this._box.Y-this._padding)), 1);
            });
        }
    }
}
