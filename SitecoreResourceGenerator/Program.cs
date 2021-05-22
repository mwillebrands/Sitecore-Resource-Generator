using System;
using System.IO;
using System.Linq;
using CommandLine;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf;
using Sitecore.Data.DataProviders.ReadOnly.Protobuf.Data;
using Sitecore.DevEx.Configuration;
using Sitecore.DevEx.Serialization.Client.Datasources.Filesystem.Configuration;
using Sitecore.DevEx.Serialization.Client.Datasources.Filesystem.Formatting.Yaml;
using Sitecore.DevEx.Serialization.Client.Datasources.Package;

namespace SitecoreResourceGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed<CommandLineOptions>(o =>
                {
                    if (!File.Exists(o.FilePath))
                    {
                        Console.Error.Write($"The package could not be found ({o.FilePath})");
                    }

                    GenerateProtobuf(o.FilePath, o.Database, o.OutputPath);
                });
        }

        static void GenerateProtobuf(string inputPath, string database, string outputPath)
        {
            ItemPackage package = new ItemPackage(new FileInfo(inputPath),
                PackageMode.Install,
                new FilesystemConfigurationManager(new NullLoggerFactory()),
                new FilesystemSerializationModuleConfigurationManager(new NullLoggerFactory()),
                new YamlSerializationFormatter(),
                new NullLoggerFactory());
            package.ReadModuleConfigurations();

            var serializationContainer = new ItemsData();
            foreach (var itemMetaData in package.ReadItemMetadatas(database).Result)
            {
                var item = package.ReadItemData(database, itemMetaData.Path).Result;
                serializationContainer.Definitions.Add(item.Id, new ItemRecord()
                {
                    ID = item.Id,
                    MasterID = item.BranchId,
                    Name = item.Name,
                    ParentID = item.ParentId,
                    TemplateID = item.TemplateId
                });

                if (item.SharedFields.Any())
                {
                    serializationContainer.SharedData.Add(item.Id,
                        item.SharedFields.ToDictionary(x => x.FieldId, x => x.Value));
                }

                var languageData = new ItemLanguagesData();
                foreach (var languageVersions in item.Versions.GroupBy(x => x.Language))
                {
                    var versionData = new VersionsData();
                    foreach (var version in languageVersions)
                    {
                        var fieldsData = new FieldsData();
                        foreach (var versionFields in version.Fields)
                        {
                            fieldsData.Add(versionFields.FieldId, versionFields.Value);
                        }
                        versionData.Add(version.VersionNumber, fieldsData);
                    }
                    languageData.Add(languageVersions.Key, versionData);
                }

                foreach (var languageVersion in item.UnversionedFields)
                {
                    var fieldsData = new FieldsData();
                    foreach (var versionFields in languageVersion.Fields)
                    {
                        fieldsData.Add(versionFields.FieldId, versionFields.Value);
                    }

                    if (!languageData.ContainsKey(languageVersion.Language))
                    {
                        var versionData = new VersionsData { { 0, fieldsData } };
                        languageData.Add(languageVersion.Language, versionData);
                    }
                    else
                    {
                        languageData[languageVersion.Language].Add(0, fieldsData);
                    }
                }
                serializationContainer.LanguageData.Add(item.Id, languageData);
            }


            var outputFilePath = $"{outputPath}.dat";
            using (var file = File.OpenWrite(outputFilePath))
            {
                Serializer.Serialize(file, serializationContainer);
            }
            Console.WriteLine($"All done, the resource file can be found at: {outputFilePath}");
        }
    }
}
