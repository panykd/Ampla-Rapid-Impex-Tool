using System;
using Autofac.Features.Indexed;
using RapidImpex.Common;
using RapidImpex.Functionality;
using Serilog;

namespace RapidImpexConsole
{
    public class RapidImpex
    {
        public ILogger Logger { get; set; }

        private readonly IIndex<string, Func<IRapidImpexFunctionality>> _functionalityFactory;

        public RapidImpex(IIndex<string, Func<IRapidImpexFunctionality>> functionalityFactory)
        {
            _functionalityFactory = functionalityFactory;
        }

        public void Run(string[] args)
        {
            // Configure and Parse out only the required operation from the console arguments
            var parser = new CommandLineParser<RapidImpexConfiguration>();
            parser.AddKeyValueOption("operation", new KeyValueOption<RapidImpexConfiguration,string>(x => x.Functionality));

            RapidImpexConfiguration config;
            if (!parser.Parse(args, out config))
            {
                Logger.Error("Enable to parse configuration");
                return;
            }

            // Get the Appropriate Fucntionality
            Logger.Information("Loading '{0}' Functionality", config.Functionality);
            var funcFac = _functionalityFactory[config.Functionality.ToLowerInvariant()];

            var functionality = funcFac();

            Logger.Debug("Initializing");
            functionality.Initialize(args);

            Logger.Debug("Executing");
            functionality.Execute();
        }
    }

    public class RapidImpexConfiguration
    {
        public string Functionality { get; set; }
    }
}