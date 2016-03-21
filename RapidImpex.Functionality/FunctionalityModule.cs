using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;

namespace RapidImpex.Functionality
{
    public class FunctionalityModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<RapidImpexImportFunctionality>()
                .Named<IRapidImpexFunctionality>("import")
                .SingleInstance();

            builder.RegisterType<RapidImpexExportFunctionality>()
                .Named<IRapidImpexFunctionality>("export")
                .SingleInstance();
        }
    }
}
