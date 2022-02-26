///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// VERSIONING
///////////////////////////////////////////////////////////////////////////////

var packageVersion = string.Empty;
#load "build/version.cake"

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////

var solutionPath = File("./src/CensorCore.sln");
var solution = ParseSolution(solutionPath);
var projects = GetProjects(solutionPath, configuration);
var artifacts = "./dist/";

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
	// Executed BEFORE the first task.
	Information("Running tasks...");
	packageVersion = BuildVersion(fallbackVersion);
});

///////////////////////////////////////////////////////////////////////////////
// TASK DEFINITIONS
///////////////////////////////////////////////////////////////////////////////

Task("Clean")
	.Does(() =>
{
	// Clean solution directories.
	foreach(var path in projects.AllProjectPaths)
	{
		Information("Cleaning {0}", path);
		CleanDirectories(path + "/**/bin/" + configuration);
		CleanDirectories(path + "/**/obj/" + configuration);
	}
	Information("Cleaning common files...");
	CleanDirectory(artifacts);
});

Task("Restore")
	.Does(() =>
{
	// Restore all NuGet packages.
	Information("Restoring solution...");
	foreach (var project in projects.AllProjectPaths) {
		DotNetRestore(project.FullPath);
	}
});

Task("Build")
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.Does(() =>
{
	Information("Building solution...");
	var settings = new DotNetBuildSettings {
		Configuration = configuration,
		NoIncremental = true,
		ArgumentCustomization = args => args.Append($"/p:Version={packageVersion}").Append("/p:AssemblyVersion=1.0.0.0")
	};
	DotNetBuild(solutionPath, settings);
});


Task("NuGet")
	.IsDependentOn("Build")
	.Does(() =>
{
	Information("Building NuGet package");
	CreateDirectory(artifacts + "package/");
	var packSettings = new DotNetPackSettings {
		Configuration = configuration,
		NoBuild = true,
		OutputDirectory = $"{artifacts}package",
		ArgumentCustomization = args => args
			.Append($"/p:Version=\"{packageVersion}\"")
			.Append("/p:NoWarn=\"NU1701 NU1602\"")
	};
	foreach(var project in projects.SourceProjectPaths) {
		Information($"Packing {project.GetDirectoryName()}...");
		DotNetPack(project.FullPath, packSettings);
	}
});

Task("Publish-Runtime")
	.IsDependentOn("Build")
	.Does(() =>
{
	var projectDir = $"{artifacts}publish";
	CreateDirectory(projectDir);
	foreach (var project in projects.SourceProjects.Where(p => !p.Name.Contains(".Console")))
	{
		var projPath = project.Path.FullPath;
		DotNetPublish(projPath, new DotNetPublishSettings {
			OutputDirectory = projectDir + "/dotnet-any",
			Configuration = configuration,
			PublishSingleFile = false,
			PublishTrimmed = false,
			ArgumentCustomization = args => args.Append($"/p:Version={packageVersion}").Append("/p:AssemblyVersion=1.0.0.0")
		});
		var runtimes = new[] { "win-x64", "osx-x64", "linux-x64"};
		foreach (var runtime in runtimes) {
			var runtimeDir = $"{projectDir}/{runtime}";
			CreateDirectory(runtimeDir);
			Information("Publishing for {0} runtime", runtime);
			var settings = new DotNetPublishSettings {
				Runtime = runtime,
				SelfContained = true,
				Configuration = configuration,
				OutputDirectory = runtimeDir,
				// PublishSingleFile = true,
				PublishTrimmed = true,
				// IncludeNativeLibrariesForSelfExtract = true,
				ArgumentCustomization = args => args.Append($"/p:Version={packageVersion}").Append("/p:AssemblyVersion=1.0.0.0")
			};
			DotNetPublish(projPath, settings);
			CreateDirectory($"{artifacts}archive");
			Zip(runtimeDir, $"{artifacts}archive/censorcore-{runtime}.zip");
		}
	}
});

Task("Publish-Standalone")
	.IsDependentOn("Build")
	.Does(() => 
{
	var consoleDir = $"{artifacts}console";
	var runtimes = new[] { "win-x64", "osx-x64", "linux-x64"};
	var projPath = "./src/CensorCore.Console/CensorCore.Console.csproj";
	foreach (var runtime in runtimes) {
		var runtimeDir = $"{consoleDir}/{runtime}";
		CreateDirectory(runtimeDir);
		Information("Publishing for {0} runtime", runtime);
		var settings = new DotNetPublishSettings {
			Runtime = runtime,
			Configuration = configuration,
			OutputDirectory = runtimeDir,
			SelfContained = true,
			PublishSingleFile = true,
			PublishTrimmed = true,
			IncludeNativeLibrariesForSelfExtract = true,
			ArgumentCustomization = args => args
				.Append($"/p:Version={packageVersion}")
				.Append("/p:AssemblyVersion=1.0.0.0")
				.Append("/p:EmbedModel='embed'")
		};
		DotNetPublish(projPath, settings);
	}
	CreateDirectory($"{artifacts}archive");
	Zip(consoleDir, $"{artifacts}archive/censorcore-console.zip");
});

Task("Default")
	.IsDependentOn("Build");

Task("Publish")
	.IsDependentOn("Publish-Runtime")
	.IsDependentOn("Publish-Standalone");

RunTarget(target);



public class ProjectCollection {
	public IEnumerable<SolutionProject> SourceProjects {get;set;}
	public IEnumerable<DirectoryPath> SourceProjectPaths {get { return SourceProjects.Select(p => p.Path.GetDirectory()); } } 
	public IEnumerable<SolutionProject> TestProjects {get;set;}
	public IEnumerable<DirectoryPath> TestProjectPaths { get { return TestProjects.Select(p => p.Path.GetDirectory()); } }
	public IEnumerable<SolutionProject> AllProjects { get { return SourceProjects.Concat(TestProjects); } }
	public IEnumerable<DirectoryPath> AllProjectPaths { get { return AllProjects.Select(p => p.Path.GetDirectory()); } }
}

ProjectCollection GetProjects(FilePath slnPath, string configuration) {
	var solution = ParseSolution(slnPath);
	var projects = solution.Projects.Where(p => p.Type != "{2150E333-8FDC-42A3-9474-1A3956D46DE8}");
	var testAssemblies = projects.Where(p => p.Name.Contains(".Tests")).Select(p => p.Path.GetDirectory() + "/bin/" + configuration + "/" + p.Name + ".dll");
	return new ProjectCollection {
		SourceProjects = projects.Where(p => !p.Name.Contains(".Tests")),
		TestProjects = projects.Where(p => p.Name.Contains(".Tests"))
	};
}