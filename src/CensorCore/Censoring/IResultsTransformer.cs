using SixLabors.ImageSharp;

namespace CensorCore.Censoring;

public interface IResultsTransformer {
    IEnumerable<Classification> TransformResults(IEnumerable<Classification> matches, IResultParser? parser);
}

public class CensorScaleTransformer : IResultsTransformer {
    private readonly float _scaleFactor;

    public CensorScaleTransformer(GlobalCensorOptions? censorOptions = null) {
        _scaleFactor = censorOptions?.RelativeCensorScale ?? 1F;

    }
    public IEnumerable<Classification> TransformResults(IEnumerable<Classification> matches, IResultParser? parser) {
        return matches.Select(m =>
        {
            m.Box = m.Box.ScaleBy(GetScaleAmount(m.Box.Width), GetScaleAmount(m.Box.Height));
            return m;
        });
    }

    private float GetScaleAmount(int srcValue, bool split = true) {
        return ((srcValue * _scaleFactor) - srcValue) / (split ? 2 : 1);
    }
}

public class IntersectingMatchMerger : IResultsTransformer {
    public record CurrentSet(List<Classification> Matches, Rectangle? Merged) {

    }
    public IEnumerable<Classification> TransformResults(IEnumerable<Classification> results, IResultParser? parser) {
        var transformed = new List<Classification>();
        var matches = results.GroupBy(r => r.Label).ToList();
        foreach (var labelGroup in matches) {
            var list = new List<List<Classification>>();
            if (labelGroup.Count() > 1) {
                foreach (var rect in labelGroup) {
                    var intersecting = labelGroup.Where(r => rect.Box.ToRectangle().IntersectsWith(r.Box.ToRectangle())).Where(r => labelGroup.Any(l => !l.Box.ToRectangle().Contains(r.Box.ToRectangle()))).ToList();
                    var isLargest = intersecting.All(r => (rect.GetSize()) >= (r.GetSize()));
                    if (isLargest) {
                        list.Add(intersecting.ToList());
                    }
                }
                foreach (var set in list.Select(l => {
                    var maxConf = l.Max(lcm => lcm.Confidence);
                    return l.Where(lc => lc.Confidence >= (maxConf*0.75F)).ToList();
                })) {
                    if (set.Count() < 2) {
                        transformed.Add(set.First());
                    }
                    else {
                        var lastMatch = set[0];
                        // Rectangle? main = set[0].Box.ToRectangle();
                        Classification? last = null;
                        for (int i = 1; i < set.Count; i++) {
                            var current = set[i];
                            var comparison = last ?? set[i-1];
                            var rectA = current.Box.ToRectangle();
                            var rectB = comparison.Box.ToRectangle();
                            var rejections = new List<Predicate<Rectangle>>() {
                                r => r.Width > rectA.Width * 2F && r.Width > rectB.Width *2F,
                                r => r.Height > rectA.Height * 2F && r.Height > rectB.Height*2F
                            };
                            var unionRect = Rectangle.Union(rectA, rectB);
                            var closest = new[] {
                                rectA.GetCenter().GetAngleTo(rectB.GetCenter()),
                                rectB.GetCenter().GetAngleTo(rectA.GetCenter())
                                }.ClosestTo(0F);
                            if (!rejections.All(r => r(unionRect))) {
                                var confidence = new[] {last?.Confidence ?? 0F, current.Confidence, comparison?.Confidence ?? 0F}.Max();
                                last = new Classification(
                                        new BoundingBox(unionRect.X, unionRect.Y, unionRect.X + unionRect.Width, unionRect.Y + unionRect.Height), confidence, labelGroup.Key) {
                                        SourceAngle = closest,
                                        VirtualBox = false //it might seem like this would be virtual, but the union rect will work either way.
                                    };
                                // i++; //skip an element
                            }
                            else {
                                transformed.Add(comparison);
                                comparison = null;
                            }

                        }
                        if (last != null) {
                            transformed.Add(last);
                        }
                    //     var rectA = pair.First().Box.ToRectangle();
                    //     var rectB = pair.Last().Box.ToRectangle();
                    //     var rejections = new List<Predicate<Rectangle>>() {
                    //     r => r.Width > rectA.Width * 2F && r.Width > rectB.Width *2F,
                    //     r => r.Height > rectA.Height * 2F && r.Height > rectB.Height*2F
                    // };
                    //     var unionRect = Rectangle.Union(rectA, rectB);
                    //     var closest = new[] {
                    //     rectA.GetCenter().GetAngleTo(rectB.GetCenter()),
                    //     rectB.GetCenter().GetAngleTo(rectA.GetCenter())
                    //     }.ClosestTo(0F);
                    //     if (!rejections.All(r => r(unionRect))) {
                    //         transformed.Add(
                    //             new Classification(
                    //                 new BoundingBox(unionRect.X, unionRect.Y, unionRect.X + unionRect.Width, unionRect.Y + unionRect.Height), Math.Max(pair.First().Confidence, pair.Last().Confidence), labelGroup.Key) {
                    //                 SourceAngle = closest,
                    //                 VirtualBox = false //it might seem like this would be virtual, but the union rect will work either way.
                    //             });
                    //     }
                    //     else {
                    //         transformed.AddRange(pair);
                    //     }
                    }
                }
            }
            else {
                transformed.AddRange(labelGroup);
            }
            // if (labelGroup.Count() > 1 && labelGroup.All(r => labelGroup.All(l => r.Box.ToRectangle().IntersectsWith(l.Box.ToRectangle()))))
            // {
            //     foreach (var pair in labelGroup.Chunk(2))
            //     {

            //     }
            // }
            // else
            // {
            //     transformed.AddRange(labelGroup);
            // }
        }
        return transformed;
    }
}
