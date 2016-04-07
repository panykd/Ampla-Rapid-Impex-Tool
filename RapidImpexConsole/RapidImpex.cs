using System;
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
}