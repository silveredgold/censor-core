using System.Linq;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;



namespace CensorCore.Censoring
{
    public static class KnownAssetTypes {
        public static string Stickers => "stickers";
    }
    public class StickerProvider : ICensorTypeProvider
    {
        private readonly IAssetStore _store;
        private readonly GlobalCensorOptions _globalOpts = new GlobalCensorOptions();

        public StickerProvider(IAssetStore store)
        {
            this._store = store;
        }
        public StickerProvider(IAssetStore store, GlobalCensorOptions options) : this(store)
        {
            this._globalOpts = options;
        }
        public async Task<Action<IImageProcessingContext>?> CensorImage(Image<Rgba32> inputImage, Classification result, string method, int level)
        {
            var mutations = new List<Action<IImageProcessingContext>>();
            var padding = inputImage.GetPadding(_globalOpts);
            float boxRatio = (float)result.Box.Width / result.Box.Height;
            var options = method.GetOptions("sticker");
            
            var useBlur = options.Parameters != null
                ? options.Parameters.TryGetFirst("useBlur", out var optionObj) && bool.TryParse((string)optionObj, out var useBlurOption) && useBlurOption
                : _globalOpts.ForcePixelBackground.HasValue ? !_globalOpts.ForcePixelBackground.Value : true;
            var usePixels = _globalOpts.ForcePixelBackground == true || ( options.Parameters != null
                ? options.Parameters.TryGetFirst("usePixels", out var pixelOption) && bool.TryParse((string)pixelOption, out var usePixelOption) && usePixelOption
                : false );
            var sticker = await GetImageAsync(boxRatio, options.Categories);
            if (useBlur) {
                var blurMutation = CensorEffects.GetMaskedBlurEffect(inputImage, result, padding, level);
                mutations.Add(blurMutation);
            } else if (usePixels) {
                var pixelMutation = CensorEffects.GetMaskedPixelEffect(inputImage, result, padding, level);
            }
            var effectCenter = result.Box.GetCenter();
            if (sticker != null) {
                var stickerImage = Image.Load(sticker);
                var stickerRatio = stickerImage.Width / stickerImage.Height;
                if (CloseEnough(stickerRatio, boxRatio)) {
                    var resizeOpts = new ResizeOptions {
                        Size = new Size(result.Box.Width, result.Box.Height),
                        Mode = ResizeMode.Max
                    };
                    stickerImage.Mutate(s => {
                        s.Resize(resizeOpts);
                    });
                    if (result.SourceAngle.HasValue) {
                        var ctr = result.Box.GetCenter();
                        var affineBuilder = new AffineTransformBuilder();
                        affineBuilder.PrependTranslation(new Vector2(ctr.X, ctr.Y));
                        affineBuilder.PrependRotationDegrees(result.SourceAngle.Value);
                        affineBuilder.AppendTranslation(new Vector2(-ctr.X, -ctr.Y));
                        stickerImage.Mutate(m => m.Transform(affineBuilder));
                    }
                    var targetLoc = new Point(effectCenter.X - (stickerImage.Width/2), effectCenter.Y - (stickerImage.Height/2));
                    mutations.Add(x => {
                        x.DrawImage(stickerImage, targetLoc, Math.Min(level/10F,1));
                    });
                }
            }
            return x => {
                foreach (var mutation in mutations)
                {
                    mutation(x);
                }
            };
        }

        private async Task<byte[]?> GetImageAsync(float? boxRatio, List<string>? categories) {
            try {
                var sticker = await this._store.GetRandomImage(KnownAssetTypes.Stickers, boxRatio, categories);
                return sticker?.RawData;
            } catch (NotImplementedException) {
                //ignored
                //not all providers will implement this, and that's okay
            }
            try {
                var allFiles = await this._store.GetImages(KnownAssetTypes.Stickers, categories);
                RawImageData? validImage = null;
                var resultCount = allFiles.Count();
                //if we pull _80%_ of the total elements by random and they don't match, we're probably not going to find one.
                for (int i = 0; i < resultCount / 1.25 && validImage == null; i++)
                {
                    var candidate = allFiles.Random(resultCount);
                    var img = Image.Identify(candidate.RawData, out var format);
                    var iRatio = img.Width / img.Height;
                    if (boxRatio == null || CloseEnough(iRatio, boxRatio.Value)) {
                        validImage = new RawImageData(candidate.RawData, format.DefaultMimeType);
                    }
                }
                return validImage?.RawData;
            } catch {
                return null;
            }

        }

        
        private bool CloseEnough(float stickerRatio, float targetRatio)
        {
            var diff = stickerRatio / targetRatio;
            return 0.75 <= diff && diff <= 1.25;
        }

        public bool Supports(string censorType) => censorType.StartsWith("sticker");
        public int Layer => 6;
    }
}
