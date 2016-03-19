﻿using System.Collections.Generic;
using RapidImpex.Models;

namespace RapidImpex.Data
{
    public interface IReportingPointDataReadWriteStrategy
    {
        IEnumerable<ReportingPointRecord> Read(string inputPath, ReportingPoint reportingPoint);

        void Write(string outputPath, IEnumerable<ReportingPointRecord> records);
    }
}