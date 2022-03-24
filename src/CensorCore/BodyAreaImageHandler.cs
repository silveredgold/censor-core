using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using CensorCore.Censoring;

namespace CensorCore;

public enum OptimizationMode {
    None,
    Normal,
    Aggressive

}

public class BodyAreaImageHandler : IImageHandler {
    private readonly IImageHandler _imageHandler;
    private readonly OptimizationMode _mode;
    private readonly BodyAIService _bodyAi;

    public BodyAreaImageHandler(IImageHandler implHandler, OptimizationMode mode = OptimizationMode.None)
    {
        var model = GetModelResource();
        _imageHandler = implHandler;
        var opts = new SessionOptions() {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
        };
        var fSession = new InferenceSession(model, opts);
        var fService = new BodyAIService(fSession, _imageHandler);
        _mode = mode;
        _bodyAi = fService;
        _bodyAi.FilterOutput = _mode == OptimizationMode.Aggressive;
    }
    public Task<ImageData> LoadImage(string path) {
        return _imageHandler.LoadImage(path);
    }

    public Task<ImageData> LoadImageData(byte[] data) {
        return _imageHandler.LoadImageData(data);
    }

    public async Task<InputImage<T>> LoadToTensor<T>(ImageData imageData, TensorLoadOptions<T> options) {
        switch (_mode)
        {
            case OptimizationMode.None:
                return await _imageHandler.LoadToTensor<T>(imageData, options);
            case OptimizationMode.Normal:
            case OptimizationMode.Aggressive:
                return await LoadCropped(imageData, options) ?? await _imageHandler.LoadToTensor(imageData, options);
            default:
                throw new ArgumentOutOfRangeException(nameof(OptimizationMode));
        }
    }

    private async Task<InputImage<T>?> LoadCropped<T>(ImageData imageData, TensorLoadOptions<T> options) {
        try {
        var timer = new System.Diagnostics.Stopwatch();
        var result = await _bodyAi.RunModelForImage(imageData, null);
        if (result != null && result.Session != null) {
            result.Session.ImageLoadTime = timer.Elapsed;
        }
        if (result == null || !result.Results.Any()) {
            return null;
        }
        var box = result.Results.First();
        var cropped = result.ImageData.SampledImage.Clone(x => x.Crop(box.ToRectangle()));
        imageData.SampledImage = cropped;
        imageData.SampleOffset = (Point)new PointF(box.X, box.Y);
        var origHeight = result.ImageData.SourceImage.Height;
        var scaleSize = new SizeF((float)result.ImageData.SourceImage.Width / cropped.Width, (float)result.ImageData.SourceImage.Height / cropped.Height);
        // imageData.ScaleFactor = scaleSize;
        var baseResult = await _imageHandler.LoadToTensor<T>(imageData, options);
        return baseResult;
        } catch {
            return null;
        }
    }

    private static byte[]? GetModelResource(System.Reflection.Assembly? assembly = null) {

            var entryAssembly = assembly ?? typeof(FacialFeaturesMiddleware).Assembly;
            var model = entryAssembly.GetManifestResourceNames();
            //TODO: this doesn't match right
            if (model != null && model.Any() && model.FirstOrDefault(r => r.Contains("rvm") && r.EndsWith(".onnx")) is var modelResource && modelResource != null) {
                // Console.WriteLine($"reading stream from {modelResource}");
                using var resourceStream = entryAssembly.GetManifestResourceStream(modelResource);
                if (resourceStream != null && resourceStream.CanRead) {
                    using var ms = new MemoryStream();
                    resourceStream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
            return null;
        }
}