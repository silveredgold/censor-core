using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace CensorCore
{
    public interface IAIService<TMatch>{
        bool Verbose { get; set; }
        SessionOptions? Options { get; }

        Task<ImageResult<TMatch>?> RunModel(byte[] data, MatchOptions? options = null);
        Task<ImageResult<TMatch>?> RunModel(string url, MatchOptions? options = null);
    }

    /// <summary>
    /// Main service type for interacting with the AI model. Responsible for setting up, configurating and executing the image classifier.
    /// </summary>
    /// <remarks>
    /// This service does not perform any censoring on the image.
    /// </remarks>
    public class AIService {
        public static readonly string[] ClassList = new[] { "EXPOSED_ANUS", "EXPOSED_ARMPITS", "COVERED_BELLY", "EXPOSED_BELLY", "COVERED_BUTTOCKS", "EXPOSED_BUTTOCKS", "FACE_F", "FACE_M", "COVERED_FEET", "EXPOSED_FEET", "COVERED_BREAST_F", "EXPOSED_BREAST_F", "COVERED_GENITALIA_F", "EXPOSED_GENITALIA_F", "EXPOSED_BREAST_M", "EXPOSED_GENITALIA_M" };
        private readonly InferenceSession _session;
        private readonly IImageHandler _imageHandler;
        public bool Verbose { get; set; } = false;

        private void Log(string message) {
            if (Verbose) {
                Console.WriteLine(message);
            }
        }

        public SessionOptions Options => new SessionOptions() {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
        };

        public static async Task<AIService> CreateFromFileAsync(string modelPath, IImageHandler imageHandler) {
            var opts = new SessionOptions() {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
            };
            return await Task.Run(() =>
            {
                var session = new InferenceSession(modelPath, opts);
                return new AIService(session, imageHandler);
            });
        }

        public static AIService Create(byte[] model, IImageHandler imageHandler, bool enableAcceleration = true) {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows && enableAcceleration) {
                InferenceSession? hwSession = null;
                var deviceId = 0;
                while (hwSession == null && deviceId < 2) {
                    try {
                        var hwOpts = new SessionOptions() {

                        };
                        hwOpts.AppendExecutionProvider_DML(deviceId);
                        hwSession = new InferenceSession(model, hwOpts);
                        return new AIService(hwSession, imageHandler);
                    }
                    catch {
                        deviceId++;
                    }
                }
                Console.WriteLine("WARN: Failed to initialize hardware acceleration!");
            }
            var opts = new SessionOptions() {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
            };
            var session = new InferenceSession(model, opts);
            return new AIService(session, imageHandler);
        }
        public static AIService CreateFromFile(string modelPath, IImageHandler imageHandler) {
            return AIService.Create(File.ReadAllBytes(modelPath), imageHandler);
        }

        public AIService(byte[] modelContents, IImageHandler imageHandler) {
            this._session = new InferenceSession(modelContents, Options);
            this._imageHandler = imageHandler;
        }

        public AIService(InferenceSession session, IImageHandler imageHandler) {
            this._session = session;
            this._imageHandler = imageHandler;
        }

        public async Task<ImageResult?> RunModel(byte[] data, MatchOptions? options = null) {
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

        public async Task<ImageResult?> RunModel(string url, MatchOptions? options = null) {
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

        private async Task<ImageResult?> RunModelForImage(ImageData imageData, MatchOptions? options) {
            options ??= MatchOptions.GetDefault();
            var timer = new System.Diagnostics.Stopwatch();
            timer.Restart();
            // var modelInput = await this._imageHandler.LoadToTensor(imageData);
            var modelInput = await this._imageHandler.LoadToTensor<float>(imageData, new NudeNetLoadOptions());
            timer.Stop();
            var tensorLoadTime = timer.Elapsed;
            Log($"Loaded tensor data in {timer.Elapsed.TotalSeconds}s");
            timer.Restart();
            var feeds = GetFeeds(modelInput).ToList();
            var output = this._session.Run(feeds);
            var runTime = timer.Elapsed;
            Log($"Finished model in {timer.Elapsed.TotalSeconds}s");
            timer.Restart();
            var classifications = this.GetResults(imageData, output.ToList(), options).ToList();
            var modelName = string.IsNullOrWhiteSpace(this._session.ModelMetadata.Description)
                ? this._session.ModelMetadata.GraphName
                : this._session.ModelMetadata.Description;
            var sessionMeta = new SessionMetadata(modelName, runTime) {
                TensorLoadTime = tensorLoadTime
            };
            var result = new ImageResult(imageData, classifications) {
                Session = sessionMeta
            };
            Log($"Built results in {timer.Elapsed.TotalSeconds}s");
            timer.Reset();
            return result;
        }

        private IEnumerable<NamedOnnxValue> GetFeeds(InputImage<float> input) {
            return this._session.InputMetadata.Select(im => NamedOnnxValue.CreateFromTensor<float>(im.Key, input.Tensor));
        }

        private IEnumerable<Classification> GetResults(ImageData imgData, List<DisposableNamedOnnxValue> tensorOutput, MatchOptions matchOptions) {
            // var boxOutput = tensorOutput.First(to => to.ElementType == TensorElementType.Float && to.);
            // var scoreOutput = tensorOutput.First(to => to.Name == "output2");
            var labelOutput = tensorOutput.First(to => to.ElementType == TensorElementType.Int32); //output3

            var floatTensors = tensorOutput.Where(to => to.ElementType == TensorElementType.Float).Select(to => to.AsTensor<float>()).ToList();
            var rawScores = floatTensors.First(ft => ft.First() < 1F);
            var rawBoxes = floatTensors.First(ft => ft.First() > 0F);
            var rawLabels = labelOutput.AsTensor<int>();

            var length = rawLabels.Length;
            if (rawLabels.Last() == -1) {
                var validLabels = rawLabels.TakeWhile(l => l != -1).ToList();
                length = validLabels.Count;
            }

            var results = new List<Classification>();
            var boxes = rawBoxes.Chunk(4).ToList();

            for (int i = 0; i < length; i++) {
                var confidence = rawScores.ElementAtOrDefault(i);
                string? className = null;
                try {
                    var classNameIndex = rawLabels.ElementAt(i);
                    className = ClassList[classNameIndex];
                } catch {
                    //ignored since it's clearly fucked
                }
                if (confidence > 0 && !string.IsNullOrWhiteSpace(className) && boxes.Count > i) {
                    if (confidence >= matchOptions.GetScoreForClass(className)) {
                        var box = boxes.ElementAt(i).ToBox(imgData.ScaleFactor);
                        results.Add(new Classification(box, confidence, className));
                        // yield return new Classification(box, confidence, className);
                    }
                }
            }
            return results;
        }
    }
}
