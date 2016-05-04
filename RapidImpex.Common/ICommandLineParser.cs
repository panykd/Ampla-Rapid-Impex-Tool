namespace RapidImpex.Common
{
    public interface ICommandLineParser<TConfig>
        where TConfig : new()
    {
        bool Parse(string[] args, out TConfig config);
    }
}