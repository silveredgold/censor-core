using CensorCore.Censoring.Features;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace CensorCore.Censoring; 
public interface ICensoringMiddleware {
    Task Prepare() => Task.CompletedTask;
    Task<IEnumerable<Classification>?> OnBeforeCensoring(ImageResult image, IResultParser? parser, Action<int, Action<IImageProcessingContext>> addLateMutation);
    Task OnAfterCensoring(Image image, IResultParser? parser) {
        return Task.CompletedTask;
    }
}

public class GifWatermarkMiddleware : ICensoringMiddleware {
    private readonly FontCollection _fonts;

    public GifWatermarkMiddleware(FontCollection? fonts = null)
    {
        _fonts = fonts ?? CaptionProvider.GetDefaultFontCollection();
    }

    public Task<IEnumerable<Classification>?> OnBeforeCensoring(ImageResult result, IResultParser? parser, Action<int, Action<IImageProcessingContext>> addLateMutation) {
        if (result.ImageData.Format is SixLabors.ImageSharp.Formats.Gif.GifFormat) {
                var fontSize = (result.ImageData.SourceImage.Width/10F);
                var font = _fonts.Families.First().CreateFont(fontSize, FontStyle.Bold);
                var origin = new Point(Math.Min(5, result.ImageData.SourceImage.Width), Math.Min(5, result.ImageData.SourceImage.Height));
                TextOptions options = new(font) {
                    Origin = origin, // Set the rendering origin.
                    WrappingLength = 0, // Greater than zero so we will word wrap at 100 pixels wide
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Font = font
                };
                IBrush brush = Brushes.Solid(Color.Black);
                IPen pen = Pens.Solid(Color.White, fontSize/16F);
                addLateMutation(10, x => x.DrawText(options, "GIF", brush, pen));
        }
        return Task.FromResult<IEnumerable<Classification>?>(null);
    }
}

public class FacialFeaturesMiddleware : ICensoringMiddleware {
    private readonly FontCollection _fonts;
    private readonly IAssetStore _assetStore;
    private FacialFeaturesService _faceService;

    public FacialFeaturesMiddleware(IAssetStore assetStore) {
        _fonts = CaptionProvider.GetDefaultFontCollection();
        _assetStore = assetStore;
        _faceService = new FacialFeaturesService(_assetStore);
    }

    public async Task<IEnumerable<Classification>?> OnBeforeCensoring(ImageResult result, IResultParser? parser, Action<int, Action<IImageProcessingContext>> addLateMutation) {
        var eyesResult = parser?.GetOptions("EYES_F", result);
        var mouthOptions = parser?.GetOptions("MOUTH_F", result);
        if (eyesResult is not null || mouthOptions is not null) {
            var classifications = new List<Classification>();
            foreach (var faceMatch in result.Results.Where(r => r.Label.ToLower().Contains("face_f"))) {
                var features = await _faceService.GetFeatures(result, faceMatch);
                if (features != null && features.TryGetValue("EYES_F", out var eyeFeature) && eyesResult != null) {
                        var factor = eyesResult.Level.GetScaleFactor(10F);
                        var rect = eyeFeature.ToVirtualBox(factor * 3F, factor * 0.25F);
                        var pts = eyeFeature.GetOffsetPoints(factor * 0.25F);
                        var angle = pts.Start.GetAngleTo(pts.End);
                        classifications.Add(new Classification(rect, faceMatch.Confidence, "EYES_F") {
                            SourceAngle = angle,
                            VirtualBox = true
                        });
                }
                if (features != null && features.TryGetValue("MOUTH_F", out var mouthFeature) && mouthOptions != null) {
                        var mouthBox = mouthFeature.ToVirtualBox(mouthOptions.Level.GetScaleFactor(10F) * 1.5F);
                        var pts = mouthFeature.GetOffsetPoints();
                        var angle = pts.Start.GetAngleTo(pts.End);
                        classifications.Add(new Classification(mouthBox, faceMatch.Confidence, "MOUTH_F") {
                            SourceAngle = angle,
                            VirtualBox = true
                        });
                    }
            }
            return classifications;
        }
        return null;
    }
}
