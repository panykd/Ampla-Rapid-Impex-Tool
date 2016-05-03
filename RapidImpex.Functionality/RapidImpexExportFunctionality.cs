using System;
using System.Collections.Generic;
using System.Linq;
using RapidImpex.Ampla;
using RapidImpex.Data;
using RapidImpex.Models;

namespace RapidImpex.Functionality
{
    public class RapidImpexExportFunctionality : RapidImpexFunctionalityBase
    {
        private readonly AmplaQueryService _amplaQueryService;
        private readonly IReportingPointDataReadWriteStrategy _readWriteStrategy;

        public RapidImpexExportFunctionality(AmplaQueryService amplaQueryService, IReportingPointDataReadWriteStrategy readWriteStrategy)
        {
            _amplaQueryService = amplaQueryService;
            _readWriteStrategy = readWriteStrategy;
        }

        public override void Initialize(RapidImpexConfiguration configuration)
        {
            base.Initialize(configuration);

            _amplaQueryService.Initialize(configuration);
        }

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}