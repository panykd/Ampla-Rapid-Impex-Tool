using System;
using System.Collections.Generic;
using System.Linq;
using RapidImpex.Ampla.AmplaData200806;
using RapidImpex.Models;
using Serilog;
using RelationshipMatrix = RapidImpex.Models.RelationshipMatrix;

namespace RapidImpex.Ampla
{
    public class AmplaCommandService
    {
        public ILogger Logger { get; set; }

        private DataWebServiceFactory _clientFactory;
        private readonly Func<RapidImpexConfiguration, DataWebServiceFactory> _factory;
        private readonly AmplaQueryService _amplaQueryService;
        private int _batchRecord;

        public AmplaCommandService(Func<RapidImpexConfiguration, DataWebServiceFactory> factory, AmplaQueryService amplaQueryService)
        {
            _factory = factory;
            _amplaQueryService = amplaQueryService;
        }

        public void Initialize(RapidImpexConfiguration configuration)
        {
            _clientFactory = _factory(configuration);
            _amplaQueryService.Initialize(configuration);
            _batchRecord = configuration.BatchRecord;
        }

        //Prasanta :: Modified this function to submit records in a batch instead of submittin at a go
        public void SubmitRecords(IEnumerable<ReportingPointRecord> records)
        {
            var submitDataRecords = new List<SubmitDataRecord>();

            IAmplaVersionModifier versionModifier = new AmplaVersionModifier();
            int ctr = 0;

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
                        !versionModifier.AdditionalReadonlyFields(reportingPoint).Contains(rpf.Value.Id) &&
                        !versionModifier.AdditionalReadonlyFields(reportingPoint).Contains(rpf.Value.DisplayName) &&
                        !((rpf.Value.DisplayName == "End Time") && (string.IsNullOrWhiteSpace(Convert.ToString(fv.Value))))
                    select new Field()
                    {
                        Name = versionModifier.AmplaFieldDisplayName(reportingPoint, fv.Key),
                        Value = ToAmplaValueString(fv.Value)
                    }).ToList();

                // update the field values for the relationship matrix

                var causeLocationField = fieldValues.FirstOrDefault(x => x.Name == "Cause Location");
                var causeField = fieldValues.FirstOrDefault(x => x.Name == "Cause");
                var classificationField = fieldValues.FirstOrDefault(x => x.Name == "Classification");
                var effectField = fieldValues.FirstOrDefault(x => x.Name == "Effect");

                if (causeLocationField == null || string.IsNullOrWhiteSpace(causeLocationField.Value))
                {
                    if (causeLocationField != null) fieldValues.Remove(causeLocationField);
                    if (causeField != null) fieldValues.Remove(causeField);
                    if (classificationField != null) fieldValues.Remove(classificationField);
                    if (effectField != null) fieldValues.Remove(effectField);
                }
                else
                {
                    var causeLocation = causeLocationField.Value;
                    //Console.WriteLine(causeLocation);
                    var relationshipMatrix =
                        new Lazy<RelationshipMatrix>(
                            () => _amplaQueryService.GetRelationshipMatrixFor(reportingPoint, causeLocation));

                    // Cause Field

                    if (causeField == null)
                    {
                        // Do Nothing
                    }
                    else if (string.IsNullOrWhiteSpace(causeField.Value))
                    {
                        fieldValues.Remove(causeField);
                    }
                    else
                    {
                        var code = relationshipMatrix.Value.GetCauseCode(causeField.Value);

                        if (code.HasValue)
                        {
                            causeField.Value = code.Value.ToString();
                        }
                        else
                        {
                            Logger.Error("Record: {0}\t\tUnable to find Cause '{1}' for '{2}'@'{3}'. Skipping field.",
                                record.Id, causeField.Value, reportingPoint.FullName, causeLocation);
                            fieldValues.Remove(causeField);
                        }
                    }
                   
                    // Effect Field

                    if (effectField == null)
                    {
                        // Do nothing
                    }
                    else if (effectField.Value == null)
                    {
                        fieldValues.Remove(effectField);
                    }
                    else
                    {
                        var code = relationshipMatrix.Value.GetEffectCode(effectField.Value);

                        if (!string.IsNullOrEmpty(code))
                        {
                            effectField.Value = code;
                        }
                        else
                        {
                            Logger.Error("Record: {0}\t\tUnable to find Effect '{1}' for '{2}'@'{3}'. Skipping field.",
                                record.Id, effectField.Value, reportingPoint.FullName, causeLocation);
                            fieldValues.Remove(effectField);
                        }
                    }

                    // Classificiation Field

                    if (classificationField == null)
                    {
                        // Do nothing
                    }
                    else if (string.IsNullOrWhiteSpace(classificationField.Value))
                    {
                        fieldValues.Remove(classificationField);
                    }
                    else
                    {
                        //Prasanta :: calling wrong method - corrected
                        var code = relationshipMatrix.Value.GetClassificationCode(classificationField.Value);

                        if (code.HasValue)
                        {
                            classificationField.Value = code.Value.ToString();
                        }
                        else
                        {
                            Logger.Error(
                                "Record: {0}\t\tUnable to find Classification '{1}' for '{2}'@'{3}'. Skipping field.",
                                record.Id, classificationField.Value, reportingPoint.FullName, causeLocation);
                            fieldValues.Remove(classificationField);
                        }
                    }
                }

                submitDataRecord.Fields = fieldValues.ToArray();

                //foreach (Field fld in submitDataRecord.Fields)
                //{
                //    Console.WriteLine("Field Name-" + Convert.ToString(fld.Name));
                //    Console.WriteLine("Value-" + Convert.ToString(fld.Value));
                //}

                // Handle Record Values
                submitDataRecords.Add(submitDataRecord);

                ctr++;
                if (ctr == _batchRecord)
                {
                    SubmitDataRecord(submitDataRecords);

                    ctr = 0;
                    submitDataRecords = new List<SubmitDataRecord>();
                }
            }

            SubmitDataRecord(submitDataRecords);
        }

        private void SubmitDataRecord(List<SubmitDataRecord> submitDataRecords)
        {
            var client = _clientFactory.GetClient();

            if (submitDataRecords.Any())
            {
                client.SubmitData(new SubmitDataRequestMessage(new SubmitDataRequest()
                {
                    Credentials = _clientFactory.GetCredentials(),
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

            //Console.WriteLine("Value-" + Convert.ToString(value));
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

            var client = _clientFactory.GetClient();

            // Submit the records
            if (deleteRecords.Any())
            {
                client.DeleteRecords(new DeleteRecordsRequestMessage(new DeleteRecordsRequest()
                {
                    Credentials = _clientFactory.GetCredentials(),
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

            var _client = _clientFactory.GetClient();

            if (confirmRecords.Any())
            {
                _client.UpdateRecordStatus(new UpdateRecordStatusRequestMessage(new UpdateRecordStatusRequest()
                {
                    Credentials = _clientFactory.GetCredentials(),
                    UpdateRecords = confirmRecords.ToArray()
                }));
            }
        }
    }

    public interface IAmplaVersionModifier
    {
        string AmplaFieldName(ReportingPoint reportingPoint, string fieldName);
        string AmplaFieldDisplayName(ReportingPoint reportingPoint, string fieldName);
        string[] AdditionalReadonlyFields(ReportingPoint reportingPoint);
    }

    public class AmplaVersionModifier : IAmplaVersionModifier
    {
        public string AmplaFieldName(ReportingPoint reportingPoint, string fieldName)
        {
            var rpf = reportingPoint.Fields.First(x => fieldName == x.Value.Id || fieldName == x.Value.DisplayName).Value;
            //Console.WriteLine("Field-" + Convert.ToString(fieldName));

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

        public string AmplaFieldDisplayName(ReportingPoint reportingPoint, string fieldName)
        {
            var rpf = reportingPoint.Fields.First(x => fieldName == x.Value.Id || fieldName == x.Value.DisplayName).Value;

            return rpf.DisplayName;
        }

        //Prasanta :: blocked the confirmedby and confirmeddatetime as we need to update these two fields 
        public string[] AdditionalReadonlyFields(ReportingPoint reportingPoint)
        {
            return new []
                {
                    "HasAudit",
                    "CreatedBy",
                    "IsManual",
                    "CreatedDateTime",
                    //"ConfirmedBy",
                    //"ConfirmedDateTime",
                    "IsDeleted",
                    "ObjectId",
                    "CauseLocationEquipmentId",
                    "CauseLocationEquipmentType"
                };
        }
    }
}