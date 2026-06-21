using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silnith.CDB;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CDBPopulator
{
    class Program
    {
        /*
         * DIS Entity Type
         * \CDB\MModel\600_MModelGeometry\1_Platform\2_Air\225_United_States\21_Utility_Helo\1_2_225_21_x_x_x\D600_S001_T001_1_2_225_21_x_x_x.flt
         * \CDB\MModel\600_MModelGeometry\1_Platform\2_Air\225_United_States\21_Utility_Helo\1_2_225_21_x_x_x\D600_S001_T002_1_2_225_21_x_x_x.flt
         * \CDB\MModel\600_MModelGeometry\1_Platform\1_Land\225_United_States\1_Tank\1_1_225_1_1_3_0\
         * \CDB\MModel\601_MModelTexture\M\1\M1A2\D601_S005_T001_W11_M1A2.rgb
         * \CDB\GTModel\510_GTModelGeometry\A_Culture\T_Comm\040_Power_Pylon\Lxx\
         * \CDB\GTModel\511_GTModelTexture\P\Y\Pylon\D511_Sxxx_Txxx_Lxx_Pylon.rgb
         * \CDB\GTModel\500_GTModelGeometry\A_Culture\L_Misc_Feature\015_Building\D500_S001_T001_AL015_050_Church-Gothic.flt
         * \CDB\Tiles\_lat_\_lon_\300_GSModelGeometry\Lxx\Ux\
         * \CDB\Tiles\_lat_\_lon_\301_GSModelTexture\Lxx\Ux__\latlon___D301_Sxxx_Txxx_Lxx_Ux_Rx_TNAM.rgb
         * \CDB\GTModel\511_GTModelTexture\_T_\_N_\TNAM\D511_Sxxx_Txxx_Lxx_TNAM.rgb
         * \CDB\Tiles\_lat_\_lon_\310_T2DModelGeometry\Lxx\Ux\
         * \CDB\Tiles\_lat_\_lon_\301_GSModelTexture\Lxx\Ux__\latlon___D301_Sxxx_Txxx_Lxx_Ux_Rx_TNAM.rgb
         * \CDB\GTModel\501_GTModelTexture\_T_\_N_\_TNAM_\D511_Sxxx_Txxx_Lxx_TNAM.rgb
         * \CDB\MModel\601_MModelTexture\M\1\M1A2\D601_S004_T005_Wxx_M1A2.rgb
         * \CDB\MModel\601_MModelTexture\M\1\M1A2\D601_S005_T001_Wxx_M1A2.rgb
         */
        static readonly SortedDictionary<char, string> categories = new()
        {
            { 'A', "A_Culture" },
            { 'B', "B_Hydrography" },
            { 'C', "C_Hypsography" },
            { 'D', "D_Physiography" },
            { 'E', "E_Vegetation" },
            { 'F', "F_Demarcation" },
            { 'G', "G_Aeronautical_Information" },
            { 'I', "I_Cadastral" },
            { 'S', "S_Special_Use" },
            { 'Z', "Z_General" },
        };
        static readonly SortedDictionary<char, string> subcategories = new()
        {
            { 'C', "C_Woodland" },
            { 'D', "D_Power_Gen" },
            { 'E', "E_Fab_Industry" },
            { 'K', "K_Recreational" },
            { 'L', "L_Misc_Feature" },
            { 'T', "T_Comm" },
        };
        static readonly SortedDictionary<int, string> codes = new()
        {
            // For category A:
            // If subcategory E, then Assembly_Plant, else Power_Plant
            { 10, "010_Power_Plant" },
            { 15, "015_Building" },
            { 20, "020_Built-Up_Area" },
            { 50, "050_Display_Sign" },
            { 30, "030_Power_Line" },
            { 40, "040_Power_Pylon" },
            { 80, "080_Comm_Tower" },
            { 110, "110_Light_Standard" },
            { 240, "240_Tower-NC" },
            { 241, "241_Tower_General" },
            // For category E:
            { 030, "030_Trees" },
        };

        /*
         * https://github.com/opengeospatial/cdb-volume-1
         * Chapter 3. CDB Structure
         * 
         * All CDB files can be located strictly based on their filename.
         * The filename contains enough information to determine the path.
         * Therefore, when translating to a database, the path is irrelevant.
         * 
         * 3.1 Top Level CDB Model/Structure Description
         * 
         * \CDB\
         * \CDB\Metadata\
         * \CDB\GTModel\
         * \CDB\MModel\
         * \CDB\Tiles\
         * \CDB\Navigation\
         * 
         * 3.3.8.1. Feature Classification
         * 
         * Feature Code
         */

        private static IHost Setup(string[] args)
        {
            HostApplicationBuilder hostApplicationBuilder = Host.CreateApplicationBuilder(args);

            hostApplicationBuilder.Services.AddSingleton<DbConnectionStringBuilder, SqliteConnectionStringBuilder>(serviceProvider =>
            {
                return new()
                {
                    DataSource = ":memory:",
                    Mode = SqliteOpenMode.ReadWriteCreate,
                    Cache = SqliteCacheMode.Default,
                    ForeignKeys = true,
                    RecursiveTriggers = true,
                    Pooling = true,
                };
            });
            hostApplicationBuilder.Services.AddTransient<DbConnection, SqliteConnection>(serviceProvider =>
            {
                DbConnectionStringBuilder dbConnectionStringBuilder = serviceProvider.GetRequiredService<DbConnectionStringBuilder>();
                SqliteConnection sqliteConnection = new(dbConnectionStringBuilder.ConnectionString);
                sqliteConnection.Open();
                return sqliteConnection;
            });
            //hostApplicationBuilder.Services.AddSingleton<DbProviderFactory>(serviceProvider =>
            //{
            //    string providerInvariantName = "System.Data.Sqlite";
            //    return DbProviderFactories.GetFactory(providerInvariantName);
            //});
            //hostApplicationBuilder.Services.AddSingleton<DbDataSource>(serviceProvider =>
            //{
            //    DbProviderFactory dbProviderFactory = serviceProvider.GetRequiredService<DbProviderFactory>();
            //    DbConnectionStringBuilder dbConnectionStringBuilder = serviceProvider.GetRequiredService<DbConnectionStringBuilder>();
            //    return dbProviderFactory.CreateDataSource(dbConnectionStringBuilder.ConnectionString);
            //});
            //hostApplicationBuilder.Services.AddTransient<DbConnection>(serviceProvider =>
            //{
            //    DbDataSource dbDataSource = serviceProvider.GetRequiredService<DbDataSource>();
            //    return dbDataSource.OpenConnection();
            //});

            hostApplicationBuilder.Services.AddSingleton<FeatureCodeDirectoryVisitor>();
            hostApplicationBuilder.Services.AddSingleton<LevelOfDetailDirectoryWalker>();
            hostApplicationBuilder.Services.AddSingleton<MovingModelDirectoryVisitor>();
            hostApplicationBuilder.Services.AddSingleton<TextureDirectoryVisitor>();
            hostApplicationBuilder.Services.AddSingleton<TiledDatasetVisitor>();

            hostApplicationBuilder.Services.AddSingleton<MetadataVisitor>();
            hostApplicationBuilder.Services.AddSingleton<GTModelVisitor>();

            return hostApplicationBuilder.Build();
        }

        private static void CreateAndAttachParameter(DbCommand dbCommand, string dbParameterName, DbType dbType)
        {
            DbParameter dbParameter = dbCommand.CreateParameter();
            dbCommand.Parameters.Add(dbParameter);
            dbParameter.DbType = dbType;
            dbParameter.ParameterName = dbParameterName;
        }

        static int Main(string[] args)
        {
            using var host = Setup(args);

            ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
            // Do not close resources acquired from the dependency injection container,
            // it will do that on its own.
            DbConnection dbConnection = host.Services.GetRequiredService<DbConnection>();

            CreateSqliteSchema(dbConnection);

            string cdbName = "CDB";
            using (DbCommand dbCommand = dbConnection.CreateCommand())
            {
                const string nameParameterName = "$name";
                const string sql = $"""
                    insert into CDB (name) values ({nameParameterName})
                    """;
                dbCommand.CommandText = sql;
                CreateAndAttachParameter(dbCommand, nameParameterName, DbType.String);
                dbCommand.Prepare();
                dbCommand.Parameters[nameParameterName].Value = cdbName;
                int rowsAffected = dbCommand.ExecuteNonQuery();
            }

            const string cdbParamName = "$cdb";
            const string datasetParamName = "$dataset";
            const string cs1ParamName = "$cs1";
            const string cs2ParamName = "$cs2";
            const string lodParamName = "$lod";
            const string featureCategoryParamName = "$feature_category";
            const string featureSubcategoryParamName = "$feature_subcategory";
            const string featureTypeParamName = "$feature_type";
            const string featureSubcodeParamName = "$feature_subcode";
            const string fileTypeParamName = "$file_type";
            const string contentParamName = "$content";

            DirectoryInfo cdbRoot = new(cdbName);
            // Metadata
            {
                DirectoryInfo metadataDir = new(Path.Combine(cdbRoot.FullName, "Metadata"));
                MetadataVisitor metadataVisitor = host.Services.GetRequiredService<MetadataVisitor>();

                const string nameParamName = "$name";
                const string insertIntoMetadataStatement = $"""
                        insert into Metadata (
                            cdb,
                            name,
                            file_type,
                            content
                        ) values (
                            {cdbParamName},
                            {nameParamName},
                            {fileTypeParamName},
                            {contentParamName}
                        )
                        """;

                using DbCommand insertIntoMetadataCommand = dbConnection.CreateCommand();
                insertIntoMetadataCommand.CommandText = insertIntoMetadataStatement;
                CreateAndAttachParameter(insertIntoMetadataCommand, cdbParamName, DbType.String);
                CreateAndAttachParameter(insertIntoMetadataCommand, nameParamName, DbType.String);
                CreateAndAttachParameter(insertIntoMetadataCommand, fileTypeParamName, DbType.String);
                CreateAndAttachParameter(insertIntoMetadataCommand, contentParamName, DbType.Binary);
                insertIntoMetadataCommand.Prepare();
                insertIntoMetadataCommand.Parameters[cdbParamName].Value = cdbName;

                metadataVisitor.WalkMetadata(metadataDir, (name, ext, file) =>
                {
                    insertIntoMetadataCommand.Parameters[nameParamName].Value = name;
                    insertIntoMetadataCommand.Parameters[fileTypeParamName].Value = ext;
                    insertIntoMetadataCommand.Parameters[contentParamName].Value = File.ReadAllBytes(file.FullName);

                    logger.LogInformation("Inserting Metadata file {File}", file);
                    int rowsAffected = insertIntoMetadataCommand.ExecuteNonQuery();
                });
            }
            // GTModel
            {
                DirectoryInfo gtModelDir = new(Path.Combine(cdbRoot.FullName, "GTModel"));
                GTModelVisitor gtModelVisitor = host.Services.GetRequiredService<GTModelVisitor>();

                const string modelNameParamName = "$model_name";
                const string textureNameParamName = "$texture_name";
                const string insertIntoGeometryStatement = $"""
                    insert into Geometry (
                        cdb,
                        dataset,
                        component_selector_1,
                        component_selector_2,
                        feature_category,
                        feature_subcategory,
                        feature_type,
                        feature_subcode,
                        model_name,
                        file_type,
                        content
                    ) values (
                        {cdbParamName},
                        {datasetParamName},
                        {cs1ParamName},
                        {cs2ParamName},
                        {featureCategoryParamName},
                        {featureSubcategoryParamName},
                        {featureTypeParamName},
                        {featureSubcodeParamName},
                        {modelNameParamName},
                        {fileTypeParamName},
                        {contentParamName}
                    )
                    """;
                const string insertIntoGeometryLodStatement = $"""
                    insert into GeometryLod (
                        cdb,
                        dataset,
                        component_selector_1,
                        component_selector_2,
                        lod,
                        feature_category,
                        feature_subcategory,
                        feature_type,
                        feature_subcode,
                        model_name,
                        file_type,
                        content
                    ) values (
                        {cdbParamName},
                        {datasetParamName},
                        {cs1ParamName},
                        {cs2ParamName},
                        {lodParamName},
                        {featureCategoryParamName},
                        {featureSubcategoryParamName},
                        {featureTypeParamName},
                        {featureSubcodeParamName},
                        {modelNameParamName},
                        {fileTypeParamName},
                        {contentParamName}
                    )
                    """;
                const string insertIntoTextureStatement = $"""
                    insert into Texture (
                        cdb,
                        dataset,
                        component_selector_1,
                        component_selector_2,
                        texture_name,
                        file_type,
                        content
                    ) values (
                        {cdbParamName},
                        {datasetParamName},
                        {cs1ParamName},
                        {cs2ParamName},
                        {textureNameParamName},
                        {fileTypeParamName},
                        {contentParamName}
                    )
                    """;
                const string insertIntoTextureLodStatement = $"""
                    insert into TextureLod (
                        cdb,
                        dataset,
                        component_selector_1,
                        component_selector_2,
                        lod,
                        texture_name,
                        file_type,
                        content
                    ) values (
                        {cdbParamName},
                        {datasetParamName},
                        {cs1ParamName},
                        {cs2ParamName},
                        {lodParamName},
                        {textureNameParamName},
                        {fileTypeParamName},
                        {contentParamName}
                    )
                    """;

                using DbCommand insertIntoGeometryCommand = dbConnection.CreateCommand();
                {
                    insertIntoGeometryCommand.CommandText = insertIntoGeometryStatement;
                    CreateAndAttachParameter(insertIntoGeometryCommand, cdbParamName, DbType.String);
                    CreateAndAttachParameter(insertIntoGeometryCommand, datasetParamName, DbType.Int32);
                    CreateAndAttachParameter(insertIntoGeometryCommand, cs1ParamName, DbType.Int32);
                    CreateAndAttachParameter(insertIntoGeometryCommand, cs2ParamName, DbType.Int32);
                    CreateAndAttachParameter(insertIntoGeometryCommand, featureCategoryParamName, DbType.String);
                    CreateAndAttachParameter(insertIntoGeometryCommand, featureSubcategoryParamName, DbType.String);
                    CreateAndAttachParameter(insertIntoGeometryCommand, featureTypeParamName, DbType.Int32);
                    CreateAndAttachParameter(insertIntoGeometryCommand, featureSubcodeParamName, DbType.Int32);
                    CreateAndAttachParameter(insertIntoGeometryCommand, modelNameParamName, DbType.String);
                    CreateAndAttachParameter(insertIntoGeometryCommand, fileTypeParamName, DbType.String);
                    CreateAndAttachParameter(insertIntoGeometryCommand, contentParamName, DbType.Binary);
                    insertIntoGeometryCommand.Prepare();
                    insertIntoGeometryCommand.Parameters[cdbParamName].Value = cdbName;
                }
                using DbCommand insertIntoGeometryLodCommand = dbConnection.CreateCommand();
                {
                    insertIntoGeometryLodCommand.CommandText = insertIntoGeometryLodStatement;
                    CreateAndAttachParameter(insertIntoGeometryLodCommand, cdbParamName, DbType.String);
                    CreateAndAttachParameter(insertIntoGeometryLodCommand, datasetParamName, DbType.Int32);
                    CreateAndAttachParameter(insertIntoGeometryLodCommand, cs1ParamName, DbType.Int32);
                    CreateAndAttachParameter(insertIntoGeometryLodCommand, cs2ParamName, DbType.Int32);
                    CreateAndAttachParameter(insertIntoGeometryLodCommand, lodParamName, DbType.Int32);
                    CreateAndAttachParameter(insertIntoGeometryLodCommand, featureCategoryParamName, DbType.String);
                    CreateAndAttachParameter(insertIntoGeometryLodCommand, featureSubcategoryParamName, DbType.String);
                    CreateAndAttachParameter(insertIntoGeometryLodCommand, featureTypeParamName, DbType.Int32);
                    CreateAndAttachParameter(insertIntoGeometryLodCommand, featureSubcodeParamName, DbType.Int32);
                    CreateAndAttachParameter(insertIntoGeometryLodCommand, modelNameParamName, DbType.String);
                    CreateAndAttachParameter(insertIntoGeometryLodCommand, fileTypeParamName, DbType.String);
                    CreateAndAttachParameter(insertIntoGeometryLodCommand, contentParamName, DbType.Binary);
                    insertIntoGeometryLodCommand.Prepare();
                    insertIntoGeometryLodCommand.Parameters[cdbParamName].Value = cdbName;
                }
                using DbCommand insertIntoTextureCommand = dbConnection.CreateCommand();
                {
                    insertIntoTextureCommand.CommandText = insertIntoTextureStatement;
                    CreateAndAttachParameter(insertIntoTextureCommand, cdbParamName, DbType.String);
                    CreateAndAttachParameter(insertIntoTextureCommand, datasetParamName, DbType.Int32);
                    CreateAndAttachParameter(insertIntoTextureCommand, cs1ParamName, DbType.Int32);
                    CreateAndAttachParameter(insertIntoTextureCommand, cs2ParamName, DbType.Int32);
                    CreateAndAttachParameter(insertIntoTextureCommand, textureNameParamName, DbType.String);
                    CreateAndAttachParameter(insertIntoTextureCommand, fileTypeParamName, DbType.String);
                    CreateAndAttachParameter(insertIntoTextureCommand, contentParamName, DbType.Binary);
                    insertIntoTextureCommand.Prepare();
                    insertIntoTextureCommand.Parameters[cdbParamName].Value = cdbName;
                }
                using DbCommand insertIntoTextureLodCommand = dbConnection.CreateCommand();
                {
                    insertIntoTextureLodCommand.CommandText = insertIntoTextureLodStatement;
                    CreateAndAttachParameter(insertIntoTextureLodCommand, cdbParamName, DbType.String);
                    CreateAndAttachParameter(insertIntoTextureLodCommand, datasetParamName, DbType.Int32);
                    CreateAndAttachParameter(insertIntoTextureLodCommand, cs1ParamName, DbType.Int32);
                    CreateAndAttachParameter(insertIntoTextureLodCommand, cs2ParamName, DbType.Int32);
                    CreateAndAttachParameter(insertIntoTextureLodCommand, lodParamName, DbType.Int32);
                    CreateAndAttachParameter(insertIntoTextureLodCommand, textureNameParamName, DbType.String);
                    CreateAndAttachParameter(insertIntoTextureLodCommand, fileTypeParamName, DbType.String);
                    CreateAndAttachParameter(insertIntoTextureLodCommand, contentParamName, DbType.Binary);
                    insertIntoTextureLodCommand.Prepare();
                    insertIntoTextureLodCommand.Parameters[cdbParamName].Value = cdbName;
                }

                gtModelVisitor.WalkGeotypicalModels(gtModelDir,
                    (geometry, file) =>
                    {
                        insertIntoGeometryCommand.Parameters[datasetParamName].Value = geometry.Dataset.Value;
                        insertIntoGeometryCommand.Parameters[cs1ParamName].Value = geometry.ComponentSelector1;
                        insertIntoGeometryCommand.Parameters[cs2ParamName].Value = geometry.ComponentSelector2;
                        insertIntoGeometryCommand.Parameters[featureCategoryParamName].Value = geometry.FeatureCode.Category;
                        insertIntoGeometryCommand.Parameters[featureSubcategoryParamName].Value = geometry.FeatureCode.Subcategory;
                        insertIntoGeometryCommand.Parameters[featureTypeParamName].Value = geometry.FeatureCode.Type;
                        insertIntoGeometryCommand.Parameters[featureSubcodeParamName].Value = geometry.FeatureSubcode;
                        insertIntoGeometryCommand.Parameters[modelNameParamName].Value = geometry.ModelName;
                        insertIntoGeometryCommand.Parameters[fileTypeParamName].Value = geometry.FileType;
                        insertIntoGeometryCommand.Parameters[contentParamName].Value = File.ReadAllBytes(file.FullName);

                        int rowsAffected = insertIntoGeometryCommand.ExecuteNonQuery();
                    },
                    (geometryLod, file) =>
                    {
                        insertIntoGeometryLodCommand.Parameters[datasetParamName].Value = geometryLod.Dataset.Value;
                        insertIntoGeometryLodCommand.Parameters[cs1ParamName].Value = geometryLod.ComponentSelector1;
                        insertIntoGeometryLodCommand.Parameters[cs2ParamName].Value = geometryLod.ComponentSelector2;
                        insertIntoGeometryLodCommand.Parameters[lodParamName].Value = geometryLod.LevelOfDetail.Level;
                        insertIntoGeometryLodCommand.Parameters[featureCategoryParamName].Value = geometryLod.FeatureCode.Category;
                        insertIntoGeometryLodCommand.Parameters[featureSubcategoryParamName].Value = geometryLod.FeatureCode.Subcategory;
                        insertIntoGeometryLodCommand.Parameters[featureTypeParamName].Value = geometryLod.FeatureCode.Type;
                        insertIntoGeometryLodCommand.Parameters[featureSubcodeParamName].Value = geometryLod.FeatureSubcode;
                        insertIntoGeometryLodCommand.Parameters[modelNameParamName].Value = geometryLod.ModelName;
                        insertIntoGeometryLodCommand.Parameters[fileTypeParamName].Value = geometryLod.FileType;
                        insertIntoGeometryLodCommand.Parameters[contentParamName].Value = File.ReadAllBytes(file.FullName);

                        int rowsAffected = insertIntoGeometryLodCommand.ExecuteNonQuery();
                    },
                    (texture, file) =>
                    {
                        insertIntoTextureCommand.Parameters[datasetParamName].Value = texture.Dataset.Value;
                        insertIntoTextureCommand.Parameters[cs1ParamName].Value = texture.ComponentSelector1;
                        insertIntoTextureCommand.Parameters[cs2ParamName].Value = texture.ComponentSelector2;
                        insertIntoTextureCommand.Parameters[textureNameParamName].Value = texture.TextureName;
                        insertIntoTextureCommand.Parameters[fileTypeParamName].Value = texture.FileType;
                        insertIntoTextureCommand.Parameters[contentParamName].Value = File.ReadAllBytes(file.FullName);

                        int rowsAffected = insertIntoTextureCommand.ExecuteNonQuery();
                    },
                    (textureLod, file) =>
                    {
                        insertIntoTextureLodCommand.Parameters[datasetParamName].Value = textureLod.Dataset.Value;
                        insertIntoTextureLodCommand.Parameters[cs1ParamName].Value = textureLod.ComponentSelector1;
                        insertIntoTextureLodCommand.Parameters[cs2ParamName].Value = textureLod.ComponentSelector2;
                        insertIntoTextureLodCommand.Parameters[lodParamName].Value = textureLod.LevelOfDetail.Level;
                        insertIntoTextureLodCommand.Parameters[textureNameParamName].Value = textureLod.TextureName;
                        insertIntoTextureLodCommand.Parameters[fileTypeParamName].Value = textureLod.FileType;
                        insertIntoTextureLodCommand.Parameters[contentParamName].Value = File.ReadAllBytes(file.FullName);

                        int rowsAffected = insertIntoTextureLodCommand.ExecuteNonQuery();
                    });
            }
            // MModel
            {
                DirectoryInfo mmodelDir = new(Path.Combine(cdbRoot.FullName, "MModel"));
                /*
                 * MModel is based on the DIS Entity Type.
                 * 
                 * Applies to 600_MModelGeometry.
                 * 603
                 */
                MovingModelDirectoryVisitor movingModelDirectoryVisitor = host.Services.GetRequiredService<MovingModelDirectoryVisitor>();
                TextureDirectoryVisitor textureDirectoryVisitor = host.Services.GetRequiredService<TextureDirectoryVisitor>();
                LevelOfDetailDirectoryWalker levelOfDetailDirectoryWalker = host.Services.GetRequiredService<LevelOfDetailDirectoryWalker>();
                movingModelDirectoryVisitor.WalkDirectories(new(Path.Combine(mmodelDir.FullName, "600_MModelGeometry")), (disEntityTypeFromDirectory, dir) =>
                {
                    Regex mModelGeometryFilesPattern = new("^D(?<dataset>\\d{3})_S(?<component_selector_1>\\d{3})_T(?<component_selector_2>\\d{3})_(?<mmdc>[^.]+)\\.(?<file_type>[^.]+)$",
                        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

                    foreach (var file in dir.EnumerateFiles())
                    {
                        Match fileMatch = mModelGeometryFilesPattern.Match(file.Name);
                        if (fileMatch.Success)
                        {
                            var dataset = int.Parse(fileMatch.Groups["dataset"].Value, CultureInfo.InvariantCulture);
                            var componentSelector1 = int.Parse(fileMatch.Groups["component_selector_1"].Value, CultureInfo.InvariantCulture);
                            var componentSelector2 = int.Parse(fileMatch.Groups["component_selector_2"].Value, CultureInfo.InvariantCulture);
                            var mmdc = fileMatch.Groups["mmdc"].Value;
                            var fileType = fileMatch.Groups["file_type"].Value;

                            if (mmdc != disEntityTypeFromDirectory.MovingModelDisCode)
                            {
                                // TODO: Log an error.
                            }

                            // insert
                            const string insert = """
                                    insert into Models (
                                        cdb,
                                        dataset,
                                        component_selector_1,
                                        component_selector_2,
                                        kind,
                                        domain,
                                        country,
                                        category,
                                        subcategory,
                                        specific,
                                        extra,
                                        file_type,
                                        content
                                    ) values ()
                                    """;
                        }
                    }
                });
                /*
                 * This next bit applies to the following datasets:
                 * with size:
                 * 601_MModelTexture
                 * 604_MModelMaterial
                 * without size:
                 * 605_MModelCMT
                 */
                textureDirectoryVisitor.WalkDirectories(new DirectoryInfo(Path.Combine(mmodelDir.FullName, "601_MModelTexture")), (textureName, dir) =>
                {
                    Regex TextureFileNamePattern = new("^D(?<dataset>\\d{3})_S(?<component_selector_1>\\d{3})_T(?<component_selector_2>\\d{3})_(?<name>[^.]+)\\.(?<file_type>[^.]+)$",
                        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);
                    Regex TextureFileSizedNamePattern = new("^D(?<dataset>\\d{3})_S(?<component_selector_1>\\d{3})_T(?<component_selector_2>\\d{3})_W(?<texture_size>\\d{2})_(?<name>[^.]+)\\.(?<file_type>[^.]+)$",
                        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

                    foreach (var file in dir.EnumerateFiles())
                    {
                        Match match = TextureFileNamePattern.Match(file.Name);
                        if (match.Success)
                        {
                            var dataset = int.Parse(match.Groups["dataset"].Value, CultureInfo.InvariantCulture);
                            var componentSelector1 = int.Parse(match.Groups["component_selector_1"].Value, CultureInfo.InvariantCulture);
                            var componentSelector2 = int.Parse(match.Groups["component_selector_2"].Value, CultureInfo.InvariantCulture);
                            var name = match.Groups["name"].Value;
                            var ext = match.Groups["file_type"].Value;

                            // insert
                            const string insert = """
                                    insert into TextureMetadata (
                                        cdb,
                                        dataset,
                                        component_selector_1,
                                        component_selector_2,
                                        texture_name,
                                        file_type,
                                        content
                                    ) values ()
                                    """;
                        }
                        Match sizedMatch = TextureFileSizedNamePattern.Match(file.Name);
                        if (sizedMatch.Success)
                        {
                            var dataset = int.Parse(sizedMatch.Groups["dataset"].Value, CultureInfo.InvariantCulture);
                            var componentSelector1 = int.Parse(sizedMatch.Groups["component_selector_1"].Value, CultureInfo.InvariantCulture);
                            var componentSelector2 = int.Parse(sizedMatch.Groups["component_selector_2"].Value, CultureInfo.InvariantCulture);
                            var textureSize = int.Parse(sizedMatch.Groups["texture_size"].Value, CultureInfo.InvariantCulture);
                            var name = sizedMatch.Groups["name"].Value;
                            var ext = sizedMatch.Groups["file_type"].Value;

                            // insert
                            const string insert = """
                                    insert into TextureSized (
                                        cdb,
                                        dataset,
                                        component_selector_1,
                                        component_selector_2,
                                        texture_size,
                                        texture_name,
                                        file_type,
                                        content
                                    ) values ()
                                    """;
                        }
                    }
                });
                /*
                 * 606_MModelSignature is DIS Entity Type with an LOD.
                 */
                // D606_Snnn_Tnnn_LOD_MMDC.ext
                movingModelDirectoryVisitor.WalkDirectories(new(Path.Combine(mmodelDir.FullName, "600_MModelGeometry")), (disEntityTypeFromDirectory, dir) =>
                {
                    Regex mModelGeometryLodFilesPattern = new("^D(?<dataset>\\d{3})_S(?<component_selector_1>\\d{3})_T(?<component_selector_2>\\d{3})_(?<lod>LC?\\d{2})_(?<mmdc>[^.]+)\\.(?<file_type>[^.]+)$",
                        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

                    levelOfDetailDirectoryWalker.WalkModelGeometryDirectories(dir, (lod, lodDir) =>
                    {
                        foreach (var file in lodDir.EnumerateFiles())
                        {
                            Match lodMatch = mModelGeometryLodFilesPattern.Match(file.Name);
                            if (lodMatch.Success)
                            {
                                // TODO: Grab it!
                                var dataset = int.Parse(lodMatch.Groups["dataset"].Value, CultureInfo.InvariantCulture);
                                var componentSelector1 = int.Parse(lodMatch.Groups["component_selector_1"].Value, CultureInfo.InvariantCulture);
                                var componentSelector2 = int.Parse(lodMatch.Groups["component_selector_2"].Value, CultureInfo.InvariantCulture);
                                var lod2 = lodMatch.Groups["lod"].Value;
                                var mmdc = lodMatch.Groups["mmdc"].Value;
                                var fileType = lodMatch.Groups["file_type"].Value;

                                if (mmdc != disEntityTypeFromDirectory.MovingModelDisCode) // || lod2 != lod.Level
                                {
                                    // TODO: Log an error.
                                }

                                // insert
                                const string insert = """
                                    insert into ModelsLOD (
                                        cdb,
                                        dataset,
                                        component_selector_1,
                                        component_selector_2,
                                        lod,
                                        kind,
                                        domain,
                                        country,
                                        category,
                                        subcategory,
                                        specific,
                                        extra,
                                        file_type,
                                        content
                                    ) values ()
                                    """;
                            }
                        }
                    });
                });
            }
            // Tiles
            {
                DirectoryInfo tilesDir = new(Path.Combine(cdbRoot.FullName, "Tiles"));
                /*
                 * Tiled datasets:
                 * Elevation
                 * MinMaxElevation
                 * MaxCulture
                 * Imagery
                 * RMTexture
                 * RMDescriptor
                 * GSFeature
                 * GTFeature
                 * GeoPolitical
                 * VectorMaterial
                 * RoadNetwork
                 * RailRoadNetwork
                 * PowerLineNetwork
                 * HydrographyNetwork
                 * GSModelGeometry
                 * GSModelTexture
                 * GSModelSignature
                 * GSModelDescriptor
                 * GSModelMaterial
                 * GSModelCMT
                 * GSModelInteriorGeometry
                 * GSModelInteriorTexture
                 * GSModelInteriorDescriptor
                 * GSModelInteriorMaterial
                 * GSModelInteriorCMT
                 * T2DModelGeometry
                 * T2DModelCMT
                 * Navigation
                 */
                TiledDatasetVisitor tiledDatasetVisitor = host.Services.GetRequiredService<TiledDatasetVisitor>();

                const string cdbParameterName = "$cdb";
                const string latitudeParameterName = "$latitude";
                const string longitudeParameterName = "$longitude";
                const string datasetParameterName = "$dataset";
                const string cs1ParameterName = "$cs1";
                const string cs2ParameterName = "$cs2";
                const string lodParameterName = "$lod";
                const string upParameterName = "$up";
                const string rightParameterName = "$right";
                const string fileTypeParameterName = "$file_type";
                const string contentParameterName = "$content";
                const string insertIntoTilesStatement = $"""
                            insert into Tiles (
                                cdb,
                                latitude,
                                longitude,
                                dataset,
                                component_selector_1,
                                component_selector_2,
                                lod,
                                up,
                                right,
                                file_type,
                                content
                            ) values (
                                {cdbParameterName},
                                {latitudeParameterName},
                                {longitudeParameterName},
                                {datasetParameterName},
                                {cs1ParameterName},
                                {cs2ParameterName},
                                {lodParameterName},
                                {upParameterName},
                                {rightParameterName},
                                {fileTypeParameterName},
                                {contentParameterName}
                            )
                            """;
                using DbCommand insertIntoTilesCommand = dbConnection.CreateCommand();
                insertIntoTilesCommand.CommandText = insertIntoTilesStatement;
                CreateAndAttachParameter(insertIntoTilesCommand, cdbParameterName, DbType.String);
                CreateAndAttachParameter(insertIntoTilesCommand, latitudeParameterName, DbType.Int32);
                CreateAndAttachParameter(insertIntoTilesCommand, longitudeParameterName, DbType.Int32);
                CreateAndAttachParameter(insertIntoTilesCommand, datasetParameterName, DbType.Int32);
                CreateAndAttachParameter(insertIntoTilesCommand, cs1ParameterName, DbType.Int32);
                CreateAndAttachParameter(insertIntoTilesCommand, cs2ParameterName, DbType.Int32);
                CreateAndAttachParameter(insertIntoTilesCommand, lodParameterName, DbType.Int32);
                CreateAndAttachParameter(insertIntoTilesCommand, upParameterName, DbType.Int32);
                CreateAndAttachParameter(insertIntoTilesCommand, rightParameterName, DbType.Int32);
                CreateAndAttachParameter(insertIntoTilesCommand, fileTypeParameterName, DbType.String);
                CreateAndAttachParameter(insertIntoTilesCommand, contentParameterName, DbType.Binary);
                insertIntoTilesCommand.Prepare();
                insertIntoTilesCommand.Parameters[cdbParameterName].Value = cdbName;
                tiledDatasetVisitor.VisitFiles(tilesDir, async (tile, file) =>
                {
                    // Insert!
                    insertIntoTilesCommand.Parameters[latitudeParameterName].Value = tile.LatitudeValue.Value;
                    insertIntoTilesCommand.Parameters[longitudeParameterName].Value = tile.LongitudeValue.Value;
                    insertIntoTilesCommand.Parameters[datasetParameterName].Value = tile.DatasetValue.Value;
                    insertIntoTilesCommand.Parameters[cs1ParameterName].Value = tile.ComponentSelector1;
                    insertIntoTilesCommand.Parameters[cs2ParameterName].Value = tile.ComponentSelector2;
                    insertIntoTilesCommand.Parameters[lodParameterName].Value = tile.Level.Level;
                    insertIntoTilesCommand.Parameters[upParameterName].Value = tile.Up;
                    insertIntoTilesCommand.Parameters[rightParameterName].Value = tile.Right;
                    insertIntoTilesCommand.Parameters[fileTypeParameterName].Value = tile.FileType;
                    insertIntoTilesCommand.Parameters[contentParameterName].Value = File.ReadAllBytes(file.FullName);
                    int rowsAffected = insertIntoTilesCommand.ExecuteNonQuery();

                    /*
                     * Datasets in ZIP archives:
                     * GSModelGeometry
                     * GSModelTexture
                     * GSModelMaterial
                     * GSModelDescriptor
                     * GSModelCMT
                     * GSModelInteriorGeometry
                     * GSModelInteriorTexture
                     * GSModelInteriorMaterial
                     * GSModelInteriorDescriptor
                     * GSModelInteriorCMT
                     * GSModelMetadata
                     * name inside archive:
                     * LatLon_Dnnn_Snnn_Tnnn_LOD_Un_Rn_extra_tokens.ext
                     * Extra Tokens definitions:
                     * dataset:
                     * GSModelGeometry: FeatureCode_FSC_MODL
                     * GSModelInteriorGeometry: FeatureCode_FSC_MODL
                     * GSModelTexture: TNAM
                     * GSModelInteriorTexture: TNAM
                     * GSModelMaterial: TNAM
                     * GSModelInteriorMaterial: TNAM
                     * GSModelGeometry: FeatureCode_FSC_MODL
                     * GSModelInteriorGeometry: FeatureCode_FSC_MODL
                     * GSModelCMT: TNAM
                     * GSModelInteriorCMT: TNAM
                     * 
                     * Need an index on:
                     * latitude
                     * longitude
                     * dataset
                     * component selectors 1 & 2
                     * lod
                     * up
                     * right
                     */
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

            dbConnection.Close();

            return 0;
        }

        static void CreateSqliteSchema(DbConnection dbConnection)
        {
            int rowsAffected;
            using DbTransaction dbTransaction = dbConnection.BeginTransaction(IsolationLevel.Serializable);

            using DbCommand dbCommand = dbConnection.CreateCommand();

            dbCommand.Transaction = dbTransaction;

            dbCommand.CommandText = """
create table CDB (
    name text primary key
)
""";
            rowsAffected = dbCommand.ExecuteNonQuery();

            dbCommand.CommandText = """
create table Metadata (
    cdb text not null references CDB(name),
    name text not null,
    file_type text not null,
    content blob not null,
    primary key(cdb, name, file_type)
)
""";
            rowsAffected = dbCommand.ExecuteNonQuery();

            // Need an index on dataset (for everything)
            // Need an index on feature_category, feature_subcategory, feature_type
            dbCommand.CommandText = """
create table Geometry (
    cdb text not null references CDB(name),
    dataset integer not null,
    component_selector_1 integer not null,
    component_selector_2 integer not null,
    feature_category text not null,
    feature_subcategory text not null,
    feature_type integer not null,
    feature_subcode integer not null,
    model_name text not null,
    file_type text not null,
    content blob not null,
    primary key(
        cdb,
        dataset,
        component_selector_1,
        component_selector_2,
        feature_category,
        feature_subcategory,
        feature_type,
        feature_subcode,
        model_name,
        file_type
    )
)
""";
            rowsAffected = dbCommand.ExecuteNonQuery();

            // Need an index on feature_category, feature_subcategory, feature_type, lod
            dbCommand.CommandText = """
create table GeometryLOD (
    cdb text not null references CDB(name),
    dataset integer not null,
    component_selector_1 integer not null,
    component_selector_2 integer not null,
    lod integer not null,
    feature_category text not null,
    feature_subcategory text not null,
    feature_type integer not null,
    feature_subcode integer not null,
    model_name text not null,
    file_type text not null,
    content blob not null,
    primary key(
        cdb,
        dataset,
        component_selector_1,
        component_selector_2,
        lod,
        feature_category,
        feature_subcategory,
        feature_type,
        feature_subcode,
        model_name,
        file_type
    )
)
""";
            rowsAffected = dbCommand.ExecuteNonQuery();

            // Need an index on texture name.
            dbCommand.CommandText = """
create table Texture (
    cdb text not null references CDB(name),
    dataset integer not null,
    component_selector_1 integer not null,
    component_selector_2 integer not null,
    texture_name text not null,
    file_type text not null,
    content blob not null,
    primary key(
        cdb,
        dataset,
        component_selector_1,
        component_selector_2,
        texture_name,
        file_type
    )
)
""";
            rowsAffected = dbCommand.ExecuteNonQuery();

            // Need an index on texture name.
            dbCommand.CommandText = """
create table TextureLod (
    cdb text not null references CDB(name),
    dataset integer not null,
    component_selector_1 integer not null,
    component_selector_2 integer not null,
    lod integer not null,
    texture_name text not null,
    file_type text not null,
    content blob not null,
    primary key(
        cdb,
        dataset,
        component_selector_1,
        component_selector_2,
        lod,
        texture_name,
        file_type
    )
)
""";
            rowsAffected = dbCommand.ExecuteNonQuery();

            // Maybe an index on kind, domain, country, category.
            // Need an index on kind, domain, country, category, subcategory, specific, extra.
            dbCommand.CommandText = """
create table Models (
    cdb text not null references CDB(name),
    dataset integer not null,
    component_selector_1 integer not null,
    component_selector_2 integer not null,
    kind integer not null,
    domain integer not null,
    country integer not null,
    category integer not null,
    subcategory integer not null,
    specific integer not null,
    extra integer not null,
    file_type text not null,
    content blob not null,
    primary key(
        cdb,
        dataset,
        component_selector_1,
        component_selector_2,
        kind,
        domain,
        country,
        category,
        subcategory,
        specific,
        extra,
        file_type
    )
)
""";
            rowsAffected = dbCommand.ExecuteNonQuery();

            // Need an index on latitude, longitude, dataset, cs1, cs2, lod, up
            dbCommand.CommandText = """
create table Tiles (
    cdb text not null references CDB(name),
    latitude integer not null,
    longitude integer not null,
    dataset integer not null,
    component_selector_1 integer not null,
    component_selector_2 integer not null,
    lod integer not null,
    up integer not null,
    right integer not null,
    file_type text not null,
    content blob not null,
    primary key(
        cdb,
        latitude,
        longitude,
        dataset,
        component_selector_1,
        component_selector_2,
        lod,
        up,
        right,
        file_type
    )
)
""";
            rowsAffected = dbCommand.ExecuteNonQuery();

            dbTransaction.Commit();
        }
    }
}
