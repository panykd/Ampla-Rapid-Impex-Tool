using System.Collections.Generic;
using System.Linq;
using RapidImpex.Ampla;
using RapidImpex.Data;
using RapidImpex.Models;

namespace RapidImpex.Functionality
{
    public class RapidImpexExportFunctionality : RapidImpexImportFunctionalityBase
    {
        private readonly AmplaQueryService _amplaQueryService;
        private readonly IReportingPointDataReadWriteStrategy _readWriteStrategy;

        public RapidImpexExportFunctionality(AmplaQueryService amplaQueryService, IReportingPointDataReadWriteStrategy readWriteStrategy)
        {
            _amplaQueryService = amplaQueryService;
            _readWriteStrategy = readWriteStrategy;
        }

        public override void Execute()
        {
            var modules = Config.Modules.Select(x => x.AsAmplaModule()).ToArray();

            var reportingPoints = _amplaQueryService.GetHeirarchyReportingPointsFor(modules);

            var reportingPointData = new Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>>();

            foreach (var reportingPoint in reportingPoints)
            {
                var reportingPointRecords = _amplaQueryService.GetData(reportingPoint, Config.StartTime, Config.EndTime);

                reportingPointData.Add(reportingPoint, reportingPointRecords);
            }

            foreach (var rpd in reportingPointData)
            {
                _readWriteStrategy.Write(Config.WorkingDirectory, rpd.Key, rpd.Value);
            }
        }
    }
}