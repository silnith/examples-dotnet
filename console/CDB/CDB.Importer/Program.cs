using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silnith.CDB.SQLite;
using Silnith.CDB.Visitor;
using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Silnith.CDB.Importer;

internal class Program
{
    private static IHost Setup(string[] args)
    {
        HostApplicationBuilder hostApplicationBuilder = Host.CreateApplicationBuilder(args);

        hostApplicationBuilder.Services.AddSingleton<DISEntityDirectoryWalker>();
        hostApplicationBuilder.Services.AddSingleton<FeatureCodeDirectoryWalker>();
        hostApplicationBuilder.Services.AddSingleton<LevelOfDetailDirectoryWalker>();
        hostApplicationBuilder.Services.AddSingleton<TextureDirectoryVisitor>();
        hostApplicationBuilder.Services.AddSingleton<TileVisitor>();

        hostApplicationBuilder.Services.AddSingleton<MetadataVisitor>();
        hostApplicationBuilder.Services.AddSingleton<GeotypicalModelVisitor>();
        hostApplicationBuilder.Services.AddSingleton<MovingModelVisitor>();
        hostApplicationBuilder.Services.AddSingleton<TileVisitor>();
        hostApplicationBuilder.Services.AddSingleton<NavigationVisitor>();

        return hostApplicationBuilder.Build();
    }

    static void Main(string[] args)
    {
        using var host = Setup(args);

        ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();

        SqliteConnectionStringBuilder connectionStringBuilder = new()
        {
            DataSource = ":memory:",
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Default,
            ForeignKeys = true,
            RecursiveTriggers = true,
            Pooling = true,
        };
        using SqliteConnection connection = new(connectionStringBuilder.ConnectionString);
        connection.Open();

        using SQLiteCDB sqliteCDB = new(connection);

        string cdbName = "CDB";
        sqliteCDB.InsertIntoCDB(cdbName);

        DirectoryInfo cdbRoot = new(cdbName);
        // Metadata
        {
            MetadataVisitor metadataVisitor = host.Services.GetRequiredService<MetadataVisitor>();

            metadataVisitor.VisitMetadata(cdbRoot, (metadata, file) =>
            {
                logger.LogInformation("Inserting Metadata file {File}", file);
                int rowsAffected = sqliteCDB.InsertIntoMetadata(cdbName, metadata, File.ReadAllBytes(file.FullName));
            });
        }
        // GTModel
        {
            GeotypicalModelVisitor gtModelVisitor = host.Services.GetRequiredService<GeotypicalModelVisitor>();

            gtModelVisitor.VisitGeotypicalModels(cdbRoot,
                (geotypicalModel, file) =>
                {
                    int rowsAffected = sqliteCDB.InsertIntoGeotypicalModel(cdbName, geotypicalModel, File.ReadAllBytes(file.FullName));
                },
                (geotypicalModelLod, file) =>
                {
                    int rowsAffected = sqliteCDB.InsertIntoGeotypicalModelLod(cdbName, geotypicalModelLod, File.ReadAllBytes(file.FullName));
                },
                (texture, file) =>
                {
                    int rowsAffected = sqliteCDB.InsertIntoTexture(cdbName, texture, File.ReadAllBytes(file.FullName));
                },
                (textureLod, file) =>
                {
                    int rowsAffected = sqliteCDB.InsertIntoTextureLod(cdbName, textureLod, File.ReadAllBytes(file.FullName));
                });
        }
        // MModel
        {
            MovingModelVisitor movingModelVisitor = host.Services.GetRequiredService<MovingModelVisitor>();

            movingModelVisitor.VisitMovingModels(cdbRoot,
                (movingModel, file) =>
                {
                    int rowsAffected = sqliteCDB.InsertIntoMovingModel(cdbName, movingModel, File.ReadAllBytes(file.FullName));
                },
                (movingModelLod, file) =>
                {
                    int rowsAffected = sqliteCDB.InsertIntoMovingModelLod(cdbName, movingModelLod, File.ReadAllBytes(file.FullName));
                },
                (texture, file) =>
                {
                    int rowsAffected = sqliteCDB.InsertIntoTexture(cdbName, texture, File.ReadAllBytes(file.FullName));
                },
                (textureLod, file) =>
                {
                    int rowsAffected = sqliteCDB.InsertIntoTextureLod(cdbName, textureLod, File.ReadAllBytes(file.FullName));
                });
        }
        // Tiles
        {
            TileVisitor tiledDatasetVisitor = host.Services.GetRequiredService<TileVisitor>();

            tiledDatasetVisitor.VisitTiles(cdbRoot, (tile, file) =>
            {
                int rowsAffected = sqliteCDB.InsertIntoTile(cdbName, tile, File.ReadAllBytes(file.FullName));

                Regex zippedTiledDatasetFilenamePatternFeature = new(
                    "^(?<north_south>[NS])(?<latitude>\\d{2})(?<east_west>[EW])(?<longitude>\\d{3})_D(?<dataset>\\d{3})_S(?<selector1>\\d{3})_T(?<selector2>\\d{3})_L(?<lod_negated>C?)(?<lod>\\d{2})_U(?<up>\\d+)_R(?<right>\\d+)_(?<feature_category>[A-Z])(?<feature_subcategory>[A-Z])(?<feature_type>\\d{3})_(?<feature_subcode>\\d{3})_(?<model_name>[^.]+)\\.(?<ext>[^.]+)$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
                Regex zippedTiledDatasetFilenamePatternTexture = new(
                    "^(?<north_south>[NS])(?<latitude>\\d{2})(?<east_west>[EW])(?<longitude>\\d{3})_D(?<dataset>\\d{3})_S(?<selector1>\\d{3})_T(?<selector2>\\d{3})_L(?<lod_negated>C?)(?<lod>\\d{2})_U(?<up>\\d+)_R(?<right>\\d+)_(?<texture_name>[^.]+)\\.(?<ext>[^.]+)$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
                if (CultureInfo.InvariantCulture.CompareInfo.Compare(tile.FileType, "zip", CompareOptions.IgnoreCase) == 0)
                {
                    using ZipArchive zipArchive = ZipFile.OpenRead(file.FullName);
                    foreach (var entry in zipArchive.Entries)
                    {
                        /*
                         * Unfortunately, file names that match the "feature code" pattern
                         * can also match the "texture name" pattern, because it just groups
                         * everything after the known stuff as the name of a texture.
                         * Therefore, order is crucial here.
                         */
                        Match featureMatch = zippedTiledDatasetFilenamePatternFeature.Match(entry.Name);
                        if (featureMatch.Success)
                        {
                            Latitude latitude = Latitude.FromRegexMatch(
                                    featureMatch.Groups["north_south"].Value,
                                    featureMatch.Groups["latitude"].Value);
                            Longitude longitude = Longitude.FromRegexMatch(
                                featureMatch.Groups["east_west"].Value,
                                featureMatch.Groups["longitude"].Value);
                            int dataset = int.Parse(featureMatch.Groups["dataset"].Value, CultureInfo.InvariantCulture);
                            int componentSelector1 = int.Parse(featureMatch.Groups["selector1"].Value, CultureInfo.InvariantCulture);
                            int componentSelector2 = int.Parse(featureMatch.Groups["selector2"].Value, CultureInfo.InvariantCulture);
                            LevelOfDetail levelOfDetail = LevelOfDetail.FromRegexMatch(
                                featureMatch.Groups["lod_negated"].Value,
                                featureMatch.Groups["lod"].Value);
                            int up = int.Parse(featureMatch.Groups["up"].Value, CultureInfo.InvariantCulture);
                            int right = int.Parse(featureMatch.Groups["right"].Value, CultureInfo.InvariantCulture);
                            FeatureCode featureCode = new(
                                featureMatch.Groups["feature_category"].Value,
                                featureMatch.Groups["feature_subcategory"].Value,
                                int.Parse(featureMatch.Groups["feature_type"].Value, CultureInfo.InvariantCulture));
                            int featureSubcode = int.Parse(featureMatch.Groups["feature_subcode"].Value, CultureInfo.InvariantCulture);
                            string modelName = featureMatch.Groups["model_name"].Value;
                            string fileType = featureMatch.Groups["ext"].Value;
                        }
                        else
                        {
                            Match textureMatch = zippedTiledDatasetFilenamePatternTexture.Match(entry.Name);
                            if (textureMatch.Success)
                            {
                                Latitude latitude = Latitude.FromRegexMatch(
                                        textureMatch.Groups["north_south"].Value,
                                        textureMatch.Groups["latitude"].Value);
                                Longitude longitude = Longitude.FromRegexMatch(
                                    textureMatch.Groups["east_west"].Value,
                                    textureMatch.Groups["longitude"].Value);
                                int dataset = int.Parse(textureMatch.Groups["dataset"].Value, CultureInfo.InvariantCulture);
                                int componentSelector1 = int.Parse(textureMatch.Groups["selector1"].Value, CultureInfo.InvariantCulture);
                                int componentSelector2 = int.Parse(textureMatch.Groups["selector2"].Value, CultureInfo.InvariantCulture);
                                LevelOfDetail levelOfDetail = LevelOfDetail.FromRegexMatch(
                                    textureMatch.Groups["lod_negated"].Value,
                                    textureMatch.Groups["lod"].Value);
                                int up = int.Parse(textureMatch.Groups["up"].Value, CultureInfo.InvariantCulture);
                                int right = int.Parse(textureMatch.Groups["right"].Value, CultureInfo.InvariantCulture);
                                string textureName = textureMatch.Groups["texture_name"].Value;
                                string fileType = textureMatch.Groups["ext"].Value;
                            }
                            else
                            {
                                // Unrecognized file, ignore it.
                            }
                        }
                    }
                }
            });
        }
        // Navigation
        {
            NavigationVisitor navigationVisitor = host.Services.GetRequiredService<NavigationVisitor>();
            navigationVisitor.VisitNavigationDatasets(cdbRoot, (navigation, file) =>
            {
                int rowsAffected = sqliteCDB.InsertIntoNavigation(cdbName, navigation, File.ReadAllBytes(file.FullName));
            });
        }

        connection.Close();
    }
}
