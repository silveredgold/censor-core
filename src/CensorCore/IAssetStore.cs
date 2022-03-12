using CensorCore.Censoring;
using SixLabors.ImageSharp;

namespace CensorCore {
    public interface IAssetStore {
        Task<string?> GetRandomCaption(string? category);
        Task<byte[]?> GetRandomImage(string imageType, float? ratio, List<string>? category);
        Task<IEnumerable<byte[]>> GetImages(string imageType, List<string>? category);
    }

    public class EmptyAssetStore : IAssetStore {
        public Task<IEnumerable<byte[]>> GetImages(string imageType, List<string>? category) {
            return Task.FromResult(Array.Empty<byte[]>().AsEnumerable());
        }

        public Task<string?> GetRandomCaption(string? category) {
            return Task.FromResult<string?>(null);
        }

        public Task<byte[]?> GetRandomImage(string imageType, float? ratio, List<string>? category) {
            return Task.FromResult<byte[]?>(null);
        }
    }

    public class DebugAssetStore : IAssetStore {
        private readonly string _imageRoot;

        public DebugAssetStore()
        {
            //just use beta safety stickers for debug purposes.
            this._imageRoot = @"X:\BetaSafety\BetaSafety-0.6.0.2\BetaSafety\browser-extension\images\stickers";
        }

        public Task<IEnumerable<byte[]>> GetImages(string imageType, List<string>? category) {
            var allFiles = Directory.EnumerateFiles(this._imageRoot, "*", SearchOption.AllDirectories).ToList();
            var allImages = allFiles.Select<string, (string FilePath, IImageInfo Image)>(fi => (FilePath: fi, Image: Image.Identify(fi)));
            var ratioImages = allImages.Where(i =>
            {
                return true;
            });
            return Task.FromResult(ratioImages.Select(i => File.ReadAllBytes(i.FilePath)));
        }

        public Task<string?> GetRandomCaption(string? category)
        {
            return Task.FromResult<string?>(null);
        }

        public async Task<byte[]?> GetRandomImage(string imageType, float? ratio, List<string>? category)
        {
            var allFiles = Directory.EnumerateFiles(this._imageRoot, "*", SearchOption.AllDirectories).ToList();
            var allImages = allFiles.Select<string, (string FilePath, IImageInfo Image)>(fi => (FilePath: fi, Image: Image.Identify(fi)));
            var ratioImages = allImages.Where(i =>
            {
                var iRatio = i.Image.Width / i.Image.Height;
                return ratio == null ? true : CloseEnough(iRatio, ratio.Value);
            });
            if (ratioImages.Any())
            {
                var selected = ratioImages.Random().FilePath;
                return await File.ReadAllBytesAsync(selected);
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
