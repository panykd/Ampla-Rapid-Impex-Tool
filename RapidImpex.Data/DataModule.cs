using System.Text;
using System.Threading.Tasks;
using Autofac;
using RapidImpex.Ampla.AmplaData200806;

namespace RapidImpex.Data
{
    public class DataModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterDecorator<IReportingPointDataReadWriteStrategy>((c, inner) =>
                new ThreadLockedReportingPointDataReadWriteStrategyAdapter(inner),
                "xlsx");

            builder.RegisterType<XlsxReportingPointDataStrategy>()
                .Named<IReportingPointDataReadWriteStrategy>("xlsx")
                .SingleInstance();

            builder.RegisterType<ByAssetXlsxMultiPartNamingStrategy>()
                .As<IMultiPartFileNamingStrategy>()
                .SingleInstance();
        }
    }
}
