using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OfficeOpenXml;
using RapidImpex.Models;

namespace RapidImpex.Data
{
    public class XlsxReportingPointDataStrategy : IReportingPointDataReadWriteStrategy
    {
        const int dataStartRow = 10;
        const int dataStartCol = 1;

        const int summaryStartRow = 1;
        const int summaryStartCol = 1;

        private readonly IMultiPartFileNamingStrategy _namingStrategy;

        public XlsxReportingPointDataStrategy(IMultiPartFileNamingStrategy namingStrategy)
        {
            _namingStrategy = namingStrategy;
        }

        public IEnumerable<ReportingPointRecord> Read(string inputPath, ReportingPoint reportingPoint)
        {
            var records = new List<ReportingPointRecord>();

            string fileName;
            string worksheetName;
            _namingStrategy.GetFileParts(reportingPoint, out fileName, out worksheetName);

            // open the file
            var filePath = Path.ChangeExtension(Path.Combine(inputPath, fileName), ".xlsx");
            var fileInfo = new FileInfo(filePath);

            using (var package = new ExcelPackage(fileInfo))
            {
                var workbook = package.Workbook;
                var worksheets = workbook.Worksheets;

                var worksheet = worksheets[worksheetName];

                if (worksheet == null)
                {
                    throw new NotImplementedException();
                }

                var fields = reportingPoint.Fields.Values;

                var indexToFieldLookup = new ReportingPointField[fields.Count()];

                // Read header row
                for (var i = 0; i < fields.Count(); i++)
                {
                    var fieldName = worksheet.Cells[dataStartRow + 0, dataStartCol + 3 + i].Text;

                    var field = fields.FirstOrDefault(x => x.Id == fieldName) ??
                                fields.First(x => x.DisplayName == fieldName);

                    indexToFieldLookup[i] = field;
                }

                // Read Data
                int currentRow = dataStartRow + 1;

                while (currentRow < ExcelPackage.MaxRows)
                {
                    var record = new ReportingPointRecord()
                    {
                        ReportingPoint = reportingPoint,
                        Values = new Dictionary<string, object>()
                    };

                    bool rowEmpty = true;

                    var idValue = worksheet.Cells[currentRow, dataStartCol + 0].Text;
                    var confirmedValue = worksheet.Cells[currentRow, dataStartCol + 1].Text;
                    var deletedValue = worksheet.Cells[currentRow, dataStartCol + 2].Text;

                    record.Id = ReadAndSetIsEmpty<long>(idValue, ref rowEmpty);
                    record.IsConfirmed = ReadAndSetIsEmpty<bool>(confirmedValue, ref rowEmpty);
                    record.IsDeleted = ReadAndSetIsEmpty<bool>(deletedValue, ref rowEmpty);

                    for (var i = 0; i < indexToFieldLookup.Count(); i++)
                    {
                        var field = indexToFieldLookup[i];
                        var fieldValue = worksheet.Cells[currentRow, dataStartCol + 3 + i].Text;
                        record.Values[field.Id] = ReadAndSetIsEmpty(fieldValue, field.FieldType, ref rowEmpty);
                    }

                    if (rowEmpty)
                    {
                        break;
                    }

                    records.Add(record);
                    currentRow++;
                }
            }

            return records;
        }

        static T ReadAndSetIsEmpty<T>(string value, ref bool isEmpty)
        {
            var returnValue = ReadAndSetIsEmpty(value, typeof(T), ref isEmpty);

            return returnValue == null ? default(T) : (T)returnValue;
        }

        static object ReadAndSetIsEmpty(string value, Type valueType, ref bool isEmpty)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            isEmpty = false;

            if (valueType == typeof(bool))
            {
                bool boolValue;
                int intvalue;

                if (bool.TryParse(value, out boolValue))
                {
                    return boolValue;
                }

                if (int.TryParse(value, out intvalue))
                {
                    return intvalue != 0;
                }

                throw new NotImplementedException();
            }

            if (valueType == typeof(DateTime))
            {
                return DateTime.SpecifyKind(Convert.ToDateTime(value), DateTimeKind.Local);
            }

            return Convert.ChangeType(value, valueType);
        }

        public void Write(string outputPath, IEnumerable<ReportingPointRecord> records)
        {
            var reportingPointData = records.GroupBy(x => x.ReportingPoint);

            foreach (var rpd in reportingPointData)
            {
                var reportingPoint = rpd.Key;

                string fileName;
                string worksheetName;

                _namingStrategy.GetFileParts(reportingPoint, out fileName, out worksheetName);

                // open the file
                var filePath = Path.ChangeExtension(Path.Combine(outputPath, fileName), ".xlsx");
                var fileInfo = new FileInfo(filePath);

                using (var package = new ExcelPackage(fileInfo))
                {
                    var workbook = package.Workbook;
                    var worksheets = workbook.Worksheets;

                    if (worksheets.Any(x => x.Name == worksheetName))
                    {
                        worksheets.Delete(worksheetName);
                    }

                    var worksheet = worksheets.Add(worksheetName);

                    // Summary

                    worksheet.Cells[summaryStartRow + 0, summaryStartCol + 0].Value = "Module";
                    worksheet.Cells[summaryStartRow + 0, summaryStartCol + 1].Value = reportingPoint.Module;

                    worksheet.Cells[summaryStartRow + 1, summaryStartCol + 0].Value = "Reporting Point";
                    worksheet.Cells[summaryStartRow + 1, summaryStartCol + 1].Value = reportingPoint.FullName;

                    // Data


                    // Header Row
                    worksheet.Cells[dataStartRow + 0, dataStartCol + 0].Value = "Id";
                    worksheet.Cells[dataStartRow + 0, dataStartCol + 1].Value = "Confirmed";
                    worksheet.Cells[dataStartRow + 0, dataStartCol + 2].Value = "Deleted";

                    // Prepare the lookups while the file isn't open
                    var idColumnLookup = new Dictionary<string, int>();
                    var displayNameColumnLookup = new Dictionary<string, int>();
                    // Select the headers out into and array so that it doesnt change over time

                    var fields = reportingPoint.Fields.Values.ToArray();

                    for (var i = 0; i < fields.Count(); i++)
                    {
                        var field = fields[i];

                        idColumnLookup[field.Id] = i;
                        displayNameColumnLookup[field.DisplayName] = i;

                        worksheet.Cells[dataStartRow + 0, dataStartCol + 3 + i].Value = fields[i].DisplayName;
                    }

                    // Data Rows
                    int currentRow = dataStartRow + 1;
                    foreach (var record in rpd)
                    {
                        worksheet.Cells[currentRow, dataStartCol + 0].Value = record.Id;
                        worksheet.Cells[currentRow, dataStartCol + 1].Value = record.IsConfirmed;
                        worksheet.Cells[currentRow, dataStartCol + 2].Value = record.IsDeleted;

                        foreach (var kvp in record.Values)
                        {
                            int fieldColumn;

                            if (idColumnLookup.TryGetValue(kvp.Key, out fieldColumn))
                            {
                            }
                            else if (displayNameColumnLookup.TryGetValue(kvp.Key, out fieldColumn))
                            {
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }

                            worksheet.Cells[currentRow, dataStartCol + 3 + fieldColumn].Value =
                                Convert.ToString(kvp.Value);
                        }

                        currentRow++;
                    }

                    package.Save();
                }
            }
        }
    }
}