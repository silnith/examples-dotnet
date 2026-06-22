using Microsoft.AspNetCore.Mvc;
using System.Data.Common;
using System.Data;
using System.Net.Mime;
using System.Text.RegularExpressions;

namespace Silnith.CDB.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class TilesController : ControllerBase
{
    private static Regex LocationPattern
    {
        get;
    } = new(@"^(?<north_south>[NS])(?<latitude>\d{2})(?<east_west>[EW])(?<longitude>\d{3})$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    private readonly ILogger<TilesController> _logger;

    public TilesController(ILogger<TilesController> logger)
    {
        _logger = logger;
    }

    [HttpGet("/{location:regex(^[[NS]]\\d{{2}}[[EW]]\\d{{3}}$)}_D{dataset:int}_S{cs1:int}_T{cs2:int}_U{up:int}_R{right:int}.{ext}")]
    public async Task<ActionResult> GetFileAsync(
        [FromRoute(Name = "location")] string location,
        [FromRoute(Name = "dataset")] int dataset,
        [FromRoute(Name = "cs1")] int cs1,
        [FromRoute(Name = "cs2")] int cs2,
        [FromRoute(Name = "up")] int up,
        [FromRoute(Name = "right")] int right,
        [FromRoute(Name = "ext")] string ext,
        [FromServices] DbConnection dbConnection,
        CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Request: {Location} {Dataset} {ComponentSelector1} {ComponentSelector2} {Up} {Right} {FileType}",
            location, dataset, cs1, cs2, up, right, ext);

        Match match = LocationPattern.Match(location);
        if (!match.Success)
        {
            return NotFound();
        }

        Latitude latitude = Latitude.FromRegexMatch(match.Groups["north_south"].Value, match.Groups["latitude"].Value);
        Longitude longitude = Longitude.FromRegexMatch(match.Groups["east_west"].Value, match.Groups["longitude"].Value);

        DbCommand dbCommand = dbConnection.CreateCommand();
        Response.RegisterForDisposeAsync(dbCommand);

        const string cdbParameterName = "$cdb";
        const string latitudeParameterName = "$latitude";
        const string longitudeParameterName = "$longitude";
        const string datasetParameterName = "$dataset";
        const string componentSelector1ParameterName = "$component_selector_1";
        const string componentSelector2ParameterName = "$component_selector_2";
        const string upParameterName = "$up";
        const string rightParameterName = "$right";
        const string fileTypeParameterName = "$file_type";
        const string sql = $"""
                select content
                from Tiles
                where cdb = {cdbParameterName}
                    and latitude = {latitudeParameterName}
                    and longitude = {longitudeParameterName}
                    and dataset = {datasetParameterName}
                    and component_selector_1 = {componentSelector1ParameterName}
                    and component_selector_2 = {componentSelector2ParameterName}
                    and up = {upParameterName}
                    and right = {rightParameterName}
                    and file_type = {fileTypeParameterName}
                """;
        dbCommand.CommandText = sql;
        CreateAndAttachParameter(dbCommand, cdbParameterName, DbType.String);
        CreateAndAttachParameter(dbCommand, latitudeParameterName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, longitudeParameterName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, datasetParameterName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, componentSelector1ParameterName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, componentSelector2ParameterName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, upParameterName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, rightParameterName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, fileTypeParameterName, DbType.String);

        await dbCommand.PrepareAsync(cancellationToken);

        dbCommand.Parameters[cdbParameterName].Value = "CDB";
        dbCommand.Parameters[latitudeParameterName].Value = latitude.Value;
        dbCommand.Parameters[longitudeParameterName].Value = longitude.Value;
        dbCommand.Parameters[datasetParameterName].Value = dataset;
        dbCommand.Parameters[componentSelector1ParameterName].Value = cs1;
        dbCommand.Parameters[componentSelector2ParameterName].Value = cs2;
        dbCommand.Parameters[upParameterName].Value = up;
        dbCommand.Parameters[rightParameterName].Value = right;
        dbCommand.Parameters[fileTypeParameterName].Value = ext;

        DbDataReader dbDataReader = await dbCommand.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow,
            cancellationToken);
        Response.RegisterForDisposeAsync(dbDataReader);
        do
        {
            while (await dbDataReader.ReadAsync(cancellationToken))
            {
                //Stream stream2 = await dbDataReader.GetFieldValueAsync<Stream>("content", cancellationToken);
                Stream stream = dbDataReader.GetStream("content");
                Response.RegisterForDisposeAsync(stream);
                string contentType = ext switch
                {
                    "xml" => MediaTypeNames.Application.Xml,
                    "zip" => MediaTypeNames.Application.Zip,
                    "gif" => MediaTypeNames.Image.Gif,
                    "jpg" => MediaTypeNames.Image.Jpeg,
                    "jpeg" => MediaTypeNames.Image.Jpeg,
                    "tif" => MediaTypeNames.Image.Tiff,
                    "txt" => MediaTypeNames.Text.Plain,
                    _ => MediaTypeNames.Application.Octet,
                };
                return new FileStreamResult(stream, contentType)
                {
                    FileDownloadName = $"{latitude.Code}{longitude.Code}_D{500:D3}_S{1:D3}_T{2:D3}_U{3:D}_R{7:D}.{ext}",
                };
            }
        } while (await dbDataReader.NextResultAsync(cancellationToken));

        return NotFound();
    }

    private static void CreateAndAttachParameter(DbCommand dbCommand, string dbParameterName, DbType dbType)
    {
        DbParameter dbParameter = dbCommand.CreateParameter();
        dbCommand.Parameters.Add(dbParameter);
        dbParameter.DbType = dbType;
        dbParameter.ParameterName = dbParameterName;
    }
}
