using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using OfficeOpenXml;
using RapidImpex.Ampla.AmplaData200806;

namespace RapidImpexConsole
{
    class Program
    {
        private static DataWebServiceClient _client;

        static void Main(string[] args)
        {
            _client = new DataWebServiceClient("NetTcp");

            var peristencePath = @"c:\temp\";

            //ExportChain(peristencePath);

            ImportChain(peristencePath);
        }

        private static void ImportChain(string importPath)
        {
            var reportingPointData = ImportData(importPath);

            var submitDataRecords = new List<SubmitDataRecord>();

            var confirmRecords = new List<UpdateRecordStatus>();
            
            var deleteRecords = new List<DeleteRecord>();

            foreach (var rpd in reportingPointData)
            {
                var location = rpd.Key.FullName;
                var module = rpd.Key.Module.AsAmplaModule();

                foreach (var record in rpd.Value)
                {
                    // Handle Record Values
                    submitDataRecords.Add(new SubmitDataRecord()
                    {
                        Location = location,
                        Module = module,
                        MergeCriteria = new MergeCriteria()
                        {
                            SetId = record.Id
                        },

                        Fields = record.Values.Select(x => new Field()
                        {
                            Name = x.Key,
                            Value = x.Value == null ? null : x.Value.ToString()
                        }).ToArray()
                    });


                    // Handle Confirmed Records
                    if (record.IsConfirmed)
                    {
                        confirmRecords.Add(new UpdateRecordStatus()
                        {
                            Location = location,
                            Module = module,
                            RecordAction = UpdateRecordStatusAction.Confirm,
                            MergeCriteria = new UpdateRecordStatusMergeCriteria()
                            {
                                SetId = record.Id
                            }
                        });
                    }

                    // Handle Deleted Records
                    if (record.IsDeleted)
                    {
                        deleteRecords.Add(new DeleteRecord()
                        {
                            Location = location,
                            Module = module,
                            MergeCriteria = new DeleteRecordsMergeCriteria()
                            {
                                SetId = record.Id
                            }
                        });
                    }
                }                
            }
        }

        private static Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>> ImportData(string importPath)
        {
            var reportingPointData = new Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>>();

            DirectoryInfo directoryInfo = new DirectoryInfo(importPath);

            var fileInfos = directoryInfo.GetFiles("*.xlsx");

            foreach (var fileInfo in fileInfos)
            {
                using (var package = new ExcelPackage(fileInfo))
                {
                    var workbook = package.Workbook;
                    var worksheets = workbook.Worksheets;

                    foreach (var worksheet in worksheets)
                    {
                        var module = worksheet.Cells[summaryStartRow + 0, summaryStartCol + 1].Text;
                        var reportingPointName = worksheet.Cells[summaryStartRow + 1, summaryStartCol + 1].Text;

                        var reportingPoint = new ReportingPoint()
                        {
                            FullName = reportingPointName,
                            Module = module
                        };

                        var reportingPointMetaData = GetReportingPointFieldInformation(reportingPoint);

                        var fields = reportingPointMetaData.Values;

                        var indexToFieldLookup = new ReportingPointField[fields.Count()];

                        // Read header row
                        for (var i = 0; i < fields.Count(); i++)
                        {
                            var fieldName = worksheet.Cells[dataStartRow + 0, dataStartCol + 3 + i].Text;

                            var field = fields.FirstOrDefault(x => x.Id == fieldName) ??
                                        fields.First(x => x.DisplayName == fieldName);

                            indexToFieldLookup[i] = field;
                        }

                        var records = new List<ReportingPointRecord>();
                        
                        // Read Data
                        int currentRow = dataStartRow + 1;

                        while (currentRow < ExcelPackage.MaxRows)
                        {
                            var record = new ReportingPointRecord()
                            {
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

                        reportingPointData[reportingPoint] = records.ToArray();
                    }
                }
            }

            return reportingPointData;
        }

        static T ReadAndSetIsEmpty<T>(string value, ref bool isEmpty)
        {
            var returnValue = ReadAndSetIsEmpty(value, typeof (T), ref isEmpty);

            return returnValue == null ? default(T) : (T) returnValue;
        }

        static object ReadAndSetIsEmpty(string value, Type valueType, ref bool isEmpty)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            isEmpty = false;

            if (valueType == typeof (bool))
            {
                bool boolValue;
                int intvalue;

                if(bool.TryParse(value, out boolValue))
                {
                    return boolValue;
                }
                
                if (int.TryParse(value, out intvalue))
                {
                    return intvalue != 0;
                }

                throw new NotImplementedException();
            }

            return Convert.ChangeType(value, valueType);
        }

        const int dataStartRow = 10;
        const int dataStartCol = 1;

        const int summaryStartRow = 1;
        const int summaryStartCol = 1;

        private static void ExportChain(string peristencePath)
        {
            var modules = Enum.GetValues(typeof (AmplaModules)).Cast<AmplaModules>();

            var reportingPoints = GetHeirarchyReportingPointsFor(modules);

            var endTimeUtc = DateTime.UtcNow;
            var startTimeUtc = endTimeUtc.AddDays(-1);

            var reportingPointData = new Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>>();

            foreach (var reportingPoint in reportingPoints)
            {
                var reportingPointRecords = GetData(reportingPoint, startTimeUtc, endTimeUtc);

                reportingPointData.Add(reportingPoint, reportingPointRecords);
            }

            ExportData(peristencePath, reportingPointData);
        }

        static void ExportData(string outputPath, Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>> reportingPointData)
        {
            // Refactor this to allow multiple strategies

            foreach (var rpd in reportingPointData)
            {
                var nameParts = rpd.Key.FullName.Split(new[] {"."}, StringSplitOptions.None);

                var partsInFile = nameParts.Count() - 2;

                var fileName = string.Join(" ", nameParts.Take(partsInFile));
                var worksheetName = string.Concat(nameParts.Skip(partsInFile)).Replace(" ", "");

                // Determine our files

                var reportingPoint = rpd.Key;
                var records = rpd.Value;

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
                    foreach (var record in records)
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

        static IEnumerable<ReportingPointRecord> GetData(ReportingPoint reportingPoint, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            var request = new GetDataRequest
            {
                Filter = new DataFilter
                {
                    Location = reportingPoint.FullName,
                    SamplePeriod =
                        string.Format(">= {0} AND < {1}", startTimeUtc.AsAmplaDateTime(), endTimeUtc.AsAmplaDateTime())
                },
                Metadata = false,
                OutputOptions = new GetDataOutputOptions { ResolveIdentifiers = true }, // TODO: Verify this matches what is returned from AllowedValues }
                View = new GetDataView
                {
                    Context = NavigationContext.Plant,
                    Mode = NavigationMode.Location,
                    Module = reportingPoint.Module.AsAmplaModule(),
                }
            };

            var data = _client.GetData(request);

            var records = new List<ReportingPointRecord>();

            foreach (var rowSet in data.RowSets)
            {
                foreach (var dataRow in rowSet.Rows)
                {
                    records.Add(dataRow.CreateRecordFor(reportingPoint));
                }
            }

            return records.ToArray();
        }

        static IEnumerable<ReportingPoint> GetHeirarchyReportingPointsFor(IEnumerable<AmplaModules> modules)
        {
            var reportingPoints = new List<ReportingPoint>();

            foreach (var module in modules)
            {
                var response = _client.GetNavigationHierarchy(new GetNavigationHierarchyRequest()
                {
                    Context = NavigationContext.Plant,
                    Mode = NavigationMode.Location,
                    Module = module
                });

                reportingPoints.AddRange(ExtractReportingPointsFromHeirachyViewPoints(response.Hierarchy.ViewPoints, response.Context.Module));
            }

            return reportingPoints.ToArray();
        }

        static IEnumerable<ReportingPoint> ExtractReportingPointsFromHeirachyViewPoints(IEnumerable<ViewPoint> viewPoints, AmplaModules module)
        {
            var allReportingPoints = new List<ReportingPoint>();

            foreach (var viewPoint in viewPoints)
            {
                foreach (var viewReportingPoint in viewPoint.ReportingPoints)
                {
                    var reportingPoint = new ReportingPoint
                    {
                        FullName = viewReportingPoint.id,
                        DisplayName = viewReportingPoint.DisplayName,
                        Module = module.ToString(),
                    };

                    reportingPoint.Fields = GetReportingPointFieldInformation(reportingPoint);

                    allReportingPoints.Add(reportingPoint);
                }

                allReportingPoints.AddRange(ExtractReportingPointsFromHeirachyViewPoints(viewPoint.ViewPoints, module));
            }

            return allReportingPoints;
        }

        static Dictionary<string, ReportingPointField> GetReportingPointFieldInformation(ReportingPoint reportingPoint)
        {
            var response = _client.GetViews(new GetViewsRequest
            {
                Context = NavigationContext.Plant,
                Mode = NavigationMode.Location, 
                Module = reportingPoint.Module.AsAmplaModule(),
                @ViewPoint = reportingPoint.FullName
            });

            var reportingPointView = response.Views.Single();

            var fields = reportingPointView.Fields.Select(fieldView => new ReportingPointField()
            {
                Id = fieldView.name,
                DisplayName = fieldView.displayName,
                IsReadOnly = fieldView.readOnly,
                IsMandatory = fieldView.required,
                HasAllowedValues = fieldView.hasAllowedValues,
                FieldType = fieldView.type.FromAmplaType()
            }).ToDictionary(k => k.Id, v => v);

            // Get Allowed Values
            var fieldsWithAllowedValues = fields.Values.Where(x => x.HasAllowedValues).Select(x => x.Id).ToArray();

            if (fieldsWithAllowedValues.Any())
            {
                var allowedValuesResponse = _client.GetAllowedValues(new GetAllowedValuesRequest
                {
                    Module = reportingPoint.Module.AsAmplaModule(),
                    Location = reportingPoint.FullName,
                    Fields = fieldsWithAllowedValues.ToArray()
                });

                foreach (var result in allowedValuesResponse.AllowedValueFields)
                {
                    fields[result.Field].AllowedValues = result.AllowedValues;
                }
            }

            return fields;
        }
    }

    public class ReportingPointRecord
    {
        public long Id { get; set; }

        public bool IsDeleted { get; set; }

        public bool IsConfirmed { get; set; }

        public Dictionary<string, object> Values { get; set; }  
    }

    public static class AmplaHelpers
    {
        public static Type FromAmplaType(this string value)
        {
            switch (value)
            {
                case "xs:Boolean":
                    return typeof (bool);

                case "xs:String":
                    return typeof (string);

                case "xs:DateTime":
                    return typeof(DateTime);

                case "xs:Int":
                    return typeof (int);

                case "xs:Double":
                    return typeof (double);

                default:
                    throw new ArgumentOutOfRangeException("value", value);
            }
        }

        public static T SafeGet<T>(this Row row, string name)
        {
            var element = row.Any.FirstOrDefault(x => HttpUtility.HtmlDecode(x.Name) == name);

            if (element == null)
            {
                return default(T);
            }

            var value = element.Value ?? element.InnerText;

            return (T) Convert.ChangeType(value, typeof (T));
        }

        public static ReportingPointRecord CreateRecordFor(this Row row, ReportingPoint reportingPoint)
        {
            var excludedFields = new[] {"Deleted", "Confirmed"};

            var record = new ReportingPointRecord
            {
                Id = long.Parse(row.id),
                IsDeleted = row.SafeGet<bool>("Deleted"),
                IsConfirmed = row.SafeGet<bool>("Confirmed"),
                Values = new Dictionary<string, object>()
            };

            foreach (var element in row.Any)
            {
                var name = XmlConvert.DecodeName(element.Name);

                string fieldName;
                ReportingPointField field;
                if ((field = reportingPoint.Fields.Values.FirstOrDefault(x => x.Id == name)) != null)
                {
                    fieldName = field.Id;
                }
                else if ((field = reportingPoint.Fields.Values.FirstOrDefault(x => x.DisplayName == name)) != null)
                {
                    fieldName = field.DisplayName;   
                }
                else
                {
                    continue;
                }

                if (excludedFields.Contains(name))
                {
                    continue;
                }

                var fieldValue = element.Value ?? element.InnerText;

                record.Values[fieldName] = Convert.ChangeType(fieldValue, field.FieldType);
            }

            return record;
        }

        public static AmplaModules AsAmplaModule(this string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            AmplaModules module;

            if (Enum.TryParse(value, out module))
            {
                return module;
            }

            throw new ArgumentException("Unrecognised Ampla Module", "value");
        }

        public static string AsAmplaDateTime(this DateTime value)
        {
            //const string format = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'";
            const string format = "yyyy-MM-ddTHH:mm:ssZ";

            if (value.Kind == DateTimeKind.Unspecified)
            {
                throw new NotImplementedException();
            }

            var utcDateTime = value.ToUniversalTime();

            return utcDateTime.ToString(format, CultureInfo.InvariantCulture);
        }
    }

    public class ReportingPointField
    {
        public string Id { get; set; }

        public string DisplayName { get; set; }

        public bool IsReadOnly { get; set; }

        public bool IsMandatory { get; set; }

        public bool HasAllowedValues { get; set; }

        public string[] AllowedValues { get; set; }

        public Type FieldType { get; set; }
    }

    [DebuggerDisplay("{DisplayName ({Module})}")]
    public class ReportingPoint
    {
        public string FullName { get; set; }

        public string DisplayName { get; set; }

        public string Module { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1})", FullName, Module);
        }

        public Dictionary<string, ReportingPointField> Fields { get; set; }   
    }
}