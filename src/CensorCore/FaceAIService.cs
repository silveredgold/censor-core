using System.Runtime.InteropServices;
using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp;

namespace CensorCore
{
    public class FaceAIService : IAIService<Point> {
        private readonly InferenceSession _session;
        private readonly IImageHandler _imageHandler;

        public bool Verbose { get ;set; } = false;

        public SessionOptions Options => new SessionOptions() {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
        };

        public static FaceAIService Create(byte[] model, IImageHandler imageHandler) {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var opts = new SessionOptions() {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
            };
            var session = new InferenceSession(model, opts);
            return new FaceAIService(session, imageHandler);
        }

        public FaceAIService(InferenceSession session, IImageHandler imageHandler) {
            this._session = session;
            this._imageHandler = imageHandler;
        }

        public async Task<ImageResult<Point>?> RunModel(byte[] data, MatchOptions? options = null) {
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

        public async Task<ImageResult<Point>?> RunModel(string url, MatchOptions? options = null) {
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

        private async Task<ImageResult<Point>?> RunModelForImage(ImageData imageData, MatchOptions? options) {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Restart();
            var modelInput = await this._imageHandler.LoadToTensor<float>(imageData, new LandmarksLoadOptions());
            timer.Stop();
            var tensorLoadTime = timer.Elapsed;
            Log($"Loaded tensor data in {timer.Elapsed.TotalSeconds}s");
            timer.Restart();
            var feeds = GetFeeds(modelInput).ToList();
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
            var result = new ImageResult<Point>(imageData, classifications) {
                Session = sessionMeta
            };
            
            Log($"Built results in {timer.Elapsed.TotalSeconds}s");
            timer.Reset();
            return result;
        }

        private IEnumerable<Point> GetResults(ImageData imgData, List<DisposableNamedOnnxValue> tensorOutput, MatchOptions matchOptions) {
            var results = tensorOutput.ToArray();
            var length = results.Length;
			var confidences = results[length - 1].AsTensor<float>().ToArray();
			var points = new Point[confidences.Length / 2];

			for (int i = 0, j = 0; i < (length = confidences.Length); i += 2)
			{
				points[j++] = new Point(
					(int)(confidences[i + 0] * imgData.SourceImage.Width),
					(int)(confidences[i + 1] * imgData.SourceImage.Height));
			}

			// dispose
			foreach (var result in results)
			{
				result.Dispose();
			}

            var left = points[36];
            var right = points[45];

            return points;
        }

        private IEnumerable<NamedOnnxValue> GetFeeds(InputImage<float> input) {
            return this._session.InputMetadata.Select(im => NamedOnnxValue.CreateFromTensor<float>(im.Key, input.Tensor));
        }
    }
}