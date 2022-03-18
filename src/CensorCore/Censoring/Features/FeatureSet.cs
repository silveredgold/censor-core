using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;

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

        private float GetLongestBracket() {
            var lineLengths = Brackets.Select(pair =>
            {
                var p1 = pair.Start;
                var p2 = pair.End;
                return p1.GetDistanceTo(p2);
            }).ToList();
            var longest = lineLengths.Max();
            return longest;
        }

        public (float Width, Point Start, Point End) ToLine(float widthFactor = 3F, float lengthFactor = 0.25F) {
            var longest = GetLongestBracket();
            var width = (float)longest * widthFactor;
            var pts = GetOffsetPoints(lengthFactor);
            return (Width: width, Start: pts.Start, End: pts.End);
        }

        public BoundingBox ToVirtualBox(float widthFactor = 3F, float lengthFactor = 0.25F) {
            var longest = GetLongestBracket();
            var width = (float)longest * widthFactor;
            var pts = GetOffsetPoints();
            var offsetLeft = pts.Start;
            var offsetRight = pts.End;
            var lineLength = offsetLeft.GetDistanceTo(offsetRight);
            var midpoint = offsetLeft.GetMidpointBetween(offsetRight);
            var lineSegment = new LinearLineSegment(offsetLeft, offsetRight);
            var rect = Rectangle.FromLTRB(
                Convert.ToInt32(midpoint.X-(lineLength/2)), 
                Convert.ToInt32(midpoint.Y-(width/2)), 
                Convert.ToInt32(midpoint.X+(lineLength/2)), 
                Convert.ToInt32(midpoint.Y+(width/2))
            );
            return new BoundingBox(rect.X, rect.Y, rect.X+rect.Width, rect.Y+rect.Height);
        }

        public (Point Start, Point End) GetOffsetPoints(float lengthFactor = 0.25F) {
            var longest = GetLongestBracket();
            var lineLength = Left.GetDistanceTo(Right);
            var offsetRight = new Point(Convert.ToInt32(Math.Abs(Right.X + (Right.X - Left.X) / lineLength * (lineLength * lengthFactor))), Convert.ToInt32(Math.Abs(Right.Y + (Right.Y - Left.Y) / lineLength * (lineLength * 0.33))));
            var offsetLeft = new Point(Convert.ToInt32(Math.Abs(Left.X - (Right.X - Left.X) / lineLength * (lineLength * lengthFactor))), Convert.ToInt32(Math.Abs(Left.Y - (Right.Y - Left.Y) / lineLength * (lineLength * 0.33))));
            offsetLeft.Offset(0, Convert.ToInt32(Math.Abs(longest / 2)));
            offsetRight.Offset(0, Convert.ToInt32(Math.Abs(longest / 2)));
            return (offsetLeft, offsetRight);
        }
    }
}