using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CensorCore {
    public delegate void ActionRef<T>(Tensor<T> data, Point point, ref Rgba32 pixel);
    // public delegate void ActionRef<Tensor<T>, Rgba32>(T data, ref Rgba32 item);
    public abstract class TensorLoadOptions<T> {
        public Func<Image, int[]> Dimensions { get; }

        public TensorLoadOptions(Func<Image, int[]> dimensions) {
            Dimensions = dimensions;
        }

        public virtual ActionRef<T>? LoadPixel {get;protected set;}
        // public Action<Tensor<T>, Rgba32>? LoadPixel { get; set; }
    }
}
