using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
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

        static async Task<int> Main(string[] args)
        {
            CancellationTokenSource source = new();
            CancellationToken cancellationToken = source.Token;
            SqliteConnectionStringBuilder sqliteConnectionStringBuilder = new()
            {
                DataSource = ":memory:",
            };
            using SqliteConnection sqliteConnection = new(sqliteConnectionStringBuilder.ConnectionString);
            await sqliteConnection.OpenAsync(cancellationToken);

            await CreateSqliteSchema(sqliteConnection, cancellationToken);

            DirectoryInfo cdbRoot = new("CDB");
            // Metadata
            {
                using DbCommand dbCommand = sqliteConnection.CreateCommand();
                dbCommand.CommandText = "insert into Metadata (name, file_type, content) values ($name, $file_type, $content)";
                DbParameter nameParam = dbCommand.CreateParameter();
                DbParameter typeParam = dbCommand.CreateParameter();
                DbParameter contentParam = dbCommand.CreateParameter();
                nameParam.DbType = DbType.String;
                nameParam.Direction = ParameterDirection.Input;
                nameParam.ParameterName = "$name";
                typeParam.DbType = DbType.String;
                typeParam.Direction = ParameterDirection.Input;
                typeParam.ParameterName = "$file_type";
                contentParam.DbType = DbType.String;
                contentParam.Direction = ParameterDirection.Input;
                contentParam.ParameterName = "$content";
                dbCommand.Parameters.Add(nameParam);
                dbCommand.Parameters.Add(typeParam);
                dbCommand.Parameters.Add(contentParam);
                DirectoryInfo metadataDir = new(Path.Combine(cdbRoot.FullName, "Metadata"));
                foreach (string metadataFilename in Directory.EnumerateFiles(metadataDir.FullName))
                {
                    FileInfo metadataFile = new(Path.Combine(metadataDir.FullName, metadataFilename));

                    nameParam.Value = metadataFile.Name.Remove(metadataFile.Name.Length - metadataFile.Extension.Length);
                    typeParam.Value = metadataFile.Extension.Substring(1);
                    contentParam.Value = await File.ReadAllBytesAsync(metadataFile.FullName, cancellationToken);

                    int rowsAffected = await dbCommand.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            // GTModel
            {
                /*
                 * - /GTModel/500_GTModelGeometry/A_Something/E_Something/010_Something/D500_S001_T001_something.duh
                 */
                /*
                 * GTModel contains the following datasets:
                 * GTModelGeometry
                 * GTModelTexture
                 * GTModelDescriptor
                 * GTModelMaterial
                 * GTModelCMT
                 * GTModelInteriorGeometry
                 * GTModelInteriorTexture
                 * GTModelInteriorDescriptor
                 * GTModelInteriorMaterial
                 * GTModelInteriorCMT
                 * GTModelSignature
                 */
                /*
                 * for datasets:
                 * 500_GTModelGeometry Entry File
                 * 510_GTModelGeometry Level of Detail
                 * 503_GTModelDescriptor
                 * also:
                 * 506_GTModelInteriorGeometry
                 * 508_GTModelInteriorDescriptor
                 * also:
                 * 502_GTModelSignature
                 * 512_GTModelSignature
                 */
                DirectoryInfo gtModelDir = new(Path.Combine(cdbRoot.FullName, "GTModel"));
                DirectoryInfo directoryInfo = new(Path.Combine(gtModelDir.FullName, "500_GTModelGeometry"));
                int datasetFromDirectory = 500;
                Regex gtModelGeometryFileNamePattern = new("^D(?<dataset>\\d{3})_S(?<component_selector_1>\\d{3})_T(?<component_selector_2>\\d{3})_(?<fc_category>[A-Z])(?<fc_subcategory>[A-Z])(?<fc_type>\\d{3})_(?<feature_subcode>\\d{3})_(?<modl>[^.]+)\\.(?<file_type>[^.]+)$",
                    RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);
                Regex gtModelGeometryLevelOfDetailFileNamePattern = new("^D(?<dataset>\\d{3})_S(?<component_selector_1>\\d{3})_T(?<component_selector_2>\\d{3})_L(?<lod_negated>C?)(?<lod>\\d{2})_(?<fc_category>[A-Z])(?<fc_subcategory>[A-Z])(?<fc_type>\\d{3})_(?<feature_subcode>\\d{3})_(?<modl>[^.]+)\\.(?<file_type>[^.]+)$",
                    RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);
                FeatureCode.WalkDirectories(directoryInfo, (featureCodeFromDirectory, directory) =>
                {
                    foreach (var file in directory.EnumerateFiles())
                    {
                        Match fileNameMatch = gtModelGeometryFileNamePattern.Match(file.Name);
                        /*
                         * The only difference between "model geometry" and "model descriptor" is the file type, flt versus xml.
                         */
                        if (fileNameMatch.Success)
                        {
                            var dataset = int.Parse(fileNameMatch.Groups["dataset"].Value, CultureInfo.InvariantCulture);
                            var componentSelector1 = int.Parse(fileNameMatch.Groups["component_selector_1"].Value, CultureInfo.InvariantCulture);
                            var componentSelector2 = int.Parse(fileNameMatch.Groups["component_selector_2"].Value, CultureInfo.InvariantCulture);
                            var fcCategory = fileNameMatch.Groups["fc_category"].Value;
                            var fcSubcategory = fileNameMatch.Groups["fc_subcategory"].Value;
                            var fcType = int.Parse(fileNameMatch.Groups["fc_type"].Value, CultureInfo.InvariantCulture);
                            var fsc = int.Parse(fileNameMatch.Groups["feature_subcode"].Value, CultureInfo.InvariantCulture);
                            var modl = fileNameMatch.Groups["modl"].Value;
                            var ext = fileNameMatch.Groups["file_type"].Value;

                            FeatureCode featureCode = new(fcCategory, fcSubcategory, fcType);

                            if (datasetFromDirectory != dataset
                                || featureCodeFromDirectory != featureCode)
                            {
                                // TODO: Log error.
                            }

                            // Insert into database.
                            const string insert1 = """
                                    insert into Geometry
                                    (
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
                                    ) values ()
                                    """;
                        }
                    }

                    foreach (var lodDir in directory.EnumerateDirectories())
                    {
                        if (LevelOfDetail.TryFromCode(lodDir.Name, out var lod))
                        {
                            var code = lod.Code;

                            foreach (var file in lodDir.EnumerateFiles())
                            {
                                Match fileNameMatch = gtModelGeometryLevelOfDetailFileNamePattern.Match(file.Name);
                                if (fileNameMatch.Success)
                                {
                                    var dataset = int.Parse(fileNameMatch.Groups["dataset"].Value, CultureInfo.InvariantCulture);
                                    var componentSelector1 = int.Parse(fileNameMatch.Groups["component_selector_1"].Value, CultureInfo.InvariantCulture);
                                    var componentSelector2 = int.Parse(fileNameMatch.Groups["component_selector_2"].Value, CultureInfo.InvariantCulture);
                                    var lodNegated = fileNameMatch.Groups["lod_negated"].Value;
                                    var lodValue = int.Parse(fileNameMatch.Groups["lod"].Value, CultureInfo.InvariantCulture);
                                    var fcCategory = fileNameMatch.Groups["fc_category"].Value;
                                    var fcSubcategory = fileNameMatch.Groups["fc_subcategory"].Value;
                                    var fcType = int.Parse(fileNameMatch.Groups["fc_type"].Value, CultureInfo.InvariantCulture);
                                    var fsc = int.Parse(fileNameMatch.Groups["feature_subcode"].Value, CultureInfo.InvariantCulture);
                                    var modl = fileNameMatch.Groups["modl"].Value;
                                    var ext = fileNameMatch.Groups["file_type"].Value;

                                    LevelOfDetail levelOfDetail = LevelOfDetail.FromRegexMatch(lodNegated, lodValue);
                                    FeatureCode featureCode = new(fcCategory, fcSubcategory, fcType);

                                    if (datasetFromDirectory != dataset
                                        || featureCodeFromDirectory != featureCode)
                                    {
                                        // TODO: Log error.
                                    }

                                    // Insert into database.
                                    const string insert2 = """
                                            insert into GeometryLOD
                                            (
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
                                            ) values ()
                                            """;
                                }
                            }
                        }
                    }
                });
                /*
                 * This next bit applies to the following datasets:
                 * 501_GTModelTexture
                 * 511_GTModelTexture
                 * 504_GTModelMaterial
                 * 505_GTModelCMT
                 * also:
                 * 507_GTModelInteriorTexture
                 * 509_GTModelInteriorMaterial
                 * 513_GTModelInteriorCMT
                 */
                TextureName.WalkDirectories(new DirectoryInfo(Path.Combine(gtModelDir.FullName, "501_GTModelTexture")), (textureName, dir) =>
                {
                    Regex TextureCmtFileNamePattern = new("^D(?<dataset>\\d{3})_S(?<component_selector_1>\\d{3})_T(?<component_selector_2>\\d{3})_(?<name>[^.]+)\\.(?<file_type>[^.]+)$",
                        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);
                    Regex TextureFileLodNamePattern = new("^D(?<dataset>\\d{3})_S(?<component_selector_1>\\d{3})_T(?<component_selector_2>\\d{3})_L(?<lod_negated>C?)(?<lod>\\d{2})_(?<name>[^.]+)\\.(?<file_type>[^.]+)$",
                        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

                    foreach (var file in dir.EnumerateFiles())
                    {
                        Match cmtMatch = TextureCmtFileNamePattern.Match(file.Name);
                        if (cmtMatch.Success)
                        {
                            var dataset = int.Parse(cmtMatch.Groups["dataset"].Value, CultureInfo.InvariantCulture);
                            var componentSelector1 = int.Parse(cmtMatch.Groups["component_selector_1"].Value, CultureInfo.InvariantCulture);
                            var componentSelector2 = int.Parse(cmtMatch.Groups["component_selector_2"].Value, CultureInfo.InvariantCulture);
                            var name = cmtMatch.Groups["name"].Value;
                            var ext = cmtMatch.Groups["file_type"].Value;

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
                        Match lodMatch = TextureFileLodNamePattern.Match(file.Name);
                        if (lodMatch.Success)
                        {
                            var dataset = int.Parse(lodMatch.Groups["dataset"].Value, CultureInfo.InvariantCulture);
                            var componentSelector1 = int.Parse(lodMatch.Groups["component_selector_1"].Value, CultureInfo.InvariantCulture);
                            var componentSelector2 = int.Parse(lodMatch.Groups["component_selector_2"].Value, CultureInfo.InvariantCulture);
                            var lodNegated = lodMatch.Groups["lod_negated"].Value;
                            var lodValue = int.Parse(lodMatch.Groups["lod"].Value, CultureInfo.InvariantCulture);
                            var name = lodMatch.Groups["name"].Value;
                            var ext = lodMatch.Groups["file_type"].Value;

                            LevelOfDetail levelOfDetail = LevelOfDetail.FromRegexMatch(lodNegated, lodValue);

                            // insert
                            const string insert = """
                                    insert into Textures (
                                        cdb,
                                        dataset,
                                        component_selector_1,
                                        component_selector_2,
                                        lod,
                                        texture_name,
                                        file_type,
                                        content
                                    ) values ()
                                    """;
                        }
                    }
                });
            }
            // MModel
            {
                /*
                 * MModel is based on the DIS Entity Type.
                 * 
                 * Applies to 600_MModelGeometry.
                 * 603
                 */
                DirectoryInfo mmodelDir = new(Path.Combine(cdbRoot.FullName, "MModel"));
                DisEntityType.WalkDirectories(new(Path.Combine(mmodelDir.FullName, "600_MModelGeometry")), (disEntityTypeFromDirectory, dir) =>
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
                TextureName.WalkDirectories(new DirectoryInfo(Path.Combine(mmodelDir.FullName, "601_MModelTexture")), (textureName, dir) =>
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
                DisEntityType.WalkDirectories(new(Path.Combine(mmodelDir.FullName, "600_MModelGeometry")), (disEntityTypeFromDirectory, dir) =>
                {
                    Regex mModelGeometryLodFilesPattern = new("^D(?<dataset>\\d{3})_S(?<component_selector_1>\\d{3})_T(?<component_selector_2>\\d{3})_(?<lod>LC?\\d{2})_(?<mmdc>[^.]+)\\.(?<file_type>[^.]+)$",
                        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

                    foreach (var lodDir in dir.EnumerateDirectories())
                    {
                        if (LevelOfDetail.TryFromCode(lodDir.Name, out var lod))
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

                                    if (mmdc != disEntityTypeFromDirectory.MovingModelDisCode|| lod2 != lod.Code)
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
                        }
                    }
                });
            }
            // Tiles
            {
                Regex latitudeDirectoryPattern = new("^(?<north_south>[NS])(?<latitude>\\d{2})$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
                Regex longitudeDirectoryPattern = new("^(?<east_west>[EW])(?<longitude>\\d{3})$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
                Regex layerDirectoryPattern = new("^(?<code>\\d{3})_(?<name>.+)$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
                Regex lodDirectoryPattern = new("^(?<sign>[L][C]?)(?<lod>\\d{2})$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
                Regex upDirectoryPattern = new("^U(?<up>\\d+)$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
                Regex rightDirectoryPattern = new("^R(?<right>\\d+)$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

                Regex filenamePattern1 = new("^(?<north_south>[NS])(?<latitude>\\d{2})(?<east_west>[EW])(?<longitude>\\d{3})_D(?<dataset>\\d{3})_S(?<selector1>\\d{3})_T(?<selector2>\\d{3})_(?<sign>[L][C]?)(?<lod>\\d{2})_(?<category>.)(?<subcategory>.)(?<type>\\d{3})_(?<name>.+)\\.(?<ext>.+)$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
                Regex filenamePattern2 = new("^D(?<dataset>\\d{3})_S(?<selector1>\\d{3})_T(?<selector2>\\d{3})_(?<category>.)(?<subcategory>.)(?<type>\\d{3})_(?<name>.+)\\.(?<ext>.+)$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
            }

            await sqliteConnection.CloseAsync();

            return 0;
        }

        static async Task CreateSqliteSchema(DbConnection dbConnection, CancellationToken cancellationToken = default)
        {
            int rowsAffected;
            using DbTransaction dbTransaction = await dbConnection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            using DbCommand dbCommand = dbConnection.CreateCommand();

            dbCommand.Transaction = dbTransaction;

            dbCommand.CommandText = """
create table CDB (
    name text primary key
)
""";
            rowsAffected = await dbCommand.ExecuteNonQueryAsync(cancellationToken);

            dbCommand.CommandText = """
create table Metadata (
    cdb text not null references CDB(name),
    name text not null,
    file_type text not null,
    content blob not null,
    primary key(cdb, name, file_type)
)
""";
            rowsAffected = await dbCommand.ExecuteNonQueryAsync(cancellationToken);

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
            rowsAffected = await dbCommand.ExecuteNonQueryAsync(cancellationToken);

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
            rowsAffected = await dbCommand.ExecuteNonQueryAsync(cancellationToken);

            // Need an index on texture name.
            dbCommand.CommandText = """
create table TextureMetadata (
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
            rowsAffected = await dbCommand.ExecuteNonQueryAsync(cancellationToken);

            // Need an index on texture name.
            dbCommand.CommandText = """
create table Textures (
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
            rowsAffected = await dbCommand.ExecuteNonQueryAsync(cancellationToken);

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
            rowsAffected = await dbCommand.ExecuteNonQueryAsync(cancellationToken);

            await dbTransaction.CommitAsync(cancellationToken);
        }
    }
}
