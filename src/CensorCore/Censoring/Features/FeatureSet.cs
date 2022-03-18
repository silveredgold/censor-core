using SixLabors.ImageSharp;

namespace CensorCore.Censoring.Features {
    public class FeatureSet {
        public Point Left { get; }

        public FeatureSet(Point left, Point right, IEnumerable<(Point Start, Point End)> brackets) {
            Left = left;
            Right = right;
            Brackets = brackets;
        }

        public Point Right { get; }
        public IEnumerable<(Point Start, Point End)> Brackets { get; }

        public (float Width, Point Start, Point End) ToLine(float widthFactor = 3F, float lengthFactor = 0.25F) {
            var lineLengths = Brackets.Select(pair =>
            {
                var p1 = pair.Start;
                var p2 = pair.End;
                return p1.GetDistanceTo(p2);
            }).ToList();
            var longest = lineLengths.Max();
            var width = (float)longest * widthFactor;
            var lineLength = Left.GetDistanceTo(Right);
            var offsetRight = new Point(Convert.ToInt32(Math.Abs(Right.X + (Right.X - Left.X) / lineLength * (lineLength * lengthFactor))), Convert.ToInt32(Math.Abs(Right.Y + (Right.Y - Left.Y) / lineLength * (lineLength * 0.33))));
            var offsetLeft = new Point(Convert.ToInt32(Math.Abs(Left.X - (Right.X - Left.X) / lineLength * (lineLength * lengthFactor))), Convert.ToInt32(Math.Abs(Left.Y - (Right.Y - Left.Y) / lineLength * (lineLength * 0.33))));
            offsetLeft.Offset(0, Convert.ToInt32(Math.Abs(longest / 2)));
            offsetRight.Offset(0, Convert.ToInt32(Math.Abs(longest / 2)));
            return (Width: width, Start: offsetLeft, End: offsetRight);
        }
    }
}