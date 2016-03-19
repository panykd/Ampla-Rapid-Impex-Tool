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

            var records = new List<ReportingPointRecord>();

            foreach (var reportingPoint in reportingPoints)
            {
                records.AddRange(_amplaQueryService.GetData(reportingPoint, Config.StartTime, Config.EndTime));
            }

            _readWriteStrategy.Write(Config.WorkingDirectory, records);
        }
    }
}