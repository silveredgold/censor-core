using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CensorCore.Censoring
{
    /// <summary>
    /// Provider for a given "style" of censoring (i.e. pixelation, blurring).
    /// </summary>
    /// <remarks>
    /// Defaults to handling censor requests with a matching type name. That is, MyMethodProvider will handle requests with a 'mymethod' type.
    /// </remarks>
    public interface ICensorTypeProvider {
        bool Supports(string censorType) {
            var typeName = this.GetType().Name.Replace("Provider", string.Empty);
            return censorType.Split('?', ':').First().Replace("Provider", string.Empty).Equals(typeName, StringComparison.InvariantCultureIgnoreCase);
        }

        int Layer { get { return 0;} }

        /// <summary>
        /// Censors a single classification result in the given image.
        /// </summary>
        /// <param name="inputImage">The base image to be censored. This may already have been partially censored by other providers/results.</param>
        /// <param name="result">The specific classification result to censor.</param>
        /// <param name="method">The requested method. Since the method will only be called if Supports() returns true, this can be used for additional parameters.</param>
        /// <param name="level">The censor "level", an integer value between 0 and 20 representing "severity" of the censoring. What this means is style-specific.</param>
        /// <returns>Either an image to be composited onto the main image (by the provider) or null, if the censoring is already completed.</returns>
        Task<Action<IImageProcessingContext>?> CensorImage(Image<Rgba32> inputImage, Classification result, string method, int level);
    }
}
