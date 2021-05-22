using CommandLine;

namespace SitecoreResourceGenerator
{
    public class CommandLineOptions
    {
        [Option('f', "file", Required = true, HelpText = "The path to the .scitempackage")]
        public string FilePath { get; set; }

        [Option('d', "database", Required = true, HelpText = "The database for the items")]
        public string Database { get; set; }

        [Option('o', "output", Required = true, HelpText = "The path for the output file, the extension is automatically added")]
        public string OutputPath { get; set; }
    }
}
