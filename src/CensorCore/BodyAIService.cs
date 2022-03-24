using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CensorCore {
    public class BodyAIService : IAIService<BoundingBox> {
        private readonly InferenceSession _session;
        private readonly IImageHandler _imageHandler;

        public bool FilterOutput {get;set;} = false;

        public bool Verbose { get ;set; } = false;

        public SessionOptions Options => new SessionOptions() {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
        };

        public BodyAIService(InferenceSession session, IImageHandler imageHandler) {
            this._session = session;
            this._imageHandler = imageHandler;
        }

        public async Task<ImageResult<BoundingBox>?> RunModel(byte[] data, MatchOptions? options = null) {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            var imageData = await this._imageHandler.LoadImageData(data);
            timer.Stop();
            Log($"Loaded image data in {timer.Elapsed.TotalSeconds}s");
            var result = await RunModelForImage(imageData, options);
            if (result != null && result.Session != null) {
                result.Session.ImageLoadTime = timer.Elapsed;
            }
            return result;
        }

        public async Task<ImageResult<BoundingBox>?> RunModel(string url, MatchOptions? options = null) {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            var imageData = await this._imageHandler.LoadImage(url);
            timer.Stop();
            Log($"Loaded image data in {timer.Elapsed.TotalSeconds}s");
            var result = await RunModelForImage(imageData, options);
            if (result != null && result.Session != null) {
                result.Session.ImageLoadTime = timer.Elapsed;
            }
            return result;
        }

        private void Log(string message) {
            if (Verbose) {
                Console.WriteLine(message);
            }
        }

        internal async Task<ImageResult<BoundingBox>?> RunModelForImage(ImageData imageData, MatchOptions? options) {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Restart();
            var tensorOpts = new RobustVideoOptions();
            var modelInput = await this._imageHandler.LoadToTensor<float>(imageData, tensorOpts);
            timer.Stop();
            var tensorLoadTime = timer.Elapsed;
            Log($"Loaded tensor data in {timer.Elapsed.TotalSeconds}s");
            timer.Restart();
            var feeds = tensorOpts.GetFeeds(_session, modelInput).ToList();
            var output = this._session.Run(feeds);
            var runTime = timer.Elapsed;
            Log($"Finished model in {timer.Elapsed.TotalSeconds}s");
            timer.Restart();
            var classifications = this.GetResults(imageData, output.ToList(), MatchOptions.GetDefault()).ToList();
            var modelName = string.IsNullOrWhiteSpace(this._session.ModelMetadata.Description)
                ? this._session.ModelMetadata.GraphName
                : this._session.ModelMetadata.Description;
            var sessionMeta = new SessionMetadata(modelName, runTime) {
                TensorLoadTime = tensorLoadTime
            };
            var result = new ImageResult<BoundingBox>(imageData, classifications) {
                Session = sessionMeta
            };
            
            Log($"Built results in {timer.Elapsed.TotalSeconds}s");
            timer.Reset();
            return result;
        }

        private IEnumerable<BoundingBox> GetResults(ImageData imgData, List<DisposableNamedOnnxValue> tensorOutput, MatchOptions matchOptions) {
            var results = tensorOutput.ToArray();
            var length = results.Length;
            var img = imgData.SampledImage ?? imgData.SourceImage;
            var foreground = results[0].AsTensor<float>().ToList();
            var alphaPred = results[1].AsTensor<float>().ToList();
            int newMinX = img.Width;
            int newMinY = img.Height;
            int newMaxX = 0;
            int newMaxY = 0;
            
            img.ProcessPixelRows(accessor =>
            {
                for (int y = 0, k = 0; y < accessor.Height; y++)
                {
                    var pixelRow = accessor.GetRowSpan(y);

                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        // Get a reference to the pixel at position x
                        ref Rgba32 pixel = ref pixelRow[x];
                        var a = alphaPred[k++];
                        if (a > 0.5F) {
                            newMinX = Math.Min(newMinX, x);
                            newMinY = Math.Min(newMinY, y);
                            newMaxX = Math.Max(newMaxX, x);
                            newMaxY = Math.Max(newMaxY, y);
                        }
                        if (FilterOutput) {
                            pixel.R = (byte)(pixel.R*a);
                            pixel.G = (byte)(pixel.G*a);
                            pixel.B = (byte)(pixel.B*a);
                            pixel.A = (byte)(pixel.A*a);
                        }
                    }
                }
            });
            var rect = Rectangle.FromLTRB(newMinX, newMinY, newMaxX, newMaxY);
            return new[] {new BoundingBox(rect.X, rect.Y, rect.X+rect.Width, rect.Y+rect.Height)};
        }

        private RectangleF GetFromAlphaPixels(List<float> alphaPred, int imageWidth, float threshold = 0.5F) {
            // var timer = new System.Diagnostics.Stopwatch();
            // timer.Start();
            var rows = alphaPred.Chunk(imageWidth).Select(ch => ch.ToList()).ToList();
            var minX = rows.Select(ch => ch.IndexOf(ch.FirstOrDefault(ap => ap > 0.5F))).Where(api => api != default(float)).Min();
            var minY = rows.IndexOf(rows.FirstOrDefault(ro => ro.Any(p => p > 0.5F)) ?? rows.First());
            var maxX = rows.Select(ch => ch.IndexOf(ch.LastOrDefault(ap => ap > 0.5F))).Where(api => api != default(float)).Max();
            var maxY = rows.IndexOf(rows.LastOrDefault(ro => ro.Any(ap => ap > 0.5F)) ?? rows.Last());
            // var listMethod = timer.Elapsed.TotalSeconds;
            // timer.Restart();
            var rect = Rectangle.FromLTRB(minX, minY, maxX, maxY);
            return rect;
        }
    }
}