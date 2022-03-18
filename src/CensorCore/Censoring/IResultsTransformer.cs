using SixLabors.ImageSharp;

namespace CensorCore.Censoring;

public interface IResultsTransformer
{
    IEnumerable<Classification> TransformResults(IEnumerable<Classification> matches);
}

public class CensorScaleTransformer : IResultsTransformer
{
    private readonly float _scaleFactor;

    public CensorScaleTransformer(GlobalCensorOptions? censorOptions = null)
    {
        _scaleFactor = censorOptions?.RelativeCensorScale ?? 1F;

    }
    public IEnumerable<Classification> TransformResults(IEnumerable<Classification> matches)
    {
        return matches.Select(m =>
        {
            m.Box = m.Box.ScaleBy(GetScaleAmount(m.Box.Width), GetScaleAmount(m.Box.Height));
            return m;
        });
    }

    private float GetScaleAmount(int srcValue, bool split = true) {
        return ((srcValue * _scaleFactor)-srcValue) / (split ? 2 : 1);
    }
}

public class IntersectingMatchMerger : IResultsTransformer
{
    public IEnumerable<Classification> TransformResults(IEnumerable<Classification> results)
    {
        var transformed = new List<Classification>();
        var matches = results.GroupBy(r => r.Label).ToList();
        foreach (var labelGroup in matches)
        {
            if (labelGroup.Count() > 1 && labelGroup.All(r => labelGroup.Any(l => r.Box.ToRectangle().IntersectsWith(l.Box.ToRectangle()))))
            {
                foreach (var pair in labelGroup.Chunk(2))
                {
                    if (pair.Count() < 2)
                    {
                        transformed.Add(pair.First());
                    }
                    var rectA = pair.First().Box.ToRectangle();
                    var rectB = pair.Last().Box.ToRectangle();
                    var rejections = new List<Predicate<Rectangle>>() {
                        r => r.Width > rectA.Width * 2F && r.Width > rectB.Width *2F,
                        r => r.Height > rectA.Height * 2F && r.Height > rectB.Height*2F
                    };
                    var unionRect = Rectangle.Union(rectA, rectB);
                    var closest = new[] {
                        rectA.GetCenter().GetAngleTo(rectB.GetCenter()),
                        rectB.GetCenter().GetAngleTo(rectA.GetCenter())
                        }.ClosestTo(0F);
                    if (!rejections.All(r => r(unionRect)))
                    {
                        transformed.Add(
                            new Classification(
                                new BoundingBox(unionRect.X, unionRect.Y, unionRect.X + unionRect.Width, unionRect.Y + unionRect.Height), Math.Max(pair.First().Confidence, pair.Last().Confidence), labelGroup.Key) {
                                    SourceAngle = closest
                                });
                    } else
                    {
                        transformed.AddRange(pair);
                    }
                }
            }
            else
            {
                transformed.AddRange(labelGroup);
            }
        }
        return transformed;
    }
}
