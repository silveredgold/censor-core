using CensorCore.Censoring;

namespace CensorCore.Web;

public class CensorImageRequestBody {
    public string? ImageUrl {get;set;}
    public string? ImageDataUrl {get;set;}
    public Dictionary<string, ImageCensorOptions> CensorOptions {get;set;} = new Dictionary<string, ImageCensorOptions>();
}
