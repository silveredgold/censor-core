using System.Runtime.InteropServices;
using Microsoft.ML.OnnxRuntime;

namespace CensorCore.Runtime;
public static class AIRuntime
{
    public static AIService CreateService(byte[] model, IImageHandler imageHandler, bool enableAcceleration = true) {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            if ((isWindows || isLinux) && enableAcceleration) {
                InferenceSession? hwSession = null;
                var deviceId = 0;
                while (hwSession == null && deviceId < 2) {
                    try {
                        var hwOpts = new SessionOptions();
                        if (isWindows) {
                            hwOpts.AppendExecutionProvider_DML(deviceId);
                        } else if (isLinux) {
                            hwOpts.AppendExecutionProvider_CUDA(deviceId);
                        }
                        hwSession = new InferenceSession(model, hwOpts);
                        return new AIService(hwSession, imageHandler);
                    }
                    catch {
                        deviceId++;
                    }
                }
                Console.WriteLine("WARN: Failed to initialize hardware acceleration!");
            }
            var opts = new SessionOptions() {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
            };
            var session = new InferenceSession(model, opts);
            return new AIService(session, imageHandler);
        }
}
