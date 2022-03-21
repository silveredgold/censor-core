using CensorCore;
using CensorCore.Censoring;
using Xunit;

namespace CensorCore.Tests;

public class IntersectionTransformerTests {
    [Fact]
    public void SkipsSingleMatches()
    {
        var merger = new IntersectingMatchMerger();
        var classifications = new[] { new Classification(new BoundingBox(5,5, 10, 10), 1, "_FACE_F") };
        var transformed = merger.TransformResults(classifications, null).ToList();

        Assert.Single(transformed);
        Assert.Equal(5, transformed[0].Box.Height);
        Assert.Equal(5, transformed[0].Box.Width);
    }

    [Fact]
    public void ReturnsAllDistinct() {
        var merger = new IntersectingMatchMerger();
        var classifications = new[] { 
            new Classification(new BoundingBox(5,5, 10, 10), 1, "_FACE_F"),
            new Classification(new BoundingBox(50, 50, 55, 65), 1, "_FACE_F") };
        var transformed = merger.TransformResults(classifications, null).ToList();

        Assert.Equal(2, transformed.Count);
        Assert.Equal(5, transformed[0].Box.Height);
        Assert.Equal(15, transformed[1].Box.Height);
    }

    [Fact]
    public void DoesNotMergeDifferentClasses() {
        var merger = new IntersectingMatchMerger();
        var classifications = new[] { 
            new Classification(new BoundingBox(5,5, 20, 20), 1, "_FACE_F"),
            new Classification(new BoundingBox(10,10, 30, 30), 1, "_FACE_M") 
        };
        var transformed = merger.TransformResults(classifications, null).ToList();

        Assert.Equal(2, transformed.Count);
        Assert.Equal(15, transformed[0].Box.Height);
        Assert.Equal(20, transformed[1].Box.Width);
    }

    [Fact]
    public void MergesOverlapping() {
        var merger = new IntersectingMatchMerger();
        var classifications = new[] { 
            new Classification(new BoundingBox(5,5, 20, 15), 1, "_FACE_F"),
            new Classification(new BoundingBox(10,10, 30, 25), 1, "_FACE_F") 
        };
        var transformed = merger.TransformResults(classifications, null).ToList();

        Assert.Single(transformed);
        Assert.Equal(25, transformed[0].Box.Width);
        Assert.Equal(20, transformed[0].Box.Height);
    }

    [Fact]
    public void MergesMultipleOverlapping() {
        var merger = new IntersectingMatchMerger();
        var classifications = new[] { 
            new Classification(new BoundingBox(5,5, 20, 15), 1, "_FACE_F"),
            new Classification(new BoundingBox(10,10, 30, 25), 1, "_FACE_F"),
            new Classification(new BoundingBox(5,15, 30, 25), 1, "_FACE_F") 
        };
        var transformed = merger.TransformResults(classifications, null).ToList();

        Assert.Single(transformed);
        Assert.Equal(25, transformed[0].Box.Width);
        Assert.Equal(20, transformed[0].Box.Height);
    }

    [Fact]
    public void ClaimsHighestConfidence() {
        var merger = new IntersectingMatchMerger();
        var classifications = new[] { 
            new Classification(new BoundingBox(5,5, 20, 15), 0.5F, "_FACE_F"),
            new Classification(new BoundingBox(10,10, 30, 25), 0.75F, "_FACE_F") ,
            new Classification(new BoundingBox(5,15, 30, 25), 1F, "_FACE_F") 
        };
        var transformed = merger.TransformResults(classifications, null).ToList();

        Assert.Single(transformed);
        Assert.Equal(25, transformed[0].Box.Width);
        Assert.Equal(1, transformed[0].Confidence);
    }

    [Fact]
    public void IgnoresLowConfidenceExtras() {
        var merger = new IntersectingMatchMerger();
        var classifications = new[] { 
            new Classification(new BoundingBox(5,5, 20, 15), 0.85F, "_FACE_F"),
            new Classification(new BoundingBox(10,10, 30, 25), 1F, "_FACE_F") ,
            new Classification(new BoundingBox(10,10, 35, 35), 0.5F, "_FACE_F") 
        };
        var transformed = merger.TransformResults(classifications, null).ToList();

        Assert.Single(transformed);
        Assert.Equal(25, transformed[0].Box.Width);
        Assert.Equal(1, transformed[0].Confidence);
    }
}