namespace CensorCore {

    /// <summary>
    /// Service type responsible for loading and manipulating images.
    /// </summary>
    /// <remarks>
    /// This type is primarily to keep concerns clean, but may be altered in future to remove the hard dependency on ImageSharp
    /// </remarks>
    public interface IImageHandler {
        Task<ImageData> LoadImage(string path);
        Task<ImageData> LoadImageData(byte[] data);
        [Obsolete("Consider migrating to the typed LoadToTensor<T> method instead", false)]
        Task<InputImage> LoadToTensor(ImageData image);
        Task<InputImage<T>> LoadToTensor<T>(ImageData image, TensorLoadOptions<T> options);
    }
}
