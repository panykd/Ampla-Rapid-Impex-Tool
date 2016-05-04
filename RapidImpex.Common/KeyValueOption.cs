using System;
using System.Linq.Expressions;
using System.Reflection;

namespace RapidImpex.Common
{
    public class KeyValueOption<TConfig, TProperty> : IKeyValueOption<TConfig>
    {
        private readonly PropertyInfo _propertyInfo;

        public KeyValueOption(Expression<Func<TConfig, TProperty>> expression)
        {
            _propertyInfo = (PropertyInfo)((MemberExpression)expression.Body).Member;
        }

        public TProperty DefaultValue { get; set; }

        public Func<string, TProperty> Mapper { get; set; }

        public void SetDefaultValue(TConfig config)
        {
            SetPropertyValue(config, DefaultValue);
        }

        public void SetValue(TConfig config, string value)
        {
            var propertyValue = Mapper == null ? (TProperty)Convert.ChangeType(value, typeof(TProperty)) : Mapper(value);

            SetPropertyValue(config, propertyValue);
        }

        public string PropertyName { get { return _propertyInfo.Name; } }

        private void SetPropertyValue(TConfig config, TProperty value)
        {
            _propertyInfo.SetValue(config, value, null);
        }
    }
}