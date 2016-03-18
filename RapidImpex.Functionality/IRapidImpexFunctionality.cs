using RapidImpex.Models;

namespace RapidImpex.Functionality
{
    public interface IRapidImpexFunctionality
    {
        void Initialize(RapidImpexConfiguration configuration);

        void Execute();
    }

    public abstract class RapidImpexImportFunctionalityBase : IRapidImpexFunctionality
    {
        protected RapidImpexConfiguration Config;

        public virtual void Initialize(RapidImpexConfiguration configuration)
        {
            Config = configuration;
        }

        public abstract void Execute();
    }
}