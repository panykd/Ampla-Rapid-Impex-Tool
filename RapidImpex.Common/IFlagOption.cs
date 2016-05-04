namespace RapidImpex.Common
{
    public interface IFlagOption<in TConfig>
    {
        void SetFlag(TConfig config);
    }
}