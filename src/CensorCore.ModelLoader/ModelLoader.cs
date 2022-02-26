using System.Reflection;

namespace CensorCore.ModelLoader;

public class ModelLoader {
    private readonly List<string> _searchPaths;
    private readonly List<Assembly> _searchAssemblies;
    private readonly ModelLoaderOptions _options;

    public ModelLoader(List<string> searchPaths, List<Assembly> searchAssemblies, ModelLoaderOptions opts)
    {
        this._searchPaths = searchPaths;
        this._searchAssemblies = searchAssemblies;
        this._options = opts;
    }

    public async Task<byte[]?> GetModel(string? filePath = null) {
        var local = await GetLocalModel(filePath);
        if (local == null) {
            local = await DownloadModel();
        }
        return local;
    }

    public async Task<byte[]?> GetLocalModel(string? filePath) {
        if (filePath != null && File.Exists(filePath) && Path.GetExtension(filePath) == ".onnx") {
            return File.ReadAllBytes(filePath);
        }
        else if (GetModelInDirectory(filePath, _options) is var modelFile && modelFile != null) {
            return File.ReadAllBytes(modelFile.FullName);
        }
        else {
            //filePath is null or invalid, time to start fishing.
            
            var local = _searchPaths.Select(p => GetModelInDirectory(p, _options)).FirstOrDefault(p => p != null);
            if (local != null) {
                return await File.ReadAllBytesAsync(local.FullName);
            }
            //it's getting dire. Check for an embedded model.
            var embedded = GetModelResource(Assembly.GetEntryAssembly());
            if (embedded != null) {
                return embedded;
            }
            return null;
        }
    }

    public async Task<byte[]?> DownloadModel() {
        var client = new RepositoryDownloadClient(_options.RepositorySlug);
        var model = await client.DownloadModel(_options.GetClassifier, _options.PreferBaseModel);
        return model;
    }

    private FileInfo? GetModelInDirectory(string? directoryPath, ModelLoaderOptions _options) {
        if (directoryPath != null && Directory.Exists(directoryPath) && Directory.GetFiles(directoryPath).Where(f => Path.GetExtension(f) == ".onnx") is var modelFiles && modelFiles.Any()) {
            //there's *A* model file here, check if it's good.
            var candidate = _options.GetClassifier
                ? modelFiles.FirstOrDefault(mf => mf.Name().StartsWith("classifier_"))
                : modelFiles.FirstOrDefault(mf => _options.PreferBaseModel ? mf.Name().Contains("_base_") : !mf.Name().Contains("_base"));
            return candidate == null ? null : new FileInfo(candidate);
        }
        return null;
    }

    private static byte[]? GetModelResource(Assembly? assembly = null) {

        var entryAssembly = assembly ?? typeof(ModelLoader).Assembly;

        var model = entryAssembly.GetManifestResourceNames();
        if (model != null && model.Any() && model.FirstOrDefault(r => r.EndsWith(".onnx")) is var modelResource && modelResource != null) {
            using var resourceStream = entryAssembly.GetManifestResourceStream(modelResource);
            if (resourceStream != null && resourceStream.CanRead) {
                var modelBytes = ExtractStreamResource(resourceStream);
                if (modelBytes != null && modelBytes.Length > 0) {
                    return modelBytes;
                }
            }
        }
        return null;
    }

    private static byte[]? ExtractStreamResource(Stream resFilestream) {
        if (resFilestream == null) return null;
        byte[] bytes = new byte[resFilestream.Length];
        int numBytesToRead = (int)resFilestream.Length;
        int numBytesRead = 0;
        do {
            // Read may return anything from 0 to 10.
            int n = resFilestream.Read(bytes, numBytesRead, 10);
            numBytesRead += n;
            numBytesToRead -= n;
        } while (numBytesToRead > 0);
        return bytes;
    }
}
