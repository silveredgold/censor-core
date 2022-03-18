using CensorCore.Censoring.Features;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace CensorCore.Censoring; 
public interface ICensoringMiddleware {
    Task Prepare() => Task.CompletedTask;
    Task<IEnumerable<Classification>?> OnBeforeCensoring(ImageResult image, IResultParser? parser, Action<int, Action<IImageProcessingContext>> addLateMutation);
    Task OnAfterCensoring(Image image);
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

    public Task OnAfterCensoring(Image image) {
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Classification>?> OnBeforeCensoring(ImageResult result, IResultParser? parser, Action<int, Action<IImageProcessingContext>> addLateMutation) {
        var eyesResult = parser?.GetOptions("EYES_F", result);
        var mouthOptions = parser?.GetOptions("MOUTH_F", result);
        if (eyesResult is not null || mouthOptions is not null) {
            foreach (var faceMatch in result.Results.Where(r => r.Label.ToLower().Contains("face_f"))) {
                var features = await _faceService.GetFeatures(result, faceMatch);
                if (features != null && features.TryGetValue("EYES_F", out var eyeFeature) && eyesResult != null) {
                    if (eyesResult.CensorType.ContainsAny(StringComparison.CurrentCultureIgnoreCase, "bars", "bb", "blackb", "caption")) {
                        var factor = eyesResult.Level.GetScaleFactor(10F);
                        var eyeLine = eyeFeature.ToLine(factor * 3F, factor * 0.25F);
                        addLateMutation(10, x =>
                        {
                            x.DrawLines(Pens.Solid(Color.Black, eyeLine.Width), eyeLine.Start, eyeLine.End);
                        });
                        if (eyesResult.CensorType.Contains("caption", StringComparison.CurrentCultureIgnoreCase)) {
                            var categories = eyesResult.CensorType.GetCategories();
                            var caption = await _assetStore.GetRandomCaption(categories?.Random());
                            if (caption != null) {
                                var lineLength = eyeLine.Start.GetDistanceTo(eyeLine.End);
                                var font = _fonts.Families.First().CreateFont(lineLength / 5F, FontStyle.Bold);
                                var midPoint = eyeLine.Start.GetMidpointBetween(eyeLine.End);
                                TextOptions options = new(font) {
                                    Origin = midPoint, //TODO // Set the rendering origin.
                                    WrappingLength = (float)(lineLength * 1.25), // Greater than zero so we will word wrap at 100 pixels wide
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center,
                                };
                                var angle = eyeLine.Start.GetAngleTo(eyeLine.End);
                                addLateMutation(10, x =>
                                {
                                    x.DrawText(
                                        new DrawingOptions() { Transform = Matrix3x2Extensions.CreateRotationDegrees(angle, eyeLine.Start.GetMidpointBetween(eyeLine.End)) },
                                        options, 
                                        caption.ToUpper(), 
                                        Brushes.Solid(Color.White), 
                                        Pens.Solid(Color.White, eyeLine.Width / 40F)
                                    );
                                });
                            }
                        }
                    }
                }
                if (features != null && features.TryGetValue("MOUTH_F", out var mouthFeature) && mouthOptions != null) {
                    if (mouthOptions.CensorType.ContainsAny(StringComparison.CurrentCultureIgnoreCase, "bars", "bb", "blackb", "caption")) {
                        // We're only pretending to support captions here.
                        // Text layout in a box this (potentially) small and weirdly-shaped is just too hard.
                        var mouthBox = mouthFeature.ToLine(mouthOptions.Level.GetScaleFactor(10F) * 1.5F);
                        addLateMutation(10, x =>
                        {
                            x.DrawLines(Pens.Solid(Color.Black, mouthBox.Width), mouthBox.Start, mouthBox.End);
                        });
                    }
                }

            }
        }
        return null;
    }
}
