using System;
using System.Globalization;
using Autofac.Features.Indexed;
using RapidImpex.Ampla;
using RapidImpex.Ampla.AmplaData200806;
using RapidImpex.Functionality;
using RapidImpex.Models;
using Serilog;

namespace RapidImpexConsole
{
    public class RapidImpex
    {
        public ILogger Logger { get; set; }

        private readonly IIndex<string, Func<RapidImpexConfiguration, IRapidImpexFunctionality>> _functionalityFactory;

        public RapidImpex(IIndex<string, Func<RapidImpexConfiguration, IRapidImpexFunctionality>> functionalityFactory)
        {
            _functionalityFactory = functionalityFactory;
        }

        public void Run(string[] args)
        {
            var parser = CreateParser();

            RapidImpexConfiguration config;
            var result = parser.Parse(args, out config);


            Func<RapidImpexConfiguration, IRapidImpexFunctionality> funcFac;

            if (config.IsImport)
            {
                Logger.Information("Loading 'Import' Functionality");
                
                funcFac = _functionalityFactory["import"];
            }
            else
            {
                Logger.Information("Loading 'Export' Functionality");

                funcFac = _functionalityFactory["export"];
            }

            var functionality = funcFac(config);

            Logger.Debug("Initializing");
            functionality.Initialize(config);

            Logger.Debug("Executing");
            functionality.Execute();
        }

        public static MyCommandLineParser CreateParser()
        {
            var parser = new MyCommandLineParser();

            parser.AddFlagOption("useHttp", new FlagOption<RapidImpexConfiguration>(x => x.UseBasicHttp));
            parser.AddFlagOption("simple", new FlagOption<RapidImpexConfiguration>(x => x.UseSimpleAuthentication));
            parser.AddFlagOption("import", new FlagOption<RapidImpexConfiguration>(x => x.IsImport));

            parser.AddKeyValueOption("path",
                new KeyValueOption<RapidImpexConfiguration, string>(x => x.WorkingDirectory)
                {
                    DefaultValue = Environment.CurrentDirectory
                });
            parser.AddKeyValueOption("file", new KeyValueOption<RapidImpexConfiguration, string>(x => x.File));
            parser.AddKeyValueOption("user", new KeyValueOption<RapidImpexConfiguration, string>(x => x.Username));
            parser.AddKeyValueOption("password", new KeyValueOption<RapidImpexConfiguration, string>(x => x.Password));
            parser.AddKeyValueOption("location", new KeyValueOption<RapidImpexConfiguration, string>(x => x.Location));
            parser.AddKeyValueOption("module", new KeyValueOption<RapidImpexConfiguration, string>(x => x.Module));

            Func<string, DateTime> localMap =
                (x) => DateTime.SpecifyKind(DateTime.Parse(x, CultureInfo.InvariantCulture), DateTimeKind.Local);
            Func<string, DateTime> utcMap =
                (x) => DateTime.SpecifyKind(DateTime.Parse(x, CultureInfo.InvariantCulture), DateTimeKind.Utc);

            parser.AddKeyValueOption("start",
                new KeyValueOption<RapidImpexConfiguration, DateTime>(x => x.StartTime) { Mapper = localMap });
            parser.AddKeyValueOption("startUtc",
                new KeyValueOption<RapidImpexConfiguration, DateTime>(x => x.StartTime) { Mapper = utcMap });
            parser.AddKeyValueOption("end",
                new KeyValueOption<RapidImpexConfiguration, DateTime>(x => x.EndTime) { Mapper = localMap });
            parser.AddKeyValueOption("endUtc",
                new KeyValueOption<RapidImpexConfiguration, DateTime>(x => x.EndTime) { Mapper = utcMap });

            return parser;
        }
    }
}