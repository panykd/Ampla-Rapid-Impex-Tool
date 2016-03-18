using System.Collections.Generic;
using System.Linq;
using RapidImpex.Ampla;
using RapidImpex.Data;
using RapidImpex.Models;

namespace RapidImpex.Functionality
{
    public class RapidImpexExportFunctionality : RapidImpexImportFunctionalityBase
    {
        public override void Execute()
        {
            var amplaQueryService = new AmplaQueryService();

            var modules = Config.Modules.Select(x => x.AsAmplaModule()).ToArray();

            var reportingPoints = amplaQueryService.GetHeirarchyReportingPointsFor(modules);

            var reportingPointData = new Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>>();

            foreach (var reportingPoint in reportingPoints)
            {
                var reportingPointRecords = amplaQueryService.GetData(reportingPoint, Config.StartTime, Config.EndTime);

                reportingPointData.Add(reportingPoint, reportingPointRecords);
            }

            ExportData(Config.WorkingDirectory, reportingPointData);
        }

        static void ExportData(string outputPath, Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>> reportingPointData)
        {
            IMultiPartFileNamingStrategy namingStrategy = new ByAssetXlsxMultiPartNamingStrategy();

            IReportingPointDataWriteStrategy writeStrategy = new XlsxReportingPointDataStrategy(namingStrategy);

            foreach (var rpd in reportingPointData)
            {
                writeStrategy.Write(outputPath, rpd.Key, rpd.Value);
            }
        }
    }
}