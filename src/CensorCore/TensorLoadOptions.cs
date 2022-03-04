using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp.PixelFormats;

namespace CensorCore {
    public delegate void ActionRef<T>(Tensor<T> data, ref Rgba32 item);
    // public delegate void ActionRef<Tensor<T>, Rgba32>(T data, ref Rgba32 item);
    public class TensorLoadOptions<T> {
        public int[] Dimensions { get; set; } = Array.Empty<int>();
        
        public ActionRef<T>? LoadPixel {get;set;}
        // public Action<Tensor<T>, Rgba32>? LoadPixel { get; set; }
    }
}
