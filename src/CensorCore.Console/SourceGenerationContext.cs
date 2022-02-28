using System.Text.Json.Serialization;

namespace CensorCore.Console;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, List<string>>), TypeInfoPropertyName = "Dictionary")]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}
