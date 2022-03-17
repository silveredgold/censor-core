namespace CensorCore.Censoring {

    public class StaticResultsParser : IResultParser {
        private readonly Dictionary<string, ImageCensorOptions> _options;
        public ImageCensorOptions? DefaultOptions { get; set; }

        public StaticResultsParser(Dictionary<string, ImageCensorOptions> options) {
            this._options = options;
        }

        public ImageCensorOptions? GetOptions(string label, ImageResult? image = null) {
            return _options.TryGetValue(label, out var opts)
                ? opts
                : DefaultOptions ?? new ImageCensorOptions("none");
        }
    }
}
