namespace CensorCore.Censoring {

    public class StaticResultsParser : IResultParser {
        private readonly Dictionary<string, ImageCensorOptions> _options;
        public ImageCensorOptions? DefaultOptions {get;set;}

        public StaticResultsParser(Dictionary<string, ImageCensorOptions> options)
        {
            this._options = options;
        }
        public ImageCensorOptions? GetOptions(Classification result, ImageResult? image = null) {
            return _options.TryGetValue(result.Label, out var opts)
                ? opts
                : DefaultOptions ?? new ImageCensorOptions("none");
        }
    }
}
