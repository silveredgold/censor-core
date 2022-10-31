using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
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

        // public bool Supports(string censorType) => censorType.StartsWith("caption");
        public int Layer => 6;

        public async Task<Action<IImageProcessingContext>?> CensorImage(Image<Rgba32> inputImage, Classification result, string method, int level) {
            var mutations = new List<Action<IImageProcessingContext>>();
            var padding = inputImage.GetPadding(_globalOpts);
            float boxRatio = (float)result.Box.Width / result.Box.Height;
            var opts = method.GetCaptionOptions("caption");
            var categories = opts.Categories;
            var caption = await _assetStore.GetRandomCaption(categories?.Random());
            var cropRect = result.Box.ToRectangle();

            if (opts.PreferBox) {
                var bar = CensorEffects.GetBlackBarEffect(inputImage, result, level);
                mutations.Add(bar);
            } else {
                var blur = CensorEffects.GetMaskedBlurEffect(inputImage, result, padding, level, minimumLevel: 10);
                mutations.Add(blur);
            }
            
            if (caption != null) {
                var levelDiff = ((level-10F)*0.75F)+10F;
                var fontSize = (result.Box.Width/4F)*(levelDiff.GetScaleFactor(10F));
                var font = _fonts.Families.First().CreateFont(fontSize, FontStyle.Bold);
                // The options are optional
                TextOptions options = new(font) {
                    Origin = cropRect.GetCenter(), // Set the rendering origin.
                    TabWidth = 4, // A tab renders as 8 spaces wide
                    WrappingLength = opts.WrapText ? result.Box.Width : 0, // Greater than zero so we will word wrap at 100 pixels wide
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };
                IBrush brush = Brushes.Solid(Color.White);
                IPen pen = Pens.Solid(Color.Black, (result.Box.Width/80F)*(level.GetScaleFactor(10F)));
                if (result.SourceAngle.HasValue) {
                    var drawOpts = new DrawingOptions() { Transform = Matrix3x2Extensions.CreateRotationDegrees(result.SourceAngle.Value, result.Box.GetCenter())};
                    mutations.Add(x => x.DrawText(drawOpts, options, caption.ToUpper(), brush, pen));
                } else {
                    mutations.Add(x => x.DrawText(options, caption.ToUpper(), brush, pen));
                }
            }
            return x =>
            {
                foreach (var mutation in mutations) {
                    mutation(x);
                }
            };
        }

        internal static FontCollection GetDefaultFontCollection() {
            return new EmbeddedFontProvider().LoadEmbeddedFonts().GetCollection();
        }
    }
}
