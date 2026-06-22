using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silnith.CDB;
using Silnith.CDB.Visitor;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

namespace CDBPopulator
{
    class Program
    {
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

            hostApplicationBuilder.Services.AddSingleton<DISEntityDirectoryWalker>();
            hostApplicationBuilder.Services.AddSingleton<FeatureCodeDirectoryWalker>();
            hostApplicationBuilder.Services.AddSingleton<LevelOfDetailDirectoryWalker>();
            hostApplicationBuilder.Services.AddSingleton<TextureDirectoryVisitor>();
            hostApplicationBuilder.Services.AddSingleton<TiledDatasetVisitor>();

            hostApplicationBuilder.Services.AddSingleton<MetadataVisitor>();
            hostApplicationBuilder.Services.AddSingleton<GTModelVisitor>();
            hostApplicationBuilder.Services.AddSingleton<MovingModelVisitor>();
            hostApplicationBuilder.Services.AddSingleton<TiledDatasetVisitor>();
            hostApplicationBuilder.Services.AddSingleton<NavigationVisitor>();

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
            using (DbCommand insertCdbCommand = dbConnection.CreateCommand())
            {
                const string nameParamName = "$name";
                const string insertCdbStatement = $"""
                    insert into CDB (name) values ({nameParamName})
                    """;
                insertCdbCommand.CommandText = insertCdbStatement;
                CreateAndAttachParameter(insertCdbCommand, nameParamName, DbType.String);
                insertCdbCommand.Prepare();
                insertCdbCommand.Parameters[nameParamName].Value = cdbName;
                int rowsAffected = insertCdbCommand.ExecuteNonQuery();
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

            const string modelNameParamName = "$model_name";
            const string textureNameParamName = "$texture_name";
            const string sizeParamName = "$size";

            const string kindParamName = "$kind";
            const string domainParamName = "$domain";
            const string countryParamName = "$country";
            const string categoryParamName = "$category";
            const string subcategoryParamName = "$subcategory";
            const string specificParamName = "$specific";
            const string extraParamName = "$extra";

            const string latitudeParamName = "$latitude";
            const string longitudeParamName = "$longitude";
            const string upParamName = "$up";
            const string rightParamName = "$right";

            using DbCommand insertIntoGeometryCommand = dbConnection.CreateCommand();
            {
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
            using DbCommand insertIntoTextureSizedCommand = dbConnection.CreateCommand();
            {
                const string insertIntoTextureSizedStatement = $"""
                        insert into TextureSized (
                            cdb,
                            dataset,
                            component_selector_1,
                            component_selector_2,
                            size,
                            texture_name,
                            file_type,
                            content
                        ) values (
                            {cdbParamName},
                            {datasetParamName},
                            {cs1ParamName},
                            {cs2ParamName},
                            {sizeParamName},
                            {textureNameParamName},
                            {fileTypeParamName},
                            {contentParamName}
                        )
                        """;

                insertIntoTextureSizedCommand.CommandText = insertIntoTextureSizedStatement;
                CreateAndAttachParameter(insertIntoTextureSizedCommand, cdbParamName, DbType.String);
                CreateAndAttachParameter(insertIntoTextureSizedCommand, datasetParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoTextureSizedCommand, cs1ParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoTextureSizedCommand, cs2ParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoTextureSizedCommand, sizeParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoTextureSizedCommand, textureNameParamName, DbType.String);
                CreateAndAttachParameter(insertIntoTextureSizedCommand, fileTypeParamName, DbType.String);
                CreateAndAttachParameter(insertIntoTextureSizedCommand, contentParamName, DbType.Binary);
                insertIntoTextureSizedCommand.Prepare();
                insertIntoTextureSizedCommand.Parameters[cdbParamName].Value = cdbName;
            }

            using DbCommand insertIntoModelsCommand = dbConnection.CreateCommand();
            {
                const string insertIntoModelsStatement = $"""
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
                        ) values (
                            {cdbParamName},
                            {datasetParamName},
                            {cs1ParamName},
                            {cs2ParamName},
                            {kindParamName},
                            {domainParamName},
                            {countryParamName},
                            {categoryParamName},
                            {subcategoryParamName},
                            {specificParamName},
                            {extraParamName},
                            {fileTypeParamName},
                            {contentParamName}
                        )
                        """;

                insertIntoModelsCommand.CommandText = insertIntoModelsStatement;
                CreateAndAttachParameter(insertIntoModelsCommand, cdbParamName, DbType.String);
                CreateAndAttachParameter(insertIntoModelsCommand, datasetParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsCommand, cs1ParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsCommand, cs2ParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsCommand, kindParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsCommand, domainParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsCommand, countryParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsCommand, categoryParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsCommand, subcategoryParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsCommand, specificParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsCommand, extraParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsCommand, fileTypeParamName, DbType.String);
                CreateAndAttachParameter(insertIntoModelsCommand, contentParamName, DbType.String);
                insertIntoModelsCommand.Prepare();
                insertIntoModelsCommand.Parameters[cdbParamName].Value = cdbName;
            }
            using DbCommand insertIntoModelsLodCommand = dbConnection.CreateCommand();
            {
                const string insertIntoModelsLodStatement = $"""
                        insert into ModelsLod (
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
                        ) values (
                            {cdbParamName},
                            {datasetParamName},
                            {cs1ParamName},
                            {cs2ParamName},
                            {lodParamName},
                            {kindParamName},
                            {domainParamName},
                            {countryParamName},
                            {categoryParamName},
                            {subcategoryParamName},
                            {specificParamName},
                            {extraParamName},
                            {fileTypeParamName},
                            {contentParamName}
                        )
                        """;

                insertIntoModelsLodCommand.CommandText = insertIntoModelsLodStatement;
                CreateAndAttachParameter(insertIntoModelsLodCommand, cdbParamName, DbType.String);
                CreateAndAttachParameter(insertIntoModelsLodCommand, datasetParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsLodCommand, cs1ParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsLodCommand, cs2ParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsLodCommand, lodParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsLodCommand, kindParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsLodCommand, domainParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsLodCommand, countryParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsLodCommand, categoryParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsLodCommand, subcategoryParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsLodCommand, specificParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsLodCommand, extraParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoModelsLodCommand, fileTypeParamName, DbType.String);
                CreateAndAttachParameter(insertIntoModelsLodCommand, contentParamName, DbType.String);
                insertIntoModelsLodCommand.Prepare();
                insertIntoModelsLodCommand.Parameters[cdbParamName].Value = cdbName;
            }

            using DbCommand insertIntoTilesCommand = dbConnection.CreateCommand();
            {
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
                                {cdbParamName},
                                {latitudeParamName},
                                {longitudeParamName},
                                {datasetParamName},
                                {cs1ParamName},
                                {cs2ParamName},
                                {lodParamName},
                                {upParamName},
                                {rightParamName},
                                {fileTypeParamName},
                                {contentParamName}
                            )
                            """;

                insertIntoTilesCommand.CommandText = insertIntoTilesStatement;
                CreateAndAttachParameter(insertIntoTilesCommand, cdbParamName, DbType.String);
                CreateAndAttachParameter(insertIntoTilesCommand, latitudeParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoTilesCommand, longitudeParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoTilesCommand, datasetParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoTilesCommand, cs1ParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoTilesCommand, cs2ParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoTilesCommand, lodParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoTilesCommand, upParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoTilesCommand, rightParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoTilesCommand, fileTypeParamName, DbType.String);
                CreateAndAttachParameter(insertIntoTilesCommand, contentParamName, DbType.Binary);
                insertIntoTilesCommand.Prepare();
                insertIntoTilesCommand.Parameters[cdbParamName].Value = cdbName;
            }
            using DbCommand insertIntoNavigationCommand = dbConnection.CreateCommand();
            {
                const string insertIntoNavigationStatement = $"""
                            insert into navigation (
                                cdb,
                                dataset,
                                component_selector_1,
                                component_selector_2,
                                file_type,
                                content
                            ) values (
                                {cdbParamName},
                                {datasetParamName},
                                {cs1ParamName},
                                {cs2ParamName},
                                {fileTypeParamName},
                                {contentParamName}
                            )
                            """;

                insertIntoNavigationCommand.CommandText = insertIntoNavigationStatement;
                CreateAndAttachParameter(insertIntoNavigationCommand, cdbParamName, DbType.String);
                CreateAndAttachParameter(insertIntoNavigationCommand, datasetParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoNavigationCommand, cs1ParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoNavigationCommand, cs2ParamName, DbType.Int32);
                CreateAndAttachParameter(insertIntoNavigationCommand, fileTypeParamName, DbType.String);
                CreateAndAttachParameter(insertIntoNavigationCommand, contentParamName, DbType.Binary);
                insertIntoNavigationCommand.Prepare();
                insertIntoNavigationCommand.Parameters[cdbParamName].Value = cdbName;
            }

            DirectoryInfo cdbRoot = new(cdbName);
            // Metadata
            {
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
                {
                    insertIntoMetadataCommand.CommandText = insertIntoMetadataStatement;
                    CreateAndAttachParameter(insertIntoMetadataCommand, cdbParamName, DbType.String);
                    CreateAndAttachParameter(insertIntoMetadataCommand, nameParamName, DbType.String);
                    CreateAndAttachParameter(insertIntoMetadataCommand, fileTypeParamName, DbType.String);
                    CreateAndAttachParameter(insertIntoMetadataCommand, contentParamName, DbType.Binary);
                    insertIntoMetadataCommand.Prepare();
                    insertIntoMetadataCommand.Parameters[cdbParamName].Value = cdbName;
                }

                metadataVisitor.VisitMetadata(cdbRoot, (name, ext, file) =>
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
                GTModelVisitor gtModelVisitor = host.Services.GetRequiredService<GTModelVisitor>();

                gtModelVisitor.VisitGeotypicalModels(cdbRoot,
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
                        insertIntoGeometryLodCommand.Parameters[lodParamName].Value = geometryLod.LevelOfDetail.Value;
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
                        insertIntoTextureCommand.Parameters[textureNameParamName].Value = texture.Name;
                        insertIntoTextureCommand.Parameters[fileTypeParamName].Value = texture.FileType;
                        insertIntoTextureCommand.Parameters[contentParamName].Value = File.ReadAllBytes(file.FullName);

                        int rowsAffected = insertIntoTextureCommand.ExecuteNonQuery();
                    },
                    (textureLod, file) =>
                    {
                        insertIntoTextureLodCommand.Parameters[datasetParamName].Value = textureLod.Dataset.Value;
                        insertIntoTextureLodCommand.Parameters[cs1ParamName].Value = textureLod.ComponentSelector1;
                        insertIntoTextureLodCommand.Parameters[cs2ParamName].Value = textureLod.ComponentSelector2;
                        insertIntoTextureLodCommand.Parameters[lodParamName].Value = textureLod.LevelOfDetail.Value;
                        insertIntoTextureLodCommand.Parameters[textureNameParamName].Value = textureLod.TextureName;
                        insertIntoTextureLodCommand.Parameters[fileTypeParamName].Value = textureLod.FileType;
                        insertIntoTextureLodCommand.Parameters[contentParamName].Value = File.ReadAllBytes(file.FullName);

                        int rowsAffected = insertIntoTextureLodCommand.ExecuteNonQuery();
                    });
            }
            // MModel
            {
                MovingModelVisitor movingModelVisitor = host.Services.GetRequiredService<MovingModelVisitor>();

                movingModelVisitor.VisitMovingModels(cdbRoot,
                    (movingModelGeometry, file) =>
                    {
                        insertIntoModelsCommand.Parameters[datasetParamName].Value = movingModelGeometry.Dataset.Value;
                        insertIntoModelsCommand.Parameters[cs1ParamName].Value = movingModelGeometry.ComponentSelector1;
                        insertIntoModelsCommand.Parameters[cs2ParamName].Value = movingModelGeometry.ComponentSelector2;
                        insertIntoModelsCommand.Parameters[kindParamName].Value = movingModelGeometry.MMDC.Kind;
                        insertIntoModelsCommand.Parameters[domainParamName].Value = movingModelGeometry.MMDC.Domain;
                        insertIntoModelsCommand.Parameters[countryParamName].Value = movingModelGeometry.MMDC.Country;
                        insertIntoModelsCommand.Parameters[categoryParamName].Value = movingModelGeometry.MMDC.Category;
                        insertIntoModelsCommand.Parameters[subcategoryParamName].Value = movingModelGeometry.MMDC.Subcategory;
                        insertIntoModelsCommand.Parameters[specificParamName].Value = movingModelGeometry.MMDC.Specific;
                        insertIntoModelsCommand.Parameters[extraParamName].Value = movingModelGeometry.MMDC.Extra;
                        insertIntoModelsCommand.Parameters[fileTypeParamName].Value = movingModelGeometry.FileType;
                        insertIntoModelsCommand.Parameters[contentParamName].Value = File.ReadAllBytes(file.FullName);

                        int rowsAffected = insertIntoModelsCommand.ExecuteNonQuery();
                    },
                    (movingModelGeometryLod, file) =>
                    {
                        insertIntoModelsLodCommand.Parameters[datasetParamName].Value = movingModelGeometryLod.Dataset.Value;
                        insertIntoModelsLodCommand.Parameters[cs1ParamName].Value = movingModelGeometryLod.ComponentSelector1;
                        insertIntoModelsLodCommand.Parameters[cs2ParamName].Value = movingModelGeometryLod.ComponentSelector2;
                        insertIntoModelsLodCommand.Parameters[lodParamName].Value = movingModelGeometryLod.LevelOfDetail.Value;
                        insertIntoModelsLodCommand.Parameters[kindParamName].Value = movingModelGeometryLod.MMDC.Kind;
                        insertIntoModelsLodCommand.Parameters[domainParamName].Value = movingModelGeometryLod.MMDC.Domain;
                        insertIntoModelsLodCommand.Parameters[countryParamName].Value = movingModelGeometryLod.MMDC.Country;
                        insertIntoModelsLodCommand.Parameters[categoryParamName].Value = movingModelGeometryLod.MMDC.Category;
                        insertIntoModelsLodCommand.Parameters[subcategoryParamName].Value = movingModelGeometryLod.MMDC.Subcategory;
                        insertIntoModelsLodCommand.Parameters[specificParamName].Value = movingModelGeometryLod.MMDC.Specific;
                        insertIntoModelsLodCommand.Parameters[extraParamName].Value = movingModelGeometryLod.MMDC.Extra;
                        insertIntoModelsLodCommand.Parameters[fileTypeParamName].Value = movingModelGeometryLod.FileType;
                        insertIntoModelsLodCommand.Parameters[contentParamName].Value = File.ReadAllBytes(file.FullName);

                        int rowsAffected = insertIntoModelsLodCommand.ExecuteNonQuery();
                    },
                    (modelTexture, file) =>
                    {
                        insertIntoTextureSizedCommand.Parameters[datasetParamName].Value = modelTexture.Dataset.Value;
                        insertIntoTextureSizedCommand.Parameters[cs1ParamName].Value = modelTexture.ComponentSelector1;
                        insertIntoTextureSizedCommand.Parameters[cs2ParamName].Value = modelTexture.ComponentSelector2;
                        insertIntoTextureSizedCommand.Parameters[sizeParamName].Value = modelTexture.TextureSize;
                        insertIntoTextureSizedCommand.Parameters[textureNameParamName].Value = modelTexture.TextureName;
                        insertIntoTextureSizedCommand.Parameters[fileTypeParamName].Value = modelTexture.FileType;
                        insertIntoTextureSizedCommand.Parameters[contentParamName].Value = File.ReadAllBytes(file.FullName);

                        int rowsAffected = insertIntoTextureSizedCommand.ExecuteNonQuery();
                    },
                    (texture, file) =>
                    {
                        insertIntoTextureCommand.Parameters[datasetParamName].Value = texture.Dataset.Value;
                        insertIntoTextureCommand.Parameters[cs1ParamName].Value = texture.ComponentSelector1;
                        insertIntoTextureCommand.Parameters[cs2ParamName].Value = texture.ComponentSelector2;
                        insertIntoTextureCommand.Parameters[textureNameParamName].Value = texture.Name;
                        insertIntoTextureCommand.Parameters[fileTypeParamName].Value = texture.FileType;
                        insertIntoTextureCommand.Parameters[contentParamName].Value = File.ReadAllBytes(file.FullName);

                        int rowsAffected = insertIntoTextureCommand.ExecuteNonQuery();
                    });
            }
            // Tiles
            {
                TiledDatasetVisitor tiledDatasetVisitor = host.Services.GetRequiredService<TiledDatasetVisitor>();

                tiledDatasetVisitor.VisitTiles(cdbRoot, (tile, file) =>
                {
                    insertIntoTilesCommand.Parameters[latitudeParamName].Value = tile.LatitudeValue.Value;
                    insertIntoTilesCommand.Parameters[longitudeParamName].Value = tile.LongitudeValue.Value;
                    insertIntoTilesCommand.Parameters[datasetParamName].Value = tile.DatasetValue.Value;
                    insertIntoTilesCommand.Parameters[cs1ParamName].Value = tile.ComponentSelector1;
                    insertIntoTilesCommand.Parameters[cs2ParamName].Value = tile.ComponentSelector2;
                    insertIntoTilesCommand.Parameters[lodParamName].Value = tile.Level.Value;
                    insertIntoTilesCommand.Parameters[upParamName].Value = tile.Up;
                    insertIntoTilesCommand.Parameters[rightParamName].Value = tile.Right;
                    insertIntoTilesCommand.Parameters[fileTypeParamName].Value = tile.FileType;
                    insertIntoTilesCommand.Parameters[contentParamName].Value = File.ReadAllBytes(file.FullName);

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
            // Navigation
            {
                NavigationVisitor navigationVisitor = host.Services.GetRequiredService<NavigationVisitor>();
                navigationVisitor.VisitNavigationDatasets(cdbRoot, (navigation, file) =>
                {
                    insertIntoNavigationCommand.Parameters[datasetParamName].Value = navigation.Dataset.Value;
                    insertIntoNavigationCommand.Parameters[cs1ParamName].Value = navigation.ComponentSelector1;
                    insertIntoNavigationCommand.Parameters[cs2ParamName].Value = navigation.ComponentSelector2;
                    insertIntoNavigationCommand.Parameters[fileTypeParamName].Value = navigation.FileType;
                    insertIntoNavigationCommand.Parameters[contentParamName].Value = File.ReadAllBytes(file.FullName);

                    int rowsAffected = insertIntoNavigationCommand.ExecuteNonQuery();
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

            // Need an index on texture name.
            dbCommand.CommandText = """
create table TextureSized (
    cdb text not null references CDB(name),
    dataset integer not null,
    component_selector_1 integer not null,
    component_selector_2 integer not null,
    size integer not null,
    texture_name text not null,
    file_type text not null,
    content blob not null,
    primary key(
        cdb,
        dataset,
        component_selector_1,
        component_selector_2,
        size,
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

            // Maybe an index on kind, domain, country, category.
            // Need an index on kind, domain, country, category, subcategory, specific, extra.
            dbCommand.CommandText = """
create table ModelsLod (
    cdb text not null references CDB(name),
    dataset integer not null,
    component_selector_1 integer not null,
    component_selector_2 integer not null,
    lod integer not null,
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
        lod,
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

            dbCommand.CommandText = """
create table Navigation (
    cdb text not null references CDB(name),
    dataset integer not null,
    component_selector_1 integer not null,
    component_selector_2 integer not null,
    file_type text not null,
    content blob not null,
    primary key(
        cdb,
        dataset,
        component_selector_1,
        component_selector_2,
        file_type
    )
)
""";
            rowsAffected = dbCommand.ExecuteNonQuery();

            dbTransaction.Commit();
        }
    }
}
