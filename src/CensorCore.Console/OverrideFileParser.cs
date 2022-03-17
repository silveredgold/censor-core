using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using CensorCore.Censoring;

namespace CensorCore.Console;

public class OverrideFileParser : IResultParser {
    private readonly FileInfo _file;
    private readonly ImageCensorOptions _defaults;
    internal Dictionary<string, List<string>>? _overrides;

    public OverrideFileParser(string filePath) : this(filePath, null) {
    }

    public OverrideFileParser(string filePath, ImageCensorOptions? defaultOptions) {
        this._file = new FileInfo(filePath);
        this._defaults = defaultOptions ?? new ImageCensorOptions("blur") { Level = 10 };
    }

    public async Task<bool> Load() {
        try {
            if (_file.Exists) {
                var json = await File.ReadAllTextAsync(_file.FullName);
                var dict = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json, SourceGenerationContext.Default.Dictionary);
                if (dict != null && dict.Keys.Any()) {
                    _overrides = dict;
                    return true;
                }
            }
            return false;
        }
        catch {
            return false;
        }
    }

    public ImageCensorOptions? GetOptions(string label, ImageResult? image = null) {
        if (_overrides != null && _overrides.TryGetValue(label, out var overrideResult)) {
            return new ImageCensorOptions(overrideResult.First(), overrideResult.Count > 1 ? int.TryParse(overrideResult[1], out var oLevel) ? oLevel : 10 : 10);
        }
        return _defaults;
    }
}
