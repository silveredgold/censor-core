using CensorCore.Censoring;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace CensorCore {

    public record RawImageData(byte[] RawData, string? MimeType = null) { }
    public interface IAssetStore {
        Task<string?> GetRandomCaption(string? category);
        Task<RawImageData?> GetRandomImage(string imageType, float? ratio, List<string>? category);
        Task<IEnumerable<RawImageData>> GetImages(string imageType, List<string>? category);
    }

    public class EmptyAssetStore : IAssetStore {
        public Task<IEnumerable<RawImageData>> GetImages(string imageType, List<string>? category) {
            return Task.FromResult(Array.Empty<RawImageData>().AsEnumerable());
        }

        public Task<string?> GetRandomCaption(string? category) {
            return Task.FromResult<string?>(null);
        }

        public Task<RawImageData?> GetRandomImage(string imageType, float? ratio, List<string>? category) {
            return Task.FromResult<RawImageData?>(null);
        }
    }

    public class DebugAssetStore : IAssetStore {
        private readonly string _imageRoot;

        public DebugAssetStore()
        {
            //just use beta safety stickers for debug purposes.
            this._imageRoot = @"X:\BetaSafety\BetaSafety-0.6.0.2\BetaSafety\browser-extension\images\stickers";
        }

        public Task<IEnumerable<RawImageData>> GetImages(string imageType, List<string>? category) {
            var allFiles = Directory.EnumerateFiles(this._imageRoot, "*", SearchOption.AllDirectories).ToList();
            var allImages = allFiles.Select<string, (string FilePath, IImageInfo Image, IImageFormat Format)>(fi => (FilePath: fi, Image: Image.Identify(fi, out var format), Format: format));
            var ratioImages = allImages.Where(i =>
            {
                return true;
            });
            return Task.FromResult(ratioImages.Select(i => new RawImageData(File.ReadAllBytes(i.FilePath), i.Format.DefaultMimeType)));
        }

        public async Task<string?> GetRandomCaption(string? category)
        {
            var lines = await File.ReadAllLinesAsync(@"X:\BetaSafety\BetaSafety-0.6.0.2\BetaSafety\browser-extension\eyebarCaptions.txt");
            return lines.Random();
        }

        public async Task<RawImageData?> GetRandomImage(string imageType, float? ratio, List<string>? category)
        {
            var allFiles = Directory.EnumerateFiles(this._imageRoot, "*", SearchOption.AllDirectories).ToList();
            var allImages = allFiles.Select<string, (string FilePath, IImageInfo Image, IImageFormat Format)>(fi => (FilePath: fi, Image: Image.Identify(fi, out var format), Format: format));
            var ratioImages = allImages.Where(i =>
            {
                var iRatio = i.Image.Width / i.Image.Height;
                return ratio == null ? true : CloseEnough(iRatio, ratio.Value);
            });
            if (ratioImages.Any())
            {
                var selected = ratioImages.Random();
                var bytes = await File.ReadAllBytesAsync(selected.FilePath);
                return new RawImageData(bytes, selected.Format.DefaultMimeType);
            }
            return null;
        }

        private bool CloseEnough(float stickerRatio, float targetRatio)
        {
            var diff = stickerRatio / targetRatio;
            return 0.75 <= diff && diff <= 1.25;
        }
    }
}
