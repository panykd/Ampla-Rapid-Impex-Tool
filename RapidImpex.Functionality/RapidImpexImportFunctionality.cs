using System;
using System.Collections.Generic;
using System.Linq;
using RapidImpex.Ampla;
using RapidImpex.Ampla.AmplaData200806;
using RapidImpex.Data;
using RapidImpex.Models;
using RelationshipMatrix = RapidImpex.Models.RelationshipMatrix;

namespace RapidImpex.Functionality
{
    public class RapidImpexImportFunctionality : RapidImpexImportFunctionalityBase
    {
        private readonly IDataWebService _dataWebService;
        private readonly AmplaQueryService _amplaQueryService;
        private readonly IReportingPointDataReadWriteStrategy _readWriteStrategy;

        public RapidImpexImportFunctionality(IDataWebService dataWebService, AmplaQueryService amplaQueryService, IReportingPointDataReadWriteStrategy readWriteStrategy)
        {
            _dataWebService = dataWebService;
            _amplaQueryService = amplaQueryService;
            _readWriteStrategy = readWriteStrategy;
        }

        public override void Execute()
        {
            var reportingPointData = ImportData(Config.WorkingDirectory);

            SubmitRecords(reportingPointData);

            DeleteRecords(reportingPointData);

            ConfirmRecords(reportingPointData);
        }

        private void SubmitRecords(Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>> reportingPointData)
        {
            var submitDataRecords = new List<SubmitDataRecord>();

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

                    if (causeLocationField != null)
                    {
                        var causeLocation = causeLocationField.Value;

                        var relationshipMatrix =
                            new Lazy<RelationshipMatrix>(() => _amplaQueryService.GetRelationshipMatrixFor(reportingPoint, causeLocation));

                        var causeField = fieldValues.FirstOrDefault(x => x.Name == "Cause");

                        if (causeField != null && !string.IsNullOrWhiteSpace(causeField.Value))
                        {
                            causeField.Value = relationshipMatrix.Value.GetCauseCode(causeField.Value).ToString();
                        }

                        var classificationField = fieldValues.FirstOrDefault(x => x.Name == "Classification");

                        if (classificationField != null && !string.IsNullOrWhiteSpace(classificationField.Value))
                        {
                            classificationField.Value =
                                relationshipMatrix.Value.GetCauseCode(classificationField.Value).ToString();
                        }
                    }

                    submitDataRecord.Fields = fieldValues.ToArray();

                    // Handle Record Values
                    submitDataRecords.Add(submitDataRecord);
                }
            }

            if (submitDataRecords.Any())
            {
                _dataWebService.SubmitData(new SubmitDataRequestMessage(new SubmitDataRequest()
                {
                    SubmitDataRecords = submitDataRecords.ToArray()
                }));
            }
        }

        private void DeleteRecords(Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>> reportingPointData)
        {
            var deleteRecords = new List<DeleteRecord>();

            foreach (var rpd in reportingPointData)
            {
                var reportingPoint = rpd.Key;

                var location = reportingPoint.FullName;
                var module = reportingPoint.Module.AsAmplaModule();

                foreach (var record in rpd.Value)
                {
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
            if (deleteRecords.Any())
            {
                _dataWebService.DeleteRecords(new DeleteRecordsRequestMessage(new DeleteRecordsRequest()
                {
                    DeleteRecords = deleteRecords.ToArray()
                }));
            }
        }

        private void ConfirmRecords(Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>> reportingPointData)
        {
            var confirmRecords = new List<UpdateRecordStatus>();

            foreach (var rpd in reportingPointData)
            {
                var reportingPoint = rpd.Key;

                var location = reportingPoint.FullName;
                var module = reportingPoint.Module.AsAmplaModule();

                foreach (var record in rpd.Value)
                {
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
                }
            }

            if (confirmRecords.Any())
            {
                _dataWebService.UpdateRecordStatus(new UpdateRecordStatusRequestMessage(new UpdateRecordStatusRequest()
                {
                    UpdateRecords = confirmRecords.ToArray()
                }));
            }
        }

        private static string ToAmplaValueString(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is DateTime)
            {
                return ((DateTime)value).AsAmplaDateTime();
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

        private Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>> ImportData(string importPath)
        {
            var modules = Config.Modules.Select(x => x.AsAmplaModule());

            var reportingPoints = _amplaQueryService.GetHeirarchyReportingPointsFor(modules);

            var reportingPointData = new Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>>();

            foreach (var reportingPoint in reportingPoints)
            {
                reportingPointData[reportingPoint] = _readWriteStrategy.Read(importPath, reportingPoint).ToArray();
            }

            return reportingPointData;
        }
    }
}