using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace CensorCore.Censoring {
    public interface ICensoringMiddleware {
        Task Prepare();
        Task<IEnumerable<Classification>?> OnBeforeCensoring(ImageResult image, IResultParser? parser, Action<int, Action<IImageProcessingContext>> addLateMutation);
        Task OnAfterCensoring(Image image);
    }

    public class FacialFeaturesMiddleware : ICensoringMiddleware {
        private FaceAIService? _fService;
        private ImageSharpHandler? _fileHandler;

        public FacialFeaturesMiddleware() {

        }

        public Task OnAfterCensoring(Image image) {
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<Classification>?> OnBeforeCensoring(ImageResult result, IResultParser? parser, Action<int, Action<IImageProcessingContext>> addLateMutation) {
            if (parser?.GetOptions("EYES_F", result) is not null || parser?.GetOptions("MOUTH_F", result) is not null) {
                if (_fService != null && _fileHandler != null) {
                    foreach (var faceMatch in result.Results.Where(r => r.Label.ToLower().Contains("face_f"))) {
                        var cropRect = faceMatch.Box.ToRectangle().GetPadded(faceMatch.Box.Height / 5, result.ImageData.SourceImage);
                        var extract = result.ImageData.SourceImage.Clone(x =>
                        {

                            // System.Console.WriteLine($"H: {inputImage.Height} || W: {inputImage.Width}");
                            // System.Console.WriteLine($"eY: {cropRect.Y+cropRect.Height} || W: {cropRect.X+cropRect.Width}");
                            x.Crop(cropRect);
                        });
                        float scaleFactor = (float)result.ImageData.SourceImage.Height / extract.Height;
                        extract.Save("./extracted_face.jpeg");
                        using (var ms = new MemoryStream()) {
                            extract.Save(ms, JpegFormat.Instance);
                            var data = ms.ToArray();
                            var imageData = await _fileHandler.LoadImageData(data);
                            var landmarks = await _fService.RunModel(data);
                            if (landmarks!.Results.Any()) {

                                // var points = landmarks.Results.Select(r => new Point(Convert.ToInt32(Math.Abs(r.X*scaleFactor)), Convert.ToInt32(Math.Abs(r.Y*scaleFactor)))).ToList();
                                var points = landmarks.Results.Select(p =>
                                {
                                    p.Offset(cropRect.X, cropRect.Y);
                                    return p;
                                }).ToList();
                                var eyesResult = parser?.GetOptions("EYES_F");
                                if (eyesResult != null && eyesResult.CensorType.ContainsAny(StringComparison.CurrentCultureIgnoreCase, "bars", "bb", "blackb")) {
                                    var left = points[36];
                                    var right = points[45];

                                    var eyePointPairs = new[] {
                                    new[] {points[37], points[41]},
                                    new[] {points[38], points[40]},
                                    new[] {points[43], points[47]},
                                    new[] {points[44], points[46]}};

                                    var eyeFeature = new FeatureSet(left, right, eyePointPairs.Select(ep => (Start: ep[0], End: ep[1])));
                                    var factor = eyesResult.Level.GetScaleFactor(10F);
                                    var eyeLine = eyeFeature.ToLine(factor * 3F, factor * 0.25F);
                                    addLateMutation(10, x =>
                                    {
                                        x.DrawLines(Pens.Solid(Color.Black, eyeLine.Width), eyeLine.Start, eyeLine.End);
                                    });
                                }
                                // var points = landmarks.Results;
                                var mouthResult = parser?.GetOptions("MOUTH_F");
                                if (mouthResult != null && mouthResult.CensorType.ContainsAny(StringComparison.CurrentCultureIgnoreCase, "bars", "bb", "blackb")) {
                                    var mouthPairs = new[] {
                                            (Start: points[49], End: points[59]),
                                            (Start: points[50], End: points[58]),
                                            (Start: points[51], End: points[57]),
                                            (Start: points[52], End: points[56]),
                                            (Start: points[53], End: points[55])
                                        };

                                    var mouth = new FeatureSet(points[48], points[54], mouthPairs);
                                    var mouthBox = mouth.ToLine(mouthResult.Level.GetScaleFactor(10F) * 1.5F);

                                    addLateMutation(10, x =>
                                    {
                                        x.DrawLines(Pens.Solid(Color.Black, mouthBox.Width), mouthBox.Start, mouthBox.End);
                                    });
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        public Task Prepare() {
            var lmModel = GetModelResource();
            _fileHandler = new ImageSharpHandler(112, 112);
            var opts = new SessionOptions() {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
            };
            var fSession = new InferenceSession(lmModel, opts);
            var fService = new FaceAIService(fSession, _fileHandler);
            fService.Verbose = true;
            _fService = fService;
            return Task.CompletedTask;
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

    internal class FeatureSet {
        public Point Left { get; }

        public FeatureSet(Point left, Point right, IEnumerable<(Point Start, Point End)> brackets) {
            Left = left;
            Right = right;
            Brackets = brackets;
        }

        public Point Right { get; }
        public IEnumerable<(Point Start, Point End)> Brackets { get; }

        public (float Width, Point Start, Point End) ToLine(float widthFactor = 3F, float lengthFactor = 0.25F) {
            var lineLengths = Brackets.Select(pair =>
            {
                var p1 = pair.Start;
                var p2 = pair.End;
                return p1.GetDistanceTo(p2);
            }).ToList();
            var longest = lineLengths.Max();
            var width = (float)longest * widthFactor;
            var lineLength = Left.GetDistanceTo(Right);
            var offsetRight = new Point(Convert.ToInt32(Math.Abs(Right.X + (Right.X - Left.X) / lineLength * (lineLength * lengthFactor))), Convert.ToInt32(Math.Abs(Right.Y + (Right.Y - Left.Y) / lineLength * (lineLength * 0.33))));
            var offsetLeft = new Point(Convert.ToInt32(Math.Abs(Left.X - (Right.X - Left.X) / lineLength * (lineLength * lengthFactor))), Convert.ToInt32(Math.Abs(Left.Y - (Right.Y - Left.Y) / lineLength * (lineLength * 0.33))));
            offsetLeft.Offset(0, Convert.ToInt32(Math.Abs(longest / 2)));
            offsetRight.Offset(0, Convert.ToInt32(Math.Abs(longest / 2)));
            return (Width: width, Start: offsetLeft, End: offsetRight);
        }
    }
}