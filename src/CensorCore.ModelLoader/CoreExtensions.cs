namespace CensorCore.ModelLoader;

internal static class CoreExtensions {
    internal static string Name(this string s) {
        return Path.GetFileNameWithoutExtension(s);
    }
}
