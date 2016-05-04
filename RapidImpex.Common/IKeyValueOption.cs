namespace RapidImpex.Common
{
    public interface IKeyValueOption<in TConfig>
    {
        void SetDefaultValue(TConfig config);

        void SetValue(TConfig config, string value);

        string PropertyName { get; }
    }
}