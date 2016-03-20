using System;
using Autofac;
using AutofacSerilogIntegration;
using RapidImpex.Ampla;
using RapidImpex.Data;
using RapidImpex.Functionality;
using Serilog;

namespace RapidImpexConsole
{
    public static class Program
    {
        static IContainer BootstrapAutofac()
        {
            var builder = new ContainerBuilder();

            // Register Logging
            builder.RegisterLogger(Log.Logger, true);

            // Load Modules
            builder.RegisterModule<AmplaModule>();
            builder.RegisterModule<DataModule>();
            builder.RegisterModule<FunctionalityModule>();

            // Register Functionality
            builder.RegisterType<RapidImpex>()
                .SingleInstance();

            return builder.Build();
        }

        static void Main(string[] args)
        {
            BootstrapLogger();

            var container = BootstrapAutofac();

            var app = container.Resolve<RapidImpex>();

            app.Run(args);
        }

        private static void BootstrapLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .MinimumLevel.Debug()
                .CreateLogger();
        }
    }
}