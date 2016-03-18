using System;
using System.Collections.Generic;
using Autofac.Features.Indexed;
using Fclp;
using RapidImpex.Functionality;
using RapidImpex.Models;

namespace RapidImpexConsole
{
    public class RapidImpex
    {
        private readonly IIndex<string, IRapidImpexFunctionality> _functionalityFactory;

        public RapidImpex(IIndex<string, IRapidImpexFunctionality> functionalityFactory)
        {
            _functionalityFactory = functionalityFactory;
        }

        private static readonly RapidImpexConfiguration Config = new RapidImpexConfiguration();

        public void Run(string[] args)
        {
            var parser = BootstrapConfigurationParser(args);

            var result = parser.Parse(args);

            if (result.HelpCalled || result.EmptyArgs)
            {
                Console.WriteLine();
                Console.WriteLine("'Rapid Impex' is a lightweight, generic, and fast Console-based Ampla data importer and exporter.");
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine(parser.OptionFormatter.Format(parser.Options));
                return;
            }

            if (result.HasErrors)
            {
                Console.WriteLine(result.ErrorText);
                return;
            }

            IRapidImpexFunctionality functionality;

            if (Config.IsImport)
            {
                functionality = _functionalityFactory["import"];
            }
            else
            {
                functionality = _functionalityFactory["export"];
            }

            functionality.Initialize(Config);

            functionality.Execute();
        }

        static FluentCommandLineParser BootstrapConfigurationParser(string[] args)
        {
            var parser = new FluentCommandLineParser();

            parser.Setup<bool>('i', "import")
                .Callback(v => Config.IsImport = v)
                .SetDefault(false)
                .WithDescription("Set to use tool to import rather than export");

            parser.Setup<string>('p', "path")
                .Callback(v => Config.WorkingDirectory = v)
                .SetDefault(Environment.CurrentDirectory)
                .WithDescription("The path to export / import files to/from");

            parser.Setup<List<string>>('m', "modules")
                .Callback(v => Config.Modules = v.ToArray())
                .Required()
                .WithDescription("The ampla modules to export from the project");

            // Time properties

            parser.Setup<string>('s', "start")
                .Callback(v =>
                {
                    var dateTime = Convert.ToDateTime(v);
                    Config.StartTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
                })
                .WithDescription("LOCAL Start Time to export data from. Only used during Export.");

            parser.Setup<string>('S', "utcStart")
                .Callback(v =>
                {
                    var dateTime = Convert.ToDateTime(v);
                    Config.StartTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                })
                .WithDescription("UTC Start Time to export data from. Only used during Export.");

            parser.Setup<string>('e', "end")
                .Callback(v =>
                {
                    var dateTime = Convert.ToDateTime(v);
                    Config.EndTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
                })
                .WithDescription("LOCAL End Time to export data to. Only used during Export.");

            parser.Setup<string>('E', "utcEnd")
                .Callback(v =>
                {
                    var dateTime = Convert.ToDateTime(v);
                    Config.EndTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                })
                .WithDescription("UTC Start Time to export data from. Only used during Export.");

            parser.SetupHelp("help", "h", "/?");

            return parser;
        }
    }
}