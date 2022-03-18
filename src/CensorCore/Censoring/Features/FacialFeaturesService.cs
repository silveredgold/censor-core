using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace CensorCore.Censoring.Features {
    public class FacialFeaturesService {
        public FacialFeaturesService(IAssetStore assetStore) {
            _assetStore = assetStore;
            var lmModel = GetModelResource();
            _fileHandler = new ImageSharpHandler(112, 112);
            var opts = new SessionOptions() {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
            };
            var fSession = new InferenceSession(lmModel, opts);
            var fService = new FaceAIService(fSession, _fileHandler);
            _fService = fService;
        }
        private FaceAIService? _fService;
        private ImageSharpHandler? _fileHandler;
        private readonly IAssetStore _assetStore;

        public Task Ready
        {
            get
            {
                return Task.CompletedTask;
            }
        }

        public async Task<Dictionary<string, FeatureSet>?> GetFeatures(ImageResult result, Classification faceMatch) {
            if (_fService != null) {

                var cropRect = faceMatch.Box.ToRectangle().GetPadded(faceMatch.Box.Height / 5, result.ImageData.SourceImage);
                var extract = result.ImageData.SourceImage.Clone(x => x.Crop(cropRect));
                float scaleFactor = (float)result.ImageData.SourceImage.Height / extract.Height;
                using (var ms = new MemoryStream()) {
                    extract.Save(ms, JpegFormat.Instance); //while we could match formats here, JPEG is **fast**.
                    var data = ms.ToArray();
                    var landmarks = await _fService.RunModel(data);
                    if (landmarks!.Results.Any()) {
                        var features = new Dictionary<string, FeatureSet>();
                        var points = landmarks.Results.Select(p =>
                        {
                            p.Offset(cropRect.X, cropRect.Y);
                            return p;
                        }).ToList();
                        var left = points[36];
                        var right = points[45];

                        var eyePointPairs = new[] {
                                    new[] {points[37], points[41]},
                                    new[] {points[38], points[40]},
                                    new[] {points[43], points[47]},
                                    new[] {points[44], points[46]}};

                        var eyeFeature = new FeatureSet(left, right, eyePointPairs.Select(ep => (Start: ep[0], End: ep[1])));
                        features.Add("EYES_F", eyeFeature);

                        var mouthPairs = new[] {
                                            (Start: points[49], End: points[59]),
                                            (Start: points[50], End: points[58]),
                                            (Start: points[51], End: points[57]),
                                            (Start: points[52], End: points[56]),
                                            (Start: points[53], End: points[55])
                                        };

                        var mouth = new FeatureSet(points[48], points[54], mouthPairs);
                        features.Add("MOUTH_F", mouth);
                        return features;
                    }
                }
            }
            return null;
        }

        private static byte[]? GetModelResource(System.Reflection.Assembly? assembly = null) {

            var entryAssembly = assembly ?? typeof(FacialFeaturesMiddleware).Assembly;
            var model = entryAssembly.GetManifestResourceNames();
            //TODO: this doesn't match right
            if (model != null && model.Any() && model.FirstOrDefault(r => r.EndsWith(".onnx")) is var modelResource && modelResource != null) {
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
}