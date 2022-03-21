using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CensorCore
{
    internal static class ImageSharpExtensions
    {
        internal static Rectangle ToRectangle(this BoundingBox box)
        {
            return new Rectangle(Convert.ToInt32(box.X), Convert.ToInt32(box.Y), box.Width, box.Height);
        }

        public static Rectangle GetPadded(this Rectangle rect, int padAmount = 1)
        {
            return new Rectangle(rect.X - padAmount, rect.Y - padAmount, rect.Width + (2 * padAmount), rect.Height + (2 * padAmount));
        }

        public static Rectangle GetPadded(this Rectangle rect, int padAmount, Image inputImage)
        {
            return new Rectangle(rect.X - padAmount, rect.Y - padAmount, rect.Width + (2 * padAmount), rect.Height + (2 * padAmount)).FitToBounds(inputImage);
        }

        public static Point ToPoint(this BoundingBox box)
        {
            return new Point(Convert.ToInt32(box.X), Convert.ToInt32(box.Y));
        }

        public static Point GetCenter(this BoundingBox box)
        {
            return new Point(Convert.ToInt32(box.X + (box.Width / 2)), Convert.ToInt32(box.Y + (box.Height / 2)));
        }

        public static Point GetCenter(this Rectangle rect)
        {
            return new Point(Convert.ToInt32(rect.X + (rect.Width / 2)), Convert.ToInt32(rect.Y + (rect.Height / 2)));
        }

        public static PointF GetCenter(this Image i) {
            return new PointF(i.Width/2, i.Height/2);
        }

        public static int GetPadding(this Image inputImage, Censoring.GlobalCensorOptions opts) {
            return Convert.ToInt32(Math.Max(10, Math.Min(inputImage.Width, inputImage.Height)/(40*1/(opts.PaddingScale ?? 1))));
        }

        public static Rectangle FitToBounds(this Rectangle rect, Image img) {
            return (Rectangle)((RectangleF)rect).FitToBounds(img);
        }

        public static RectangleF FitToBounds(this RectangleF rect, Image img) {
            rect.X = Math.Max(rect.X, 0);
            rect.Y = Math.Max(rect.Y, 0);
            if (rect.X+rect.Width > img.Width) {
                rect.Width = img.Width - rect.X;
            }
            if (rect.Y+rect.Height > img.Height) {
                rect.Height = img.Height - rect.Y;
            }
            return rect;
        }

        public static BoundingBox FitToBounds(this BoundingBox rect, Image img) {
            return new BoundingBox(
                Math.Max(rect.X, 0),
                Math.Max(rect.Y, 0),
                Math.Min(rect.X+rect.Width, img.Width),
                Math.Min(rect.Y+rect.Height, img.Height)
            );
        }

        public static float GetDistanceTo(this Point a, Point b) {
            return (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }

        public static Point GetMidpointBetween(this Point a, Point b) {
            return new Point((a.X + b.X)/2, (a.Y + b.Y)/2);
        }

        public static float GetAngleTo(this Point start, Point end) {
            var deltaY = start.Y - end.Y;
            var deltaX = end.X - start.X;
            var angle = (180/Math.PI) * Math.Atan2(deltaY, deltaX);
            return (float)(-angle);
        }
    }


}
