using System;
using System.Collections.Generic;
using System.Linq;
using RapidImpex.Ampla;
using RapidImpex.Ampla.AmplaData200806;
using RapidImpex.Models;
using RelationshipMatrix = RapidImpex.Models.RelationshipMatrix;

namespace RapidImpexConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var peristencePath = @"c:\temp\";

            //ExportChain(peristencePath);

            ImportChain(peristencePath);

            //IDataWebService client = new DataWebServiceClient("NetTcp");
        }

        private static string ToAmplaValueString(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is DateTime)
            {
                return ((DateTime) value).AsAmplaDateTime();
            }

            return value.ToString();
        }

        private static string AmplaFieldName(ReportingPoint reportingPoint, string fieldName)
        {
            var rpf = reportingPoint.Fields.First(x => fieldName == x.Value.Id || fieldName == x.Value.DisplayName).Value;

            switch (rpf.Id)
            {
                case "StartDateTime":
                case "EndDateTime":
                case "Explanation":
                case "PercentDowntime":
                case "SampleDateTime":
                    return rpf.DisplayName;
            }

            return fieldName;
        }

        private static void ImportChain(string importPath)
        {
            var reportingPointData = ImportData(importPath);

            var submitDataRecords = new List<SubmitDataRecord>();

            var confirmRecords = new List<UpdateRecordStatus>();
            
            var deleteRecords = new List<DeleteRecord>();

            foreach (var rpd in reportingPointData)
            {
                var reportingPoint = rpd.Key;

                var location = reportingPoint.FullName;
                var module = reportingPoint.Module.AsAmplaModule();

                var excludedFields = new[]
                {
                    "HasAudit",
                    "CreatedBy",
                    "IsManual",
                    "CreatedDateTime",
                    "ConfirmedBy",
                    "ConfirmedDateTime",
                    "IsDeleted",
                    "ObjectId"
                };

                foreach (var record in rpd.Value)
                {

                    var submitDataRecord = new SubmitDataRecord()
                    {
                        Location = location,
                        Module = module,
                        MergeCriteria = new MergeCriteria()
                        {
                            SetId = record.Id
                        }
                    };

                    var fieldValues = (from fv in record.Values
                        join rpf in reportingPoint.Fields on fv.Key equals rpf.Key
                        where !rpf.Value.IsReadOnly && !excludedFields.Contains(fv.Key)
                        select new Field()
                        {
                            Name = AmplaFieldName(reportingPoint, fv.Key),
                            Value = ToAmplaValueString(fv.Value)
                        }).ToArray();

                    // update the field values for the relationship matrix


                    var causeLocationField = fieldValues.FirstOrDefault(x => x.Name == "Cause Location");

                    var amplaQueryService = new AmplaQueryService();

                    if (causeLocationField != null)
                    {
                        var causeLocation = causeLocationField.Value;

                        var relationshipMatrix = new Lazy<RelationshipMatrix>(() => amplaQueryService.GetRelationshipMatrixFor(reportingPoint, causeLocation));

                        var causeField = fieldValues.FirstOrDefault(x => x.Name == "Cause");

                        if(causeField != null && !string.IsNullOrWhiteSpace(causeField.Value))
                        {
                            causeField.Value = relationshipMatrix.Value.GetCauseCode(causeField.Value).ToString();
                        }

                        var classificationField = fieldValues.FirstOrDefault(x => x.Name == "Classification");

                        if (classificationField != null && !string.IsNullOrWhiteSpace(classificationField.Value))
                        {
                            classificationField.Value = relationshipMatrix.Value.GetCauseCode(classificationField.Value).ToString();
                        }
                    }
                    
                    submitDataRecord.Fields = fieldValues.ToArray();

                    // Handle Record Values
                    submitDataRecords.Add(submitDataRecord);

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

            // Submit the records

            IDataWebService client = new DataWebServiceClient("NetTcp");

            if (submitDataRecords.Any())
            {
                client.SubmitData(new SubmitDataRequestMessage(new SubmitDataRequest()
                {
                    SubmitDataRecords = submitDataRecords.ToArray()
                }));
            }

            if (deleteRecords.Any())
            {
                client.DeleteRecords(new DeleteRecordsRequestMessage(new DeleteRecordsRequest()
                {
                    DeleteRecords = deleteRecords.ToArray()
                }));
            }

            if (confirmRecords.Any())
            {
                client.UpdateRecordStatus(new UpdateRecordStatusRequestMessage(new UpdateRecordStatusRequest()
                {
                    UpdateRecords = confirmRecords.ToArray()
                }));
            }
        }

        private static Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>> ImportData(string importPath)
        {
            var amplaQueryService = new AmplaQueryService();

            IMultiPartFileNamingStrategy namingStrategy = new ByAssetXlsxMultiPartNamingStrategy();
            IReportingPointDataReadStrategy readStrategy = new XlsxReportingPointDataStrategy(namingStrategy);

            var modules = new[] {AmplaModules.Downtime, AmplaModules.Production, AmplaModules.Knowledge};
            var reportingPoints = amplaQueryService.GetHeirarchyReportingPointsFor(modules);

            var reportingPointData = new Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>>();

            foreach (var reportingPoint in reportingPoints)
            {
                reportingPointData[reportingPoint] = readStrategy.Read(importPath, reportingPoint).ToArray();
            }

            return reportingPointData;
        }

        private static void ExportChain(string peristencePath)
        {
            var amplaQueryService = new AmplaQueryService();

            var modules = Enum.GetValues(typeof (AmplaModules)).Cast<AmplaModules>();

            var reportingPoints = amplaQueryService.GetHeirarchyReportingPointsFor(modules);

            var endTimeUtc = DateTime.UtcNow;
            var startTimeUtc = endTimeUtc.AddDays(-1);

            var reportingPointData = new Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>>();

            foreach (var reportingPoint in reportingPoints)
            {
                var reportingPointRecords = amplaQueryService.GetData(reportingPoint, startTimeUtc, endTimeUtc);

                reportingPointData.Add(reportingPoint, reportingPointRecords);
            }

            ExportData(peristencePath, reportingPointData);
        }
        
        static void ExportData(string outputPath, Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>> reportingPointData)
        {
            IMultiPartFileNamingStrategy namingStrategy = new ByAssetXlsxMultiPartNamingStrategy();
            IReportingPointDataWriteStrategy writeStrategy = new XlsxReportingPointDataStrategy(namingStrategy);

            foreach (var rpd in reportingPointData)
            {
                writeStrategy.Write(outputPath, rpd.Key, rpd.Value);
            }
        }
    }

    public interface IReportingPointDataReadStrategy
    {
        IEnumerable<ReportingPointRecord> Read(string inputPath, ReportingPoint reportingPoint);
    }
}