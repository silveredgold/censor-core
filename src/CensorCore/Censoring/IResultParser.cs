namespace CensorCore.Censoring {
    public interface IResultParser {
        ImageCensorOptions? GetOptions(Classification result, ImageResult? image = null) {
            return GetOptions(result.Label, image);
        }
        ImageCensorOptions? GetOptions(string label, ImageResult? image = null);
    }
}
