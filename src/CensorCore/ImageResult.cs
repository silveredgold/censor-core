namespace CensorCore
{
    /// <summary>
    /// The final result of classifying an image with the model.
    /// </summary>
    public class ImageResult : ImageResult<Classification>
    {
        /// <summary>
        /// Creates a new result object with a given image and set of results.
        /// </summary>
        /// <param name="imgData">The image that was classified.</param>
        /// <param name="results">Any classification results for the given image.</param>
        public ImageResult(ImageData imgData, List<Classification> results) : base(imgData, results)
        {
        }
    }

    /// <summary>
    /// The final result of classifying an image with the model.
    /// </summary>
    public class ImageResult<T>
    {
        /// <summary>
        /// Creates a new result object with a given image and set of results.
        /// </summary>
        /// <param name="imgData">The image that was classified.</param>
        /// <param name="results">Any classification results for the given image.</param>
        public ImageResult(ImageData imgData, List<T> results)
        {
            this.ImageData = imgData;
            this.Results = results;
        }

        /// <summary>
        /// The source data for the image the results relate to.
        /// </summary>
        public ImageData ImageData { get; protected set; }

        /// <summary>
        /// A list of all the classification matching results for this image.
        /// </summary>
        /// <value>The classification results.</value>
        public List<T> Results { get; protected set;}

        public SessionMetadata? Session {get; set;}
    }

    public class SessionMetadata {
        public TimeSpan ModelRunTime {get;set;}
        public TimeSpan? ImageLoadTime {get;set;}
        public TimeSpan? TensorLoadTime {get;set;}
        public string ModelName {get;set;}

        public SessionMetadata(string modelName, TimeSpan modelRunTime) {
            ModelName = modelName;
            ModelRunTime = modelRunTime;
        }
    }
}
