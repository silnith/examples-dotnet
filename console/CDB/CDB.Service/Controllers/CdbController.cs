using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace Silnith.CDB.Service.Controllers;

[ApiController]
[Route("CDB")]
public class CdbController : ControllerBase
{
    private readonly ILogger<CdbController> logger;

    private readonly IDataStore dataStore;

    public CdbController(ILogger<CdbController> logger,
        IDataStore dataStore)
    {
        this.logger = logger;
        this.dataStore = dataStore;
    }

    [HttpGet("{**fileNameAndPath}")]
    public IActionResult Get(string fileNameAndPath)
    {
        const string Message = $"{nameof(Get)}({{FileNameAndPath}})";
        using var _ = logger.BeginScope(Message, fileNameAndPath);

        if (dataStore.TryReadFile(fileNameAndPath, out var content))
        {
            logger.LogDebug("File found.  {Size}", content.LongLength);

            string filename = Path.GetFileName(fileNameAndPath);
            string contentType = Path.GetExtension(fileNameAndPath).ToLowerInvariant() switch
            {
                ".dbf" => "application/vnd.dbf",
                ".flt" => "model/flt",
                ".gif" => MediaTypeNames.Image.Gif,
                ".gpkg" => "application/geopackage+sqlite3",
                ".jpg" or ".jpeg" => MediaTypeNames.Image.Jpeg,
                ".jp2" or ".jpg2" => "image/jp2",
                ".jsn" or ".json" => MediaTypeNames.Application.Json,
                ".pdf" => MediaTypeNames.Application.Pdf,
                ".rgb" or ".rgba" => "image/sgi",
                ".rtf" => MediaTypeNames.Application.Rtf,
                ".shp" => "application/vnd.shp",
                ".shx" => "application/vnd.shp.shx",
                ".tif" or ".tiff" => MediaTypeNames.Image.Tiff,
                ".xml" or ".xsd" => MediaTypeNames.Application.Xml,
                ".zip" => MediaTypeNames.Application.Zip,
                _ => MediaTypeNames.Application.Octet,
            };
            return File(content, contentType, filename);
        }
        else
        {
            logger.LogDebug("File not found.");

            return NotFound();
        }
    }
}
