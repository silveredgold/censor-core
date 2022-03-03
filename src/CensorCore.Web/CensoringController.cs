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

    [HttpGet("info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetInfo() {
        return Ok(new {version = CoreManager.GetCoreVersion()});
    }



    [HttpPost("censorImage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CensoredImage>> CensorImage([FromBody]CensorImageRequestBody requestBody, [FromQuery] bool returnEncoded = false) {
        var imageUrl = requestBody.ImageDataUrl ?? requestBody.ImageUrl;
        if (!string.IsNullOrWhiteSpace(imageUrl)) {
            var result = await this._ai.RunModel(imageUrl);
            IResultParser? parser = null;
            if (result != null) {
                if (requestBody.CensorOptions != null && requestBody.CensorOptions.Any()) {
                    parser = new StaticResultsParser(requestBody.CensorOptions);
                }
                var censored = await this._censor.CensorImage(result, parser);
                if (returnEncoded) {
                    return Ok(new { imageUrl = censored.ImageDataUrl, imageType = censored.MimeType});
                }
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
                var defaults = new Dictionary<string, ImageCensorOptions> {
                    ["EXPOSED_BREAST_F"] = new ImageCensorOptions("pixelate") { Level = 10 },
                    ["FACE_F"] = new ImageCensorOptions("blur") { Level = 10 },
                    ["EXPOSED_GENITALIA_F"] = new ImageCensorOptions("blackbars") { Level = 15 },
                    ["EXPOSED_BUTTOCKS"] = new ImageCensorOptions("pixelate") { Level = 14 }
                };
                var censored = await this._censor.CensorImage(result, new StaticResultsParser(defaults));
                return File(censored.ImageContents, censored.MimeType);
            } else {
                return UnprocessableEntity();
            }
        } else {
            return BadRequest();
        }
    }
}
