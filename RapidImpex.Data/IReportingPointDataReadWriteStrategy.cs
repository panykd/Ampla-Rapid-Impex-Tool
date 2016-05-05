using System.Collections.Generic;
using RapidImpex.Ampla;
using RapidImpex.Models;

namespace RapidImpex.Data
{
    public interface IReportingPointDataReadWriteStrategy
    {
        IEnumerable<ReportingPointRecord> Read(string inputPath);
        Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>> ReadFromFile(string inputPath);

        void Write(string outputPath, IEnumerable<ReportingPointRecord> records);
        void WriteToSheet(string filePath, ReportingPoint reportingPoint, IEnumerable<ReportingPointRecord> records); //Prasanta :: added this method
        void WriteToFile(string filePath, string worksheetName, ReportingPoint reportingPoint, IEnumerable<ReportingPointRecord> records);
        
        IAmplaQueryService AmplaQueryService { get; set; }
    }
}