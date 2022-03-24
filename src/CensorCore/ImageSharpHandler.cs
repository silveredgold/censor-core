using Microsoft.ML.OnnxRuntime;
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
        public bool ForceFit {get;set;}

        public ImageSharpHandler()
        {
            this._maxHeight = 1080;
            this._maxWidth = 1920;
        }

        public ImageSharpHandler(int? maxWidth, int? maxHeight)
        {
            this._maxWidth = maxWidth ?? 1920;
            this._maxHeight = maxHeight ?? 1080;
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
                } catch (Exception e) {
                    throw new Exception("Invalid base64 data URI!", e);
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
            if (!ForceFit) {
                var sampled = image.Clone(ctx => {
                    if (landscape && width > this._maxWidth) {
                        ctx.Resize(this._maxWidth, 0);
                    } else if (!landscape && height > this._maxHeight) {
                        ctx.Resize(0, this._maxHeight);
                    }
                });
                return sampled;
            } else {
                return image.Clone(ctx => {
                    ctx.Resize(new ResizeOptions {
                        Mode = ResizeMode.Max,
                        Size = new Size(this._maxWidth, this._maxHeight)
                    });
                });
            }
        }

        [Obsolete("Use typed variant instead", true)]
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
            var frameCount = img.Frames.Count;
            for (int i = 1; i < frameCount; i++)
            {
                img.Frames.RemoveFrame(frameCount - i);
            }
            var samples = this.ResizeImage(img);
            var scaleSize = new SizeF((float)img.Width / samples.Width, (float)img.Height / samples.Height);
            return Task.FromResult(new ImageData(img) {
                SampledImage = samples,
                ScaleFactor = scaleSize,
                Format = format
            });
        }
    }
}
