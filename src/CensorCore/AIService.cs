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
    /// <summary>
    /// Main service type for interacting with the AI model. Responsible for setting up, configurating and executing the image classifier.
    /// </summary>
    /// <remarks>
    /// This service does not perform any censoring on the image.
    /// </remarks>
    public class AIService
    {
        public static readonly string[] ClassList = new[] {"EXPOSED_ANUS", "EXPOSED_ARMPITS", "COVERED_BELLY", "EXPOSED_BELLY", "COVERED_BUTTOCKS", "EXPOSED_BUTTOCKS", "FACE_F", "FACE_M", "COVERED_FEET", "EXPOSED_FEET", "COVERED_BREAST_F", "EXPOSED_BREAST_F", "COVERED_GENITALIA_F", "EXPOSED_GENITALIA_F", "EXPOSED_BREAST_M", "EXPOSED_GENITALIA_M"};
        private readonly InferenceSession _session;
        private readonly IImageHandler _imageHandler;

        public SessionOptions Options => new SessionOptions()
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
        };

        public static async Task<AIService> CreateFromFileAsync(string modelPath, IImageHandler imageHandler)
        {
            var opts = new SessionOptions()
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
            };
            return await Task.Run(() => {
                var session = new InferenceSession(modelPath, opts);
                return new AIService(session, imageHandler);
            });
        }

        public static AIService Create(byte[] model, IImageHandler imageHandler, bool enableAcceleration = true) {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows && enableAcceleration) {
                try {
                var hwOpts = new SessionOptions()
                {
                };
                hwOpts.AppendExecutionProvider_DML();
                var hwSession = new InferenceSession(model, hwOpts);
                return new AIService(hwSession, imageHandler);
                } catch {
                    Console.WriteLine("WARN: Failed to initialize hardware acceleration!");
                }
            }
            var opts = new SessionOptions()
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
            };
            var session = new InferenceSession(model, opts);
            return new AIService(session, imageHandler);
        }
        public static AIService CreateFromFile(string modelPath, IImageHandler imageHandler)
        {
            return AIService.Create(File.ReadAllBytes(modelPath), imageHandler);
        }

        public AIService(byte[] modelContents, IImageHandler imageHandler)
        {
            this._session = new InferenceSession(modelContents, Options);
            this._imageHandler = imageHandler;
        }

        private AIService(InferenceSession session, IImageHandler imageHandler)
        {
            this._session = session;
            this._imageHandler = imageHandler;
        }

        public async Task<ImageResult> RunModel(byte[] data) {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            var imageData = await this._imageHandler.LoadImageData(data);
            timer.Stop();
            Console.WriteLine($"Loaded image data in {timer.Elapsed.TotalSeconds}s");
            return await RunModelForImage(imageData);
        }
        
        public async Task<ImageResult> RunModel(string url)
        {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            var imageData = await this._imageHandler.LoadImage(url);
            timer.Stop();
            Console.WriteLine($"Loaded image data in {timer.Elapsed.TotalSeconds}s");
            return await RunModelForImage(imageData);
        }

        private async Task<ImageResult> RunModelForImage(ImageData imageData) {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Restart();
            var modelInput = await this._imageHandler.LoadToTensor(imageData);
            timer.Stop();
            Console.WriteLine($"Loaded tensor data in {timer.Elapsed.TotalSeconds}s");
            timer.Restart();
            var feeds = GetFeeds(modelInput).ToList();
            var output = this._session.Run(feeds);
            Console.WriteLine($"Finished model in {timer.Elapsed.TotalSeconds}s");
            timer.Restart();
            var classifications = this.GetResults(imageData, output.ToList(), MatchOptions.GetDefault()).ToList();
            var result = new ImageResult(imageData, classifications);
            Console.WriteLine($"Built results in {timer.Elapsed.TotalSeconds}s");
            timer.Reset();
            return result;
        }

        private IEnumerable<NamedOnnxValue> GetFeeds(InputImage input) {
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

            for (int i = 0; i < length; i++)
            {
                var confidence = rawScores.ElementAt(i);
                var className = ClassList[rawLabels.ElementAt(i)];
                if (confidence >= matchOptions.GetScoreForClass(className)) {
                    var box = boxes.ElementAt(i).ToBox(imgData.ScaleFactor);
                    results.Add(new Classification(box, confidence, className));
                    // yield return new Classification(box, confidence, className);
                }
            }
            return results;
        }
    }
}
