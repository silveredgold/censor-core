using System.Reflection;
using SixLabors.Fonts;

namespace CensorCore {
    public class EmbeddedFontProvider {
        private readonly FontCollection _collection;

        public EmbeddedFontProvider() {
            _collection = new FontCollection();
        }

        public EmbeddedFontProvider LoadEmbeddedFonts(Assembly? assembly = null) {
            assembly ??= typeof(EmbeddedFontProvider).Assembly;
            var model = assembly.GetManifestResourceNames();
            //TODO: this doesn't match right
            if (model != null && model.Any()) {
                var fonts = model.Where(f => Path.GetExtension(f) == ".ttf").ToList();
                foreach (var font in fonts) {
                    using var resourceStream = assembly.GetManifestResourceStream(font);
                    if (resourceStream != null) {
                        _collection.Add(resourceStream, out var description);
                    }
                }
            }
            return this;
        }

        public FontCollection GetCollection() {
            return _collection;
        }

        private static byte[]? ExtractStreamResource(Stream resFilestream) {
            if (resFilestream == null) return null;
            byte[] bytes = new byte[resFilestream.Length];
            // Console.WriteLine($"reading stream of length '{resFilestream.Length}'");
            var reader = new BinaryReader(resFilestream);
            var readBytes = reader.ReadBytes(Convert.ToInt32(resFilestream.Length));
            return bytes;
        }
    }
}