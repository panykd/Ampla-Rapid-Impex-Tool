using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Autofac.Features.Indexed;
using Fclp;
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
            var parser = new MyCommandLineParser();

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
    }

    public class MyCommandLineParser
    {
        public ILogger Logger { get; set; }

        public bool Parse(string[] args, out RapidImpexConfiguration configuration)
        {
            var flagRegex = new Regex("--(?'flag'.+)", RegexOptions.Compiled);
            var argumentRegex = new Regex("-(?'arg'.+)=(?'open'\")(?'value'.+)(?'close-open'\")", RegexOptions.Compiled);

            configuration = new RapidImpexConfiguration();

            try
            {
                // Flags
                var flags = (from f in args
                    let m = flagRegex.Match(f)
                    where m.Success
                    select m.Groups["flag"].Value).ToArray();

                // Arguments
                var argValues = (from a in args
                    let m = argumentRegex.Match(a)
                    where m.Success
                    select new KeyValuePair<string, string>(m.Groups["arg"].Value, m.Groups["value"].Value))
                    .ToDictionary(k => k.Key, v => v.Value);


                configuration.UseBasicHttp = flags.Contains("useHttp");
                configuration.UseSimpleAuthentication = flags.Contains("simple");
                configuration.IsImport = flags.Contains("import");

                configuration.WorkingDirectory = argValues.ContainsKey("path")
                    ? argValues["path"]
                    : Environment.CurrentDirectory;
                configuration.Username = argValues.ContainsKey("user") ? argValues["user"] : null;
                configuration.Password = argValues.ContainsKey("password") ? argValues["password"] : null;
                ;

                //Extract Modules
                var allModules = Enum.GetNames(typeof (AmplaModules));
                var modules = flags.Where(flag => allModules.Any(x => x == flag)).ToList();

                configuration.Modules = modules.ToArray();

                // Set Start Time
                if (argValues.ContainsKey("start"))
                {
                    var value = argValues["start"];
                    configuration.StartTime = DateTime.SpecifyKind(DateTime.Parse(value, CultureInfo.InvariantCulture),
                        DateTimeKind.Local);
                }
                else if (argValues.ContainsKey("startUtc"))
                {
                    var value = argValues["startUtc"];
                    configuration.StartTime = DateTime.SpecifyKind(DateTime.Parse(value, CultureInfo.InvariantCulture),
                        DateTimeKind.Utc);
                }

                if (argValues.ContainsKey("end"))
                {
                    var value = argValues["end"];
                    configuration.EndTime = DateTime.SpecifyKind(DateTime.Parse(value, CultureInfo.InvariantCulture),
                        DateTimeKind.Local);
                }
                else if (argValues.ContainsKey("endUtc"))
                {
                    var value = argValues["endUtc"];
                    configuration.EndTime = DateTime.SpecifyKind(DateTime.Parse(value, CultureInfo.InvariantCulture),
                        DateTimeKind.Utc);
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, "An error has parsing command line arguments");
                return false;
            }
        }
    }
}