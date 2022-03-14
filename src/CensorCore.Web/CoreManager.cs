using System.Diagnostics;
using System.Reflection;

namespace CensorCore.Web
{
    public static class CoreManager
    {
        public static string GetCoreVersion(bool preferType = true) {
            var assembly = preferType ? typeof(AIService).Assembly : Assembly.GetExecutingAssembly();
            var version = GetProductVersion(assembly);
            if (!string.IsNullOrWhiteSpace(version)) {
                return $"v{version}";
            } else {
                return "v0.0.0";
            }
        }

        private static string? GetProductVersion(Assembly assembly) {
            try {
            object[] attributes = assembly
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
        return attributes.Length == 0 ?
            null :
            ((AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;
            } catch {
                return null;
            }
        }
    }
}