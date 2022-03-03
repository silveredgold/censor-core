namespace CensorCore.Censoring {
    public interface IResultParser {
        ImageCensorOptions? GetOptions(Classification result, ImageResult? image = null);
    }
}
