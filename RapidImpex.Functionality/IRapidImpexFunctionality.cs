using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using RapidImpex.Models;
using Serilog;

namespace RapidImpex.Functionality
{
    public interface IRapidImpexFunctionality
    {
        void Initialize(string[] args);

        void Execute();
    }

    public abstract class RapidImpexFunctionalityBase : IRapidImpexFunctionality
    {
        protected RapidImpexImportExportConfiguration Config;

        public void Initialize(string[] args)
        {
            var parser = new RapidImpexImportExportCommandLineParser();

            parser.Parse(args, out Config);
        }

        public abstract void Execute();
    }

    public class RapidImpexImportExportCommandLineParser
    {
        public ILogger Logger { get; set; }

        public bool Parse(string[] args, out RapidImpexImportExportConfiguration importExportConfiguration)
        {
            var flagRegex = new Regex("--(?'flag'.+)", RegexOptions.Compiled);
            var argumentRegex = new Regex("-(?'arg'.+)=(?'value'.+)", RegexOptions.Compiled);

            importExportConfiguration = new RapidImpexImportExportConfiguration();

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


                importExportConfiguration.UseBasicHttp = flags.Contains("useHttp");
                importExportConfiguration.UseSimpleAuthentication = flags.Contains("simple");
                importExportConfiguration.IsImport = flags.Contains("import");

                importExportConfiguration.WorkingDirectory = argValues.ContainsKey("path") ? argValues["path"] : Environment.CurrentDirectory;
                importExportConfiguration.Username = argValues.ContainsKey("user") ? argValues["user"] : null;
                importExportConfiguration.Password = argValues.ContainsKey("password") ? argValues["password"] : null;

                importExportConfiguration.File = argValues.ContainsKey("file") ? argValues["file"] : null;
                importExportConfiguration.Location = argValues.ContainsKey("location") ? argValues["location"] : null;
                importExportConfiguration.Module = argValues.ContainsKey("module") ? argValues["module"] : null;

                //Prasanta :: Added to read the batchrecord value
                if (argValues.ContainsKey("batchRecord"))
                {
                    int batchRecord = 0;
                    if (int.TryParse(Convert.ToString(argValues["batchRecord"]), out batchRecord))
                    {
                        importExportConfiguration.BatchRecord = batchRecord;
                    }
                    else
                    {
                        importExportConfiguration.BatchRecord = int.MaxValue;
                    }
                }
                else
                {
                    importExportConfiguration.BatchRecord = 0;
                }

                // Set Start Time
                if (argValues.ContainsKey("start"))
                {
                    var value = argValues["start"];
                    importExportConfiguration.StartTime = DateTime.SpecifyKind(DateTime.Parse(value, CultureInfo.InvariantCulture),
                        DateTimeKind.Local);
                }
                else if (argValues.ContainsKey("startUtc"))
                {
                    var value = argValues["startUtc"];
                    importExportConfiguration.StartTime = DateTime.SpecifyKind(DateTime.Parse(value, CultureInfo.InvariantCulture),
                        DateTimeKind.Utc);
                }

                if (argValues.ContainsKey("end"))
                {
                    var value = argValues["end"];
                    importExportConfiguration.EndTime = DateTime.SpecifyKind(DateTime.Parse(value, CultureInfo.InvariantCulture),
                        DateTimeKind.Local);
                }
                else if (argValues.ContainsKey("endUtc"))
                {
                    var value = argValues["endUtc"];
                    importExportConfiguration.EndTime = DateTime.SpecifyKind(DateTime.Parse(value, CultureInfo.InvariantCulture),
                        DateTimeKind.Utc);
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