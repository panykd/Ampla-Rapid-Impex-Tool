using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.ServiceModel.Configuration;
using System.Text.RegularExpressions;
using RapidImpex.Models;
using Serilog;

namespace RapidImpexConsole
{
    public interface IFlagOption<in TConfig>
    {
        void SetFlag(TConfig config);
    }

    class FlagOption<TConfig> : IFlagOption<TConfig>
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

    public interface IKeyValueOption<in TConfig>
    {
        void SetDefaultValue(TConfig config);

        void SetValue(TConfig config, string value);

        string PropertyName { get; }
    }

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
            var propertyValue = Mapper == null ? (TProperty) Convert.ChangeType(value, typeof (TProperty)) : Mapper(value);

            SetPropertyValue(config, propertyValue);
        }

        public string PropertyName { get { return _propertyInfo.Name; } }

        private void SetPropertyValue(TConfig config, TProperty value)
        {       
            _propertyInfo.SetValue(config, value, null);
        }
    }

    public class MyCommandLineParser<TConfig> where TConfig : new()
    {
        public ILogger Logger { get; set; }

        private readonly Dictionary<string, IFlagOption<TConfig>> _flagOptions =
            new Dictionary<string, IFlagOption<TConfig>>();

        private readonly Dictionary<string, IKeyValueOption<TConfig>> _keyValueOptions =
            new Dictionary<string, IKeyValueOption<TConfig>>();

        public void AddFlagOption(string flag, IFlagOption<TConfig> flagOption)
        {
            _flagOptions.Add(flag, flagOption);
        }

        public void AddKeyValueOption(string key, IKeyValueOption<TConfig> keyValueOption)
        {
            _keyValueOptions.Add(key, keyValueOption);
        }

        public bool Parse(string[] args, out TConfig configuration)
        {
            var flagRegex = new Regex("--(?'flag'.+)", RegexOptions.Compiled);
            var argumentRegex = new Regex("-(?'arg'.+)=(?'value'.+)", RegexOptions.Compiled);

            configuration = new TConfig();

            try
            {
                // Flags
                var flags = (from f in args
                    let m = flagRegex.Match(f)
                    where m.Success
                    select m.Groups["flag"].Value).ToArray();

                // Arguments
                var argValues = (from a in args
                    let m = argumentRegex.Match(a)
                    where m.Success
                    select new KeyValuePair<string, string>(m.Groups["arg"].Value, m.Groups["value"].Value))
                    .ToDictionary(k => k.Key, v => v.Value);

                foreach (var flagOption in _flagOptions)
                {
                    if (flags.Contains(flagOption.Key))
                    {
                        flagOption.Value.SetFlag(configuration);
                    }
                }

                var keyValueOptionsSet = new Dictionary<string, bool>();

                foreach (var keyValueOption in _keyValueOptions)
                {
                    var option = keyValueOption.Value;

                    var optionAlreadySet = keyValueOptionsSet.ContainsKey(option.PropertyName) &&
                                           keyValueOptionsSet[option.PropertyName];

                    if (argValues.ContainsKey(keyValueOption.Key))
                    {
                        if (optionAlreadySet)
                        {
                            throw new NotImplementedException();
                        }

                        option.SetValue(configuration, argValues[keyValueOption.Key]);
                        keyValueOptionsSet[option.PropertyName] = true;
                    }
                    else
                    {
                        if (optionAlreadySet)
                        {
                            continue;
                        }

                        option.SetDefaultValue(configuration);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, "An error has parsing command line arguments");
                return false;
            }
        }
    }
}