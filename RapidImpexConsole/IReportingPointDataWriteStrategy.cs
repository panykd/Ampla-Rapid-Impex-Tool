using System.Collections.Generic;
using RapidImpex.Models;

namespace RapidImpexConsole
{
    public interface IReportingPointDataWriteStrategy
    {
        void Write(string outputPath, ReportingPoint reportingPoint, IEnumerable<ReportingPointRecord> records);
    }
}