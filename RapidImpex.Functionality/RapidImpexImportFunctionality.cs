using System;
using System.Collections.Generic;
using System.Linq;
using RapidImpex.Ampla;
using RapidImpex.Ampla.AmplaData200806;
using RapidImpex.Data;
using RapidImpex.Models;

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
            var records = ImportData(Config.WorkingDirectory).ToArray();

            _amplaCommandService.SubmitRecords(records);
            

            _amplaCommandService.DeleteRecords(records.Where(x => x.IsDeleted));

            _amplaCommandService.ConfirmRecords(records.Where(x => x.IsConfirmed));
        }

        private IEnumerable<ReportingPointRecord> ImportData(string importPath)
        {
            var modules = Config.Modules.Select(x => x.AsAmplaModule());

            var reportingPoints = _amplaQueryService.GetHeirarchyReportingPointsFor(modules);

            var records = new List<ReportingPointRecord>();

            foreach (var reportingPoint in reportingPoints)
            {
                records.AddRange(_readWriteStrategy.Read(importPath, reportingPoint).ToArray());
            }

            return records;
        }
    }
}