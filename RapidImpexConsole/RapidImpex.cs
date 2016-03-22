using System;
using System.Collections.Generic;
using Autofac.Features.Indexed;
using Fclp;
using RapidImpex.Functionality;
using RapidImpex.Models;
using Serilog;

namespace RapidImpexConsole
{
    public class RapidImpex
    {
        public ILogger Logger { get; set; }

        private readonly IIndex<string, Func<RapidImpexConfiguration, IRapidImpexFunctionality>> _functionalityFactory;

        private static readonly RapidImpexConfiguration Config = new RapidImpexConfiguration();

        public RapidImpex(IIndex<string, Func<RapidImpexConfiguration, IRapidImpexFunctionality>> functionalityFactory)
        {
            _functionalityFactory = functionalityFactory;
        }

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

            Func<RapidImpexConfiguration, IRapidImpexFunctionality> funcFac;

            if (Config.IsImport)
            {
                Logger.Information("Loading 'Import' Functionality");

                funcFac = _functionalityFactory["import"];
            }
            else
            {
                Logger.Information("Loading 'Export' Functionality");

                funcFac = _functionalityFactory["export"];
            }

            var functionality = funcFac(Config);

            Logger.Debug("Initializing");
            functionality.Initialize(Config);

            Logger.Debug("Executing");
            functionality.Execute();
        }

        static FluentCommandLineParser BootstrapConfigurationParser(string[] args)
        {
            var parser = new FluentCommandLineParser();

            // Transport Options

            parser.Setup<bool>("useHttp")
                .Callback(v => Config.UseBasicHttp = v)
                .SetDefault(false)
                .WithDescription("Use Basic HTTP instead of TCP");

            parser.Setup<string>('u', "user")
                .Callback(v => Config.Username = v)
                .WithDescription("Simple Security Username");

            parser.Setup<string>('p', "password")
                .Callback(v => Config.Password = v)
                .WithDescription("Simple Security Password");

            parser.Setup<bool>("sa")
                .Callback(v => Config.UseSimpleAuthentication = v)
                .SetDefault(false)
                .WithDescription("Use Simple Authentication instead of Integrated Windows Authentication");

            // Other Settings
            parser.Setup<bool>("import")
                .Callback(v => Config.IsImport = v)
                .SetDefault(false)
                .WithDescription("Set to use tool to import rather than export");

            parser.Setup<string>('o', "path")
                .Callback(v => Config.WorkingDirectory = v)
                .SetDefault(Environment.CurrentDirectory)
                .WithDescription("The path to export / import files to/from");

            parser.Setup<List<string>>('m')
                .Callback(v => Config.Modules = v.ToArray())
                .Required()
                .WithDescription("The ampla modules to export from the project");

            // Time Properties

            parser.Setup<string>("start")
                .Callback(v =>
                {
                    var dateTime = Convert.ToDateTime(v);
                    Config.StartTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
                })
                .WithDescription("LOCAL Start Time to export data from. Only used during Export.");

            parser.Setup<string>("utcStart")
                .Callback(v =>
                {
                    var dateTime = Convert.ToDateTime(v);
                    Config.StartTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                })
                .WithDescription("UTC Start Time to export data from. Only used during Export.");

            parser.Setup<string>("end")
                .Callback(v =>
                {
                    var dateTime = Convert.ToDateTime(v);
                    Config.EndTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
                })
                .WithDescription("LOCAL End Time to export data to. Only used during Export.");

            parser.Setup<string>("utcEnd")
                .Callback(v =>
                {
                    var dateTime = Convert.ToDateTime(v);
                    Config.EndTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                })
                .WithDescription("UTC Start Time to export data from. Only used during Export.");

            // Setup help
            parser.SetupHelp("help");

            return parser;
        }
    }
}