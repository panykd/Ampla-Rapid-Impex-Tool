using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using AutofacSerilogIntegration;
using Moq;
using RapidImpex.Ampla;
using RapidImpex.Data;
using Serilog;

namespace BMA.QuikMerge
{
    class Program
    {
        static IContainer BootstrapAutofac()
        {
            var builder = new ContainerBuilder();

            // Register Logging
            builder.RegisterLogger(Log.Logger, true);

            // Load Modules
            builder.RegisterModule<DataModule>();

            // Need to break this dependancy
            builder.RegisterInstance(Mock.Of<IAmplaQueryService>()).As<IAmplaQueryService>();

            // Register Functionality
            builder.RegisterType<global::BMA.QuikMerge.QuikMerge>()
                .SingleInstance();

            return builder.Build();
        }

        static void Main(string[] args)
        {
            BootstrapLogger();

            var container = BootstrapAutofac();

            var app = container.Resolve<global::BMA.QuikMerge.QuikMerge>();

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
