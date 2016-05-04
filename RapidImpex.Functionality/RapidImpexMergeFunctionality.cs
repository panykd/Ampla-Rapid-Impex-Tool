using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RapidImpex.Common;
using RapidImpex.Data;
using RapidImpex.Models;
using Serilog;

namespace RapidImpex.Functionality
{
    public class RapidImpexMergeFunctionality : IRapidImpexFunctionality
    {
        private readonly IReportingPointDataReadWriteStrategy _readWriteStrategy;
        public ILogger Logger { get; set; }

        public RapidImpexMergeFunctionality(IReportingPointDataReadWriteStrategy readWriteStrategy)
        {
            _readWriteStrategy = readWriteStrategy;
        }

        private RapidImpexMergeConfiguration _configuration;

        public void Initialize(string[] args)
        {
            var parser = new RapidImpexMergeConfigurationParser();

            if (!parser.Parse(args, out _configuration))
            {
                Logger.Fatal("Unable to parse configuration: '{0}'", string.Join(" | ", args));
            }
            else
            {
                Logger.Debug("Configuration Loaded");
            }
        }

        public void Execute()
        {
            // Load FROM files
            Logger.Information("Loading FROM records in file '{0}' into memory...", _configuration.FromFile);

            var fromReportingPoints = _readWriteStrategy.ReadFromFile(_configuration.FromFile);
            var fromReportingPointsRecordCount = fromReportingPoints.Select(x => x.Value.Count()).Sum();

            Logger.Debug("Loaded '{0}' records from '{1}' Reporting Points", fromReportingPointsRecordCount, fromReportingPoints.Count());

            // Load TO files
            Logger.Information("Loading FROM records in file '{0}' into memory...", _configuration.FromFile);

            var toReportingPoints = _readWriteStrategy.ReadFromFile(_configuration.ToFile);
            var toReportingPointsRecordCount = toReportingPoints.Select(x => x.Value.Count()).Sum();

            Logger.Debug("Loaded '{0}' records from '{1}' Reporting Points", toReportingPointsRecordCount, toReportingPoints.Count());

            // Go through each of the FROM Reporting Points and merge
            foreach (var fromReportingPoint in fromReportingPoints)
            {
                Logger.Information("Merging Reporting Point '{0}'", fromReportingPoint.Key);

                var frp = fromReportingPoint;

                var trps = toReportingPoints.Where(x => x.Key.FullName == frp.Key.FullName).ToArray();

                if (!trps.Any())
                {
                    Logger.Error("Unable to find Reporting Point '{0}' in the TO record set. Skipping", frp.Key.FullName);
                    continue;
                }

                if (trps.Count() > 1)
                {
                    Logger.Error("Found multiple TO Reporting Points for '{0}'. Skipping", frp.Key.FullName);
                    continue;
                }


                var outerReportingPoint = trps.Single();
                var outerReportingPointRecords = outerReportingPoint.Value.ToArray();

                if (!outerReportingPointRecords.Any())
                {
                    Logger.Warning("No TO records to merge. Skipping");
                    continue;
                }

                var mergedRecords = new List<ReportingPointRecord>();

                foreach (var innerRecord in frp.Value)
                {
                    var mergeValue = innerRecord.Values[_configuration.MergeField];

                    var outerRecords = outerReportingPointRecords.Where(x => x.Values[_configuration.MergeField].Equals(mergeValue)).ToArray();

                    if (!outerRecords.Any())
                    {
                        Logger.Debug("No matching TO record for merge key '{0}'. Creating new record", mergeValue);

                        var newRecord = new ReportingPointRecord()
                        {
                            Id = 0,
                            IsConfirmed = innerRecord.IsConfirmed,
                            IsDeleted = innerRecord.IsDeleted,
                            Values = innerRecord.Values
                        };

                        mergedRecords.Add(newRecord);

                        continue;
                    }

                    if (outerRecords.Count() > 1)
                    {
                        Logger.Error("Multiple TO record for merge key '{0}'. Skipping");
                        continue;
                    }

                    var outerRecord = outerRecords.Single();

                    Logger.Debug("Merging key '{0}'", mergeValue);

                    var mergedRecord = new ReportingPointRecord()
                    {
                        Id = outerRecord.Id,
                        ReportingPoint = innerRecord.ReportingPoint,
                        IsConfirmed = innerRecord.IsConfirmed,
                        IsDeleted = innerRecord.IsDeleted,

                        Values = new Dictionary<string, object>()
                    };

                    foreach (var rv in innerRecord.Values)
                    {
                        if (!outerRecord.Values.ContainsKey(rv.Key) ||
                            ExcludedFields().Contains(rv.Key))
                        {
                            mergedRecord.Values.Add(rv.Key, outerRecord.Values[rv.Key]);
                        }
                        else
                        {
                            mergedRecord.Values.Add(rv.Key, innerRecord.Values[rv.Key]);
                        }
                    }

                    mergedRecords.Add(mergedRecord);
                }

                var filePath = Path.Combine(_configuration.WorkingDirectory ?? Environment.CurrentDirectory, _configuration.OutputFile);
                Logger.Information("Writing '{0}' records to file '{1}'", mergedRecords.Count(), filePath);

                _readWriteStrategy.WriteToSheet(filePath, fromReportingPoint.Key, mergedRecords);
            }
        }

        private static string[] ExcludedFields()
        {
            return new string[0];
        }
    }

    public class RapidImpexMergeConfigurationParser : ICommandLineParser<RapidImpexMergeConfiguration>
    {
        private CommandLineParser<RapidImpexMergeConfiguration> _parser;

        public RapidImpexMergeConfigurationParser()
        {
            _parser = new CommandLineParser<RapidImpexMergeConfiguration>();

            _parser.AddKeyValueOption("from", new KeyValueOption<RapidImpexMergeConfiguration, string>(x => x.FromFile));
            _parser.AddKeyValueOption("to", new KeyValueOption<RapidImpexMergeConfiguration, string>(x => x.ToFile));

            _parser.AddKeyValueOption("output", new KeyValueOption<RapidImpexMergeConfiguration, string>(x => x.OutputFile));

            _parser.AddKeyValueOption("key", new KeyValueOption<RapidImpexMergeConfiguration, string>(x => x.MergeField));

            _parser.AddKeyValueOption("path", new KeyValueOption<RapidImpexMergeConfiguration, string>(x => x.WorkingDirectory));
        }

        public bool Parse(string[] args, out RapidImpexMergeConfiguration config)
        {
            RapidImpexMergeConfiguration configuration;

            var result = _parser.Parse(args, out configuration);

            config = configuration;

            return result;
        }
    }

    public class RapidImpexMergeConfiguration
    {
        public string FromFile { get; set; }
        public string ToFile { get; set; }

        public string OutputFile { get; set; }

        public string WorkingDirectory { get; set; }

        public string MergeField { get; set; }
    }
}