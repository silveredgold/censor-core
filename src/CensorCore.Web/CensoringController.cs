using CensorCore.Censoring;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace CensorCore.Web;
[ApiController]
[Route("[controller]")]
public class CensoringController : ControllerBase
{
    private readonly AIService _ai;
    private readonly ICensoringProvider _censor;

    public CensoringController(AIService aiService, ICensoringProvider censoringProvider)
    {
        this._ai = aiService;
        this._censor = censoringProvider;
    }
    [HttpPost("censorImage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CensoredImage>> CensorImage([FromBody]CensorImageRequestBody requestBody) {
        var imageUrl = requestBody.ImageDataUrl ?? requestBody.ImageUrl;
        if (!string.IsNullOrWhiteSpace(imageUrl)) {
            var result = await this._ai.RunModel(imageUrl);
            if (result != null) {
                var censored = await this._censor.CensorImage(result);
                return File(censored.ImageContents, censored.MimeType);
            } else {
                return UnprocessableEntity();
            }
        } else {
            return BadRequest();
        }
    }

    [HttpGet("getCensored")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetCensoredImage([FromQuery] string url) {
        if (!string.IsNullOrWhiteSpace(url)) {
            var result = await this._ai.RunModel(url);
            if (result != null) {
                var censored = await this._censor.CensorImage(result, new StaticResultsParser());
                return File(censored.ImageContents, censored.MimeType);
            } else {
                return UnprocessableEntity();
            }
        } else {
            return BadRequest();
        }
    }
}

public class StaticResultsParser : IResultParser {

    private Dictionary<string, ImageCensorOptions> Overrides = new() {
        ["EXPOSED_BREAST_F"] = new ImageCensorOptions("pixelate") {Level = 10},
        ["FACE_F"] = new ImageCensorOptions("blur") {Level = 10},
        ["EXPOSED_GENITALIA_F"] = new ImageCensorOptions("pixelate") { Level = 20 },
        ["EXPOSED_BUTTOCKS"] = new ImageCensorOptions("blackbars") { Level = 15 }
    };
    
    public ImageCensorOptions GetOptions(Classification result, ImageResult? image = null) {
        return Overrides.TryGetValue(result.Label, out var options)
            ? options
            : new ImageCensorOptions("none");
    }
}
