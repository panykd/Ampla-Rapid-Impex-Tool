using System;
using System.Collections.Generic;
using System.Linq;
using RapidImpex.Ampla.AmplaData200806;
using RapidImpex.Models;
using RelationshipMatrix = RapidImpex.Models.RelationshipMatrix;

namespace RapidImpex.Ampla
{
    public class AmplaCommandService
    {
        private readonly IDataWebService _client;
        private readonly AmplaQueryService _amplaQueryService;

        public AmplaCommandService(IDataWebService client, AmplaQueryService amplaQueryService)
        {
            _client = client;
            _amplaQueryService = amplaQueryService;
        }

        public void SubmitRecords(IEnumerable<ReportingPointRecord> records)
        {
            var submitDataRecords = new List<SubmitDataRecord>();

            IAmplaVersionModifier versionModifier = new AmplaVersionModifier();

            foreach (var record in records)
            {
                var reportingPoint = record.ReportingPoint;

                var location = record.ReportingPoint.FullName;
                var module = record.ReportingPoint.Module.AsAmplaModule();

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
                    where
                        !rpf.Value.IsReadOnly &&
                        !versionModifier.AdditionalReadonlyFields(reportingPoint).Contains(fv.Key)
                    select new Field()
                    {
                        Name = versionModifier.AmplaFieldName(reportingPoint, fv.Key),
                        Value = ToAmplaValueString(fv.Value)
                    }).ToArray();

                // update the field values for the relationship matrix

                var causeLocationField = fieldValues.FirstOrDefault(x => x.Name == "Cause Location");

                if (causeLocationField != null)
                {
                    var causeLocation = causeLocationField.Value;

                    var relationshipMatrix =
                        new Lazy<RelationshipMatrix>(
                            () => _amplaQueryService.GetRelationshipMatrixFor(reportingPoint, causeLocation));

                    var causeField = fieldValues.FirstOrDefault(x => x.Name == "Cause");

                    if (causeField != null && !String.IsNullOrWhiteSpace(causeField.Value))
                    {
                        causeField.Value = relationshipMatrix.Value.GetCauseCode(causeField.Value).ToString();
                    }

                    var classificationField = fieldValues.FirstOrDefault(x => x.Name == "Classification");

                    if (classificationField != null && !String.IsNullOrWhiteSpace(classificationField.Value))
                    {
                        classificationField.Value =
                            relationshipMatrix.Value.GetCauseCode(classificationField.Value).ToString();
                    }
                }

                submitDataRecord.Fields = fieldValues.ToArray();

                // Handle Record Values
                submitDataRecords.Add(submitDataRecord);
            }

            if (submitDataRecords.Any())
            {
                _client.SubmitData(new SubmitDataRequestMessage(new SubmitDataRequest()
                {
                    SubmitDataRecords = submitDataRecords.ToArray()
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

        public void DeleteRecords(IEnumerable<ReportingPointRecord> records)
        {
            var deleteRecords = new List<DeleteRecord>();

            foreach (var record in records)
            {
                deleteRecords.Add(new DeleteRecord()
                {
                    Location = record.ReportingPoint.FullName,
                    Module = record.ReportingPoint.Module.AsAmplaModule(),
                    MergeCriteria = new DeleteRecordsMergeCriteria()
                    {
                        SetId = record.Id
                    }
                });
            }

            // Submit the records
            if (deleteRecords.Any())
            {
                _client.DeleteRecords(new DeleteRecordsRequestMessage(new DeleteRecordsRequest()
                {
                    DeleteRecords = deleteRecords.ToArray()
                }));
            }
        }

        public void ConfirmRecords(IEnumerable<ReportingPointRecord> records)
        {
            var confirmRecords = new List<UpdateRecordStatus>();

            foreach (var record in records)
            {
                confirmRecords.Add(new UpdateRecordStatus()
                {
                    Location = record.ReportingPoint.FullName,
                    Module = record.ReportingPoint.Module.AsAmplaModule(),
                    RecordAction = UpdateRecordStatusAction.Confirm,
                    MergeCriteria = new UpdateRecordStatusMergeCriteria()
                    {
                        SetId = record.Id
                    }
                });
            }

            if (confirmRecords.Any())
            {
                _client.UpdateRecordStatus(new UpdateRecordStatusRequestMessage(new UpdateRecordStatusRequest()
                {
                    UpdateRecords = confirmRecords.ToArray()
                }));
            }
        }
    }

    public interface IAmplaVersionModifier
    {
        string AmplaFieldName(ReportingPoint reportingPoint, string fieldName);
        string[] AdditionalReadonlyFields(ReportingPoint reportingPoint);
    }

    public class AmplaVersionModifier : IAmplaVersionModifier
    {
        public string AmplaFieldName(ReportingPoint reportingPoint, string fieldName)
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

        public string[] AdditionalReadonlyFields(ReportingPoint reportingPoint)
        {
            return new []
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
        }
    }
}