using System.Diagnostics;
using System.Reflection;

namespace CensorCore.Web
{
    public static class CoreManager
    {
        public static string GetCoreVersion(bool preferType = true) {
            var assembly = preferType ? typeof(AIService).Assembly : Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            var version = fileVersionInfo?.ProductVersion;
            if (!string.IsNullOrWhiteSpace(version)) {
                return $"v{version}";
            } else {
                return "v0.0.0";
            }
        }
    }
}