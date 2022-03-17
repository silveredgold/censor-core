using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;


namespace CensorCore
{
    public class ImageSharpHandler : IImageHandler
    {
        private readonly int _maxWidth;
        private readonly int _maxHeight;

        public ImageSharpHandler()
        {
            this._maxHeight = 1080;
            this._maxWidth = 1920;
        }

        public ImageSharpHandler(int maxWidth, int maxHeight)
        {
            this._maxWidth = maxWidth;
            this._maxHeight = maxHeight;
        }

        private async Task<byte[]> DownloadFile(Uri path)
        {
            using var client = new HttpClient();
            var result = await client.GetByteArrayAsync(path.ToString());
            return result;
        }

        public async Task<ImageData> LoadImage(string path)
        {
            if (path.StartsWith("data:")) {
                try {
                    var encodedBytes = path.Split(',')[1];
                    var contents = Convert.FromBase64String(encodedBytes);
                    return await LoadImageData(contents);
                } catch {
                    throw new Exception("Invalid base64 data URI!");
                }
            }
            if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out var uri))
            {
                var contents = uri.IsFile
                    ? await System.IO.File.ReadAllBytesAsync(uri.AbsolutePath)
                    : await DownloadFile(uri);
                return await LoadImageData(contents);
            } else
            {
                throw new Exception("Could not parse image URL!");
            }
            
        }

        private Image<Rgba32> ResizeImage(Image<Rgba32> image) {
            var width = image.Width;
            var height = image.Height;
            var landscape = width > height;
            var sampled = image.Clone(ctx => {
                if (landscape && width > this._maxWidth) {
                    ctx.Resize(this._maxWidth, 0);
                } else if (!landscape && height > this._maxHeight) {
                    ctx.Resize(0, this._maxHeight);
                }
            });
            return sampled;
        }

        public Task<InputImage> LoadToTensor(ImageData image)
        {
            var img = image.SampledImage ?? image.SourceImage;
            var origHeight = img.Height;
            // img.CopyPixelDataTo(new Span<Rgba32>());
            Tensor<float> data = new DenseTensor<float>(new[] {1, img.Height, img.Width, 3});
            img.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var pixelRow = accessor.GetRowSpan(y);

                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        // Get a reference to the pixel at position x
                        ref Rgba32 pixel = ref pixelRow[x];
                        data[0, y, x, 0] = pixel.B - 103.939F;
                        data[0, y, x, 1] = pixel.G - 116.779F;
                        data[0, y, x, 2] = pixel.R - 123.68F;
                    }
                }
            });
            return Task.FromResult(new InputImage(data, image));
        }

        public Task<InputImage<T>> LoadToTensor<T>(ImageData image, TensorLoadOptions<T> options) {
            var img = image.SampledImage ?? image.SourceImage;
            var origHeight = img.Height;
            // img.CopyPixelDataTo(new Span<Rgba32>());
            Tensor<T> data = new DenseTensor<T>(options.Dimensions(img));
            img.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var pixelRow = accessor.GetRowSpan(y);

                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        // Get a reference to the pixel at position x
                        ref Rgba32 pixel = ref pixelRow[x];
                        if (options.LoadPixel != null) {
                            options.LoadPixel(data, new Point(x, y), ref pixel);
                        }
                    }
                }
            });
            return Task.FromResult(new InputImage<T>(data, image));
        }

        public Task<ImageData> LoadImageData(byte[] contents) {
            var img = Image.Load<Rgba32>(contents, out var format);
            var samples = this.ResizeImage(img);
            float scaleFactor = (float)img.Height / samples.Height;
            return Task.FromResult(new ImageData(img) {
                SampledImage = samples,
                ScaleFactor = scaleFactor,
                Format = format
            });
        }
    }

    public class NudeNetLoadOptions : TensorLoadOptions<float> {
        public NudeNetLoadOptions() : base(img => new[] {1, img.Height, img.Width, 3}) {
            LoadPixel = LoadNudeNetPixels;
        }

        private void LoadNudeNetPixels(Tensor<float> data, Point point, ref Rgba32 pixel) {
            var y = point.Y;
            var x = point.X;
            data[0, y, x, 0] = pixel.B - 103.939F;
            data[0, y, x, 1] = pixel.G - 116.779F;
            data[0, y, x, 2] = pixel.R - 123.68F;
        }
    }

    public class FaceDetectionLoadOptions : TensorLoadOptions<float> {
        public FaceDetectionLoadOptions() : base(img => new[] {1, 3, img.Height, img.Width}) {
            LoadPixel = LoadLandmarkPixels;
        }

        private void LoadLandmarkPixels(Tensor<float> data, Point point, ref Rgba32 pixel) {
            var y = point.Y;
            var x = point.X;
            data[0, 0, y, x] = (pixel.B - 127F)/128;
            data[0, 1, y, x] = (pixel.G - 127F)/128;
            data[0, 2, y, x] = (pixel.R - 127F)/128;
        }
    }

    public class LandmarksLoadOptions : TensorLoadOptions<float> {
        public LandmarksLoadOptions() : base(img => new[] {1, 3, 112, 112}) {
            LoadPixel = LoadLandmarkPixels;
        }

        private void LoadLandmarkPixels(Tensor<float> data, Point point, ref Rgba32 pixel) {
            var y = point.Y;
            var x = point.X;
            data[0, 0, y, x] = pixel.B / 255F;
            data[0, 1, y, x] = pixel.G / 255F;
            data[0, 2, y, x] = pixel.R / 255F;
        }
    }
}
