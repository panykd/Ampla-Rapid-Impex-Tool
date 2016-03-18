using Autofac;
using RapidImpex.Ampla.AmplaData200806;

namespace RapidImpex.Ampla
{
    public class AmplaModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {

            builder.Register(c => new DataWebServiceClient("NetTcp"))
                .As<IDataWebService>()
                .SingleInstance();

            builder.RegisterType<AmplaQueryService>()
                .AsSelf();
        }
    }
}