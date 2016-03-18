using System;
using Autofac;
using RapidImpex.Ampla;
using RapidImpex.Data;
using RapidImpex.Functionality;

namespace RapidImpexConsole
{
    public static class Program
    {
        static IContainer BootstrapAutofac()
        {
            var builder = new ContainerBuilder();

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
            var container = BootstrapAutofac();

            var app = container.Resolve<RapidImpex>();

            app.Run(args);
        }
    }
}