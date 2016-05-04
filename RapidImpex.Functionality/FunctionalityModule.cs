using Autofac;

namespace RapidImpex.Functionality
{
    public class FunctionalityModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<RapidImpexFileImportFunctionality>()
                .Named<IRapidImpexFunctionality>("import")
                .SingleInstance();

            builder.RegisterType<RapidImpexFileExportFunctionality>()
                .Named<IRapidImpexFunctionality>("export")
                .SingleInstance();

            builder.RegisterType<RapidImpexMergeFunctionality>()
                .Named<IRapidImpexFunctionality>("merge")
                .SingleInstance();
        }
    }
}
