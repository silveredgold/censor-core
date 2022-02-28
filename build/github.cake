#addin nuget:?package=Octokit&version=0.50.0
using Octokit;

Task("Download-Model")
	.Does(async () => 
{

    if (!FileExists("./detector_v2_default_checkpoint.onnx")) {
        Information("Downloading model file...");
        var github = new GitHubClient(new ProductHeaderValue("CensorCore.Build"));
        var releases = await github.Repository.Release.GetAll("notAI-tech", "NudeNet");

        var checkpointRelease = releases.OrderByDescending(r => r.CreatedAt).FirstOrDefault(r => r.Assets.Any(a => a.Name.EndsWith(".onnx")));
        if (checkpointRelease != null) {
            Information($"Loading assets from '{checkpointRelease.Name}'");
            var baseRelease = checkpointRelease.Assets.FirstOrDefault(r => r.Name == "detector_v2_default_checkpoint.onnx");
            if (baseRelease != null) {
                Information($"Downloading asset '{baseRelease.Id}'...");
                var baseAsset = await github.Repository.Release.GetAsset("notAI-tech", "NudeNet", baseRelease.Id);
                var download = await github.Connection.Get<byte[]>(new Uri(baseAsset.Url), new Dictionary<string, string>(), "application/octet-stream");
                System.IO.File.WriteAllBytes(modelPath, download.Body);
            }
        }
    }
});
