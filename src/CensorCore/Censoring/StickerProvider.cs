using SixLabors.ImageSharp;
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
        public async Task<Image<Rgba32>?> CensorImage(Image<Rgba32> inputImage, Classification result, string method, int level)
        {
            var padding = inputImage.GetPadding(_globalOpts);
            float boxRatio = (float)result.Box.Width / result.Box.Height;
            var categories = GetCategories(method);
            var sticker = await this._store.GetRandomImage(KnownAssetTypes.Stickers, boxRatio, categories);
            var mask = new EffectMask(result.Box, padding);
            var extract = inputImage.Clone(x => {
                var cropRect = result.Box.ToRectangle().GetPadded(padding, inputImage);
                x.Crop(cropRect);
                x.GaussianBlur(Math.Max(level, 10)*2);
            });
            mask.DrawMaskedEffect(inputImage, extract);
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
                    var targetLoc = new Point(effectCenter.X - (stickerImage.Width/2), effectCenter.Y - (stickerImage.Height/2));
                    inputImage.Mutate(x => {
                        x.DrawImage(stickerImage, targetLoc, Math.Min(level/10F,1));
                    });
                }
            }
            return null;
        }

        public bool Supports(string censorType) => censorType.StartsWith("sticker");

        private bool CloseEnough(float stickerRatio, float targetRatio) {
            var diff = stickerRatio / targetRatio;
            return 0.75 <= diff && diff <= 1.25;
        }

        private List<string>? GetCategories(string method) {
            List<string>? categories = null;
            var catString = method.Split(":").Skip(1).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(catString)) {
                var cats = catString.Split(',', ';').ToList();
                if (!cats.Any()) {
                    throw new InvalidOperationException();
                } else {
                    categories = cats;
                }
            }
            return categories;
        }
    }
}
