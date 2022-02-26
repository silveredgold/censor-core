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

        public async Task<byte[]?> DownloadModel(bool getClassifier = false, bool preferBase = false) {
            var github = new GitHubClient(new ProductHeaderValue("CensorCore.ModelLoader"));
            var releases = await github.Repository.Release.GetAll("notAI-tech", "NudeNet");
            var checkpointRelease = releases.OrderByDescending(r => r.CreatedAt).FirstOrDefault(r => r.Assets.Any(a => a.Name.EndsWith(".onnx")));
            if (checkpointRelease != null) {
                if (getClassifier) {
                    var detectorRelease = checkpointRelease.Assets.FirstOrDefault(r => r.Name.Contains("classifier") && r.Name.EndsWith(".onnx"));
                    if (detectorRelease == null) {
                        throw new Exception("Could not locate detector asset in GitHub Releases!");
                    }
                    var baseAsset = await github.Repository.Release.GetAsset(this._owner, this._repo, detectorRelease.Id);
                    var download = await github.Connection.Get<byte[]>(new Uri(baseAsset.Url), new Dictionary<string, string>(), "application/octet-stream");
                    return download.Body;
                }
                if (!getClassifier && checkpointRelease.Assets.FirstOrDefault(r => (preferBase ? r.Name.Contains("_base_") : !r.Name.Contains("_base")) && r.Name.EndsWith(".onnx")) is var baseRelease && baseRelease != null) {
                    var baseAsset = await github.Repository.Release.GetAsset(this._owner, this._repo, baseRelease.Id);
                    var download = await github.Connection.Get<byte[]>(new Uri(baseAsset.Url), new Dictionary<string, string>(), "application/octet-stream");
                    return download.Body;
                }
                return null;
            }
            return null;
        }
    }
}