// using System.Numerics.Tensors;

using Microsoft.ML.OnnxRuntime.Tensors;

namespace CensorCore
{
    /// <summary>
    /// A simple utility type for the inputs required for classification.
    /// </summary>
    public class InputImage
    {
        /// <summary>
        /// The image data correctly formatted as a Tensor for model input.
        /// </summary>
        /// <value>The image data in Tensor form.</value>
        /// <remarks>Any pixel/array/value manipulation should have been completed before now</remarks>
        internal Tensor<float> Tensor { get; set; }

        public InputImage(Tensor<float> tensor, ImageData image)
        {
            Tensor = tensor;
            Image = image;
        }

        ImageData Image { get; set; }
    }
}
