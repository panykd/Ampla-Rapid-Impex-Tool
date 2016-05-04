using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RapidImpex.Common
{
    public class CommandLineParser<TConfig> where TConfig : new()
    {
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
            catch (Exception)
            {
                return false;
            }
        }
    }
}
