using Octokit;

namespace CensorCore.ModelLoader
{
    public class RepositoryDownloadClient
    {
        private readonly string _owner;
        private readonly string _repo;

        public RepositoryDownloadClient(string repoId)
        {
            this._owner = repoId.Split("/").First();
            this._repo = repoId.Split("/").Last();
        }

        public async Task<(string FileName, byte[] ModelData)?> DownloadModel(bool getClassifier = false, bool preferBase = false) {
            var github = new GitHubClient(new ProductHeaderValue("CensorCore.ModelLoader"));
            var releases = await github.Repository.Release.GetAll("notAI-tech", "NudeNet");
            var checkpointRelease = releases.OrderByDescending(r => r.CreatedAt).FirstOrDefault(r => r.Assets.Any(a => a.Name.EndsWith(".onnx")));
            if (checkpointRelease != null) {
                if (getClassifier) {
                    var classifierRelease = checkpointRelease.Assets.FirstOrDefault(r => r.Name.Contains("classifier") && r.Name.EndsWith(".onnx"));
                    if (classifierRelease == null) {
                        throw new Exception("Could not locate detector asset in GitHub Releases!");
                    }
                    var baseAsset = await github.Repository.Release.GetAsset(this._owner, this._repo, classifierRelease.Id);
                    var download = await github.Connection.Get<byte[]>(new Uri(baseAsset.Url), new Dictionary<string, string>(), "application/octet-stream");
                    return (baseAsset.Name, download.Body);
                } else {
                    var baseRelease = checkpointRelease.Assets.FirstOrDefault(r => r.IsDetector(preferBase));
                    if (baseRelease != null) {
                        var baseAsset = await github.Repository.Release.GetAsset(this._owner, this._repo, baseRelease.Id);
                        var download = await github.Connection.Get<byte[]>(new Uri(baseAsset.Url), new Dictionary<string, string>(), "application/octet-stream");
                        return (baseAsset.Name, download.Body);
                    }
                }
                return null;
            }
            return null;
        }
    }

    public static class AssetExtensions {
        public static bool IsDetector(this ReleaseAsset asset, bool preferBase) {
            var outcome = asset.Name.EndsWith(".onnx") &&
                asset.Name.Contains("detector") &&
                (preferBase 
                    ? asset.Name.Contains("_base_") 
                    : !asset.Name.Contains("_base"));
            return outcome;
        } 
    }
}