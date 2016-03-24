using System.Collections.Generic;
using RapidImpex.Models;

namespace RapidImpex.Data
{
    public interface IReportingPointDataReadWriteStrategy
    {
        IEnumerable<ReportingPointRecord> Read(string inputPath);
        Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>> ReadFromFile(string inputPath);

        void Write(string outputPath, IEnumerable<ReportingPointRecord> records);
        void WriteToFile(string filePath, string worksheetName, ReportingPoint reportingPoint, IEnumerable<ReportingPointRecord> records);
    }
}