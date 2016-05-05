using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RapidImpex.Ampla;
using RapidImpex.Data;

namespace RapidImpex.Functionality
{
    class RapidImpexFileExportFunctionality : RapidImpexFunctionalityBase
    {
        private readonly AmplaQueryService _amplaQueryService;

        private readonly IReportingPointDataReadWriteStrategy _readWriteStrategy;

        public RapidImpexFileExportFunctionality(AmplaQueryService amplaQueryService, IReportingPointDataReadWriteStrategy readWriteStrategy)
            : base()
        {
            _amplaQueryService = amplaQueryService;
            _readWriteStrategy = readWriteStrategy;

            _readWriteStrategy.AmplaQueryService = amplaQueryService;
        }

        public override void Execute()
        {
            var reportingPoint = Config.Location;
            var module = Config.Module;
            
            var reportingPointInfo = _amplaQueryService.GetReportingPoint(reportingPoint, module);

            var records = _amplaQueryService.GetData(reportingPointInfo, Config.StartTime, Config.EndTime);

            var filePath = Path.Combine(Config.WorkingDirectory, Config.File);

            _readWriteStrategy.WriteToSheet(filePath, reportingPointInfo, records); //Prasanta -- added this line
        }
    }

    class RapidImpexFileImportFunctionality : RapidImpexFunctionalityBase
    {
        private readonly IReportingPointDataReadWriteStrategy _readWriteStrategy;

        private readonly AmplaCommandService _amplaCommandService;

        public RapidImpexFileImportFunctionality(AmplaQueryService amplaQueryService, AmplaCommandService amplaCommandService, IReportingPointDataReadWriteStrategy readWriteStrategy)
        {
            _amplaCommandService = amplaCommandService;
            _readWriteStrategy = readWriteStrategy;
            _readWriteStrategy.AmplaQueryService = amplaQueryService;
        }

        public override void Execute()
        {
            var file = Path.Combine(Config.WorkingDirectory, Config.File);

            var records = _readWriteStrategy.ReadFromFile(file);

            _amplaCommandService.SubmitRecords(records.SelectMany(x => x.Value));
        }
    }
}
