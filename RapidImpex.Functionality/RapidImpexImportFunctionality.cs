using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Features.ResolveAnything;
using RapidImpex.Ampla;
using RapidImpex.Data;

namespace RapidImpex.Functionality
{
    public class RapidImpexImportFunctionality : RapidImpexFunctionalityBase
    {
        private readonly AmplaQueryService _amplaQueryService;
        private readonly AmplaCommandService _amplaCommandService;
        private readonly IReportingPointDataReadWriteStrategy _readWriteStrategy;

        public RapidImpexImportFunctionality(AmplaQueryService amplaQueryService, IReportingPointDataReadWriteStrategy readWriteStrategy, AmplaCommandService amplaCommandService)
        {
            _amplaQueryService = amplaQueryService;
            _readWriteStrategy = readWriteStrategy;
            _amplaCommandService = amplaCommandService;
        }

        public override void Execute()
        {
            var records = _readWriteStrategy.Read(Config.WorkingDirectory).ToArray();

            _amplaCommandService.SubmitRecords(records);
            
            _amplaCommandService.DeleteRecords(records.Where(x => x.IsDeleted));

            _amplaCommandService.ConfirmRecords(records.Where(x => x.IsConfirmed));
        }
    }
}