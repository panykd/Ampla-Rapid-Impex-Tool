using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RapidImpex.Data;
using RapidImpex.Models;
using RapidImpexConsole;
using Serilog;

public class QuikMerge
{
    private IReportingPointDataReadWriteStrategy _readWriteStrategy;

    public QuikMerge(IReportingPointDataReadWriteStrategy readWriteStrategy)
    {
        _readWriteStrategy = readWriteStrategy;
    }

    static MyCommandLineParser<QuikMergeConfiguration> CreateParser()
    {
        var parser = new MyCommandLineParser<QuikMergeConfiguration>();

        parser.AddKeyValueOption("from", new KeyValueOption<QuikMergeConfiguration, string>(x => x.FromFile));
        parser.AddKeyValueOption("to", new KeyValueOption<QuikMergeConfiguration, string>(x => x.ToFile));
        
        parser.AddKeyValueOption("output", new KeyValueOption<QuikMergeConfiguration, string>(x => x.OutputFile));
        
        parser.AddKeyValueOption("key", new KeyValueOption<QuikMergeConfiguration, string>(x => x.MergeField));
        
        parser.AddKeyValueOption("path", new KeyValueOption<QuikMergeConfiguration, string>(x => x.WorkingDirectory));

        return parser;
    }

    private static string[] ExcludedFields(ReportingPoint reportingPoint)
    {
        return new string[0];
    }

    public ILogger Logger { get; set; }

    public void Run(string[] args)
    {
        QuikMergeConfiguration configuration;

        var parser = CreateParser();

        if (!parser.Parse(args, out configuration))
        {
            Logger.Fatal("Unable to parse configuration: '{0}'", string.Join(" | ", args));
            return;
        }

        // Load FROM files
        Logger.Information("Loading FROM records in file '{0}' into memory...", configuration.FromFile);
        
        var fromReportingPoints = _readWriteStrategy.ReadFromFile(configuration.FromFile);
        var fromReportingPointsRecordCount = fromReportingPoints.Select(x => x.Value.Count()).Sum();

        Logger.Debug("Loaded '{0}' records from '{1}' Reporting Points", fromReportingPointsRecordCount, fromReportingPoints.Count());

        // Load TO files
        Logger.Information("Loading FROM records in file '{0}' into memory...", configuration.FromFile);

        var toReportingPoints = _readWriteStrategy.ReadFromFile(configuration.ToFile);
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

            if(trps.Count() > 1)
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
                var mergeValue = innerRecord.Values[configuration.MergeField];

                var outerRecords = outerReportingPointRecords.Where(x => x.Values[configuration.MergeField].Equals(mergeValue)).ToArray();

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
                        ExcludedFields(innerRecord.ReportingPoint).Contains(rv.Key))
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

            var filePath = Path.Combine(configuration.WorkingDirectory ?? Environment.CurrentDirectory, configuration.OutputFile);
            Logger.Information("Writing '{0}' records to file '{1}'", mergedRecords.Count(), filePath);

            _readWriteStrategy.WriteToSheet(filePath, fromReportingPoint.Key, mergedRecords);
        }
    }
}