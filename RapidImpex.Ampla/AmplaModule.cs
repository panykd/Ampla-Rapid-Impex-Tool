using Autofac;
using RapidImpex.Ampla.AmplaData200806;

namespace RapidImpex.Ampla
{
    public class AmplaModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DataWebServiceFactory>()
                .AsSelf()
                .SingleInstance();

            builder.Register(c => new DataWebServiceClient("NetTcp"))
                .Named<IDataWebService>("Tcp")
                .SingleInstance();

            builder.Register(c => new DataWebServiceClient("BasicHttp"))
                .Named<IDataWebService>("BasicHttp")
                .SingleInstance();

            builder.RegisterType<AmplaQueryService>()
                .AsSelf();
        }
    }
}