using RapidImpex.Models;

namespace RapidImpex.Data
{
    public interface IMultiPartFileNamingStrategy
    {
        void GetFileParts(ReportingPoint reportingPoint, out string fileName, out string partName);
    }
}