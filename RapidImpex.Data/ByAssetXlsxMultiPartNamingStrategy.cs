using System;
using System.Linq;
using RapidImpex.Models;

namespace RapidImpex.Data
{
    public class ByAssetXlsxMultiPartNamingStrategy : IMultiPartFileNamingStrategy
    {
        public void GetFileParts(ReportingPoint reportingPoint, out string fileName, out string partName)
        {
            var nameParts = reportingPoint.FullName.Split(new[] { "." }, StringSplitOptions.None);

            var partsInFile = nameParts.Count() - 2;

            fileName = string.Join(" ", nameParts.Take(partsInFile));
            partName = string.Concat(nameParts.Skip(partsInFile)).Replace(" ", "");

            // Xlsx tabs have a maximum length of 31 characters
            partName = partName.Length > 31 ? partName.Substring(0, 31) : partName;
        }
    }
}