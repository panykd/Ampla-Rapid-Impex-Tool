using System;
using System.Linq.Expressions;
using System.Reflection;

namespace RapidImpex.Common
{
    public class FlagOption<TConfig> : IFlagOption<TConfig>
    {
        private readonly Expression<Func<TConfig, bool>> _expression;

        public FlagOption(Expression<Func<TConfig, bool>> expression)
        {
            _expression = expression;
        }

        public void SetFlag(TConfig config)
        {
            var property = (PropertyInfo)((MemberExpression)_expression.Body).Member;
            property.SetValue(config, true, null);
        }
    }
}