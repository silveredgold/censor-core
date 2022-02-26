namespace CensorCore.ModelLoader;

public class ModelLoaderOptions {
    public bool PreferBaseModel {get;set;} = false;
    public bool GetClassifier {get;set;} = false;
    public string RepositorySlug {get;set;} = "notAI-tech/NudeNet";
}
