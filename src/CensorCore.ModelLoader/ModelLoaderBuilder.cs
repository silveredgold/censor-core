using System.Reflection;

namespace CensorCore.ModelLoader;

public class ModelLoaderBuilder {
    private readonly List<string> _searchPaths;
    private readonly List<Assembly> _assemblies;
    private readonly ModelLoaderOptions _opts;

    public ModelLoaderBuilder()
    {
        this._searchPaths = new List<string>();
        this._assemblies = new List<Assembly>();
        this._opts = new ModelLoaderOptions();
    }

    public ModelLoaderBuilder AddDefaultPaths() {
        var tempDir = Path.Combine(Path.GetTempPath(), ".nudenet");
        var paths = new[] { Environment.CurrentDirectory, AppContext.BaseDirectory, tempDir};
        _searchPaths.AddRange(paths);
        return this;
    }

    public ModelLoaderBuilder AddSearchPath(string path) {
        _searchPaths.Add(path);
        return this;
    }

    public ModelLoaderBuilder SearchAssembly(Assembly? assembly) {
        if (assembly != null) {
            _assemblies.Add(assembly);
        }
        return this;
    }

    public ModelLoaderBuilder PreferBase(bool preferBaseModel = true) {
        _opts.PreferBaseModel = preferBaseModel;
        return this;
    }

    public ModelLoaderBuilder GetClassifier(bool preferClassifierModel = true) {
        _opts.GetClassifier = preferClassifierModel;
        return this;
    }

    public ModelLoaderBuilder UseAlternateRepository(string repoSlug) {
        _opts.RepositorySlug = repoSlug;
        return this;
    }
    
    public ModelLoader Build() {
        return new ModelLoader(_searchPaths, _assemblies, _opts);
    }
}
