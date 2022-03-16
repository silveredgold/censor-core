using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;



namespace CensorCore.Censoring {
    public class CaptionProvider : ICensorTypeProvider {
        private readonly FontCollection _fonts;
        private readonly GlobalCensorOptions _globalOpts;
        private readonly IAssetStore _assetStore;

        public CaptionProvider(IAssetStore assetStore, FontCollection? fonts = null, GlobalCensorOptions? opts = null) =>
            (_assetStore, _fonts, _globalOpts) = (assetStore, fonts ?? GetDefaultFontCollection(), opts ?? new GlobalCensorOptions());

        public bool Supports(string censorType) => censorType.StartsWith("caption");
        public int Layer => 6;

        public async Task<Action<IImageProcessingContext>> CensorImage(Image<Rgba32> inputImage, Classification result, string method, int level) {
            var mutations = new List<Action<IImageProcessingContext>>();
            var padding = inputImage.GetPadding(_globalOpts);
            float boxRatio = (float)result.Box.Width / result.Box.Height;
            var categories = GetCategories(method);
            var caption = await _assetStore.GetRandomCaption(categories?.Random());
            // var caption = GetCaption().ToUpper();
            var mask = new EffectMask(result.Box, padding);
            var cropRect = result.Box.ToRectangle().GetPadded(padding, inputImage);
            var extract = inputImage.Clone(x =>
            {
                x.Crop(cropRect);
                x.GaussianBlur(Math.Max(level, 10) * 2.5F);
            });
            mutations.Add(mask.GetMutation(extract));
            if (caption != null) {
                var font = _fonts.Families.First().CreateFont(result.Box.Width/4F, FontStyle.Bold);
                // The options are optional
                TextOptions options = new(font) {
                    Origin = cropRect.GetCenter(), // Set the rendering origin.
                    TabWidth = 4, // A tab renders as 8 spaces wide
                    WrappingLength = result.Box.Width, // Greater than zero so we will word wrap at 100 pixels wide
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                IBrush brush = Brushes.Solid(Color.White);
                IPen pen = Pens.Solid(Color.Black, result.Box.Width/80F);

                // Draws the text with horizontal red and blue hatching with a dash dot pattern outline.
                mutations.Add(x => x.DrawText(options, caption.ToUpper(), brush, pen));
            }
            return x =>
            {
                foreach (var mutation in mutations) {
                    mutation(x);
                }
            };
        }

        private List<string>? GetCategories(string method) {
            List<string>? categories = null;
            var catString = method.Split(":").Skip(1).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(catString)) {
                var cats = catString.Split(',', ';').ToList();
                if (!cats.Any()) {
                    throw new InvalidOperationException();
                }
                else {
                    categories = cats;
                }
            }
            return categories;
        }

        internal static FontCollection GetDefaultFontCollection() {
            return new EmbeddedFontProvider().LoadEmbeddedFonts().GetCollection();
        }
    }
}
