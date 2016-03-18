using System;
using System.Collections.Generic;
using System.Linq;
using RapidImpex.Ampla;
using RapidImpex.Ampla.AmplaData200806;
using RapidImpex.Data;
using RapidImpex.Models;
using RelationshipMatrix = RapidImpex.Models.RelationshipMatrix;

namespace RapidImpex.Functionality
{
    public class RapidImpexImportFunctionality : RapidImpexImportFunctionalityBase
    {
        private readonly IDataWebService _dataWebService;
        private readonly AmplaQueryService _amplaQueryService;
        private readonly AmplaCommandService _amplaCommandService;
        private readonly IReportingPointDataReadWriteStrategy _readWriteStrategy;

        public RapidImpexImportFunctionality(IDataWebService dataWebService, AmplaQueryService amplaQueryService, IReportingPointDataReadWriteStrategy readWriteStrategy)
        {
            _dataWebService = dataWebService;
            _amplaQueryService = amplaQueryService;
            _readWriteStrategy = readWriteStrategy;
        }

        public override void Execute()
        {
            var reportingPointData = ImportData(Config.WorkingDirectory);

            _amplaCommandService.SubmitRecords(reportingPointData);

            _amplaCommandService.DeleteRecords(reportingPointData);

            _amplaCommandService.ConfirmRecords(reportingPointData);
        }

        private Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>> ImportData(string importPath)
        {
            var modules = Config.Modules.Select(x => x.AsAmplaModule());

            var reportingPoints = _amplaQueryService.GetHeirarchyReportingPointsFor(modules);

            var reportingPointData = new Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>>();

            foreach (var reportingPoint in reportingPoints)
            {
                reportingPointData[reportingPoint] = _readWriteStrategy.Read(importPath, reportingPoint).ToArray();
            }

            return reportingPointData;
        }
    }
}