using Autofac;

namespace RapidImpexConsole
{
    public static class Program
    {
        static IContainer BootstrapAutofac()
        {
            var builder = new ContainerBuilder();

            // Load Modules



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