using System.Linq;
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
        public async Task<Action<IImageProcessingContext>> CensorImage(Image<Rgba32> inputImage, Classification result, string method, int level)
        {
            var mutations = new List<Action<IImageProcessingContext>>();
            var padding = inputImage.GetPadding(_globalOpts);
            float boxRatio = (float)result.Box.Width / result.Box.Height;
            var categories = GetCategories(method);
            var sticker = await GetImageAsync(boxRatio, categories);
            var mask = new EffectMask(result.Box, padding);
            var extract = inputImage.Clone(x => {
                var cropRect = result.Box.ToRectangle().GetPadded(padding, inputImage);
                x.Crop(cropRect);
                x.GaussianBlur(Math.Max(level, 10)*2);
            });
            mutations.Add(mask.GetMutation(extract));
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
                if (sticker is not null) {
                    return sticker.RawData;
                }
            } catch (NotImplementedException) {
                //ignored
                //not all providers will implement this, and that's okay
            }
            try {
                var allFiles = await this._store.GetImages(KnownAssetTypes.Stickers, categories);
                var allImages = allFiles.Select<RawImageData, (byte[] ImageData, IImageInfo Image)>(fi => (ImageData: fi.RawData, Image: Image.Identify(fi.RawData)));
                var ratioImages = allImages.Where(i =>
                {
                    var iRatio = i.Image.Width / i.Image.Height;
                    return boxRatio == null ? true : CloseEnough(iRatio, boxRatio.Value);
                });
                if (ratioImages.Any())
                {
                    return ratioImages.Random().ImageData;
                }
                return null;
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
