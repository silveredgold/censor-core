namespace CensorCore
{
    /// <summary>
    /// The final result of classifying an image with the model.
    /// </summary>
    public class ImageResult
    {
        /// <summary>
        /// Creates a new result object with a given image and set of results.
        /// </summary>
        /// <param name="imgData">The image that was classified.</param>
        /// <param name="results">Any classification results for the given image.</param>
        public ImageResult(ImageData imgData, List<Classification> results)
        {
            this.ImageData = imgData;
            this.Results = results;
        }

        /// <summary>
        /// The source data for the image the results relate to.
        /// </summary>
        public ImageData ImageData { get; }

        /// <summary>
        /// A list of all the classification matching results for this image.
        /// </summary>
        /// <value>The classification results.</value>
        public List<Classification> Results { get; }
    }
}
