using System;
using System.Collections.Generic;
using System.Linq;
using RapidImpex.Ampla.AmplaData200806;
using RapidImpex.Models;
using RelationshipMatrix = RapidImpex.Models.RelationshipMatrix;

namespace RapidImpex.Ampla
{
    public class AmplaQueryService
    {
        private readonly Func<RapidImpexConfiguration, DataWebServiceFactory> _factory;
        private DataWebServiceFactory _clientFactory;

        public AmplaQueryService(Func<RapidImpexConfiguration, DataWebServiceFactory> factory)
        {
            _factory = factory;
        }

        public void Initialize(RapidImpexConfiguration configuration)
        {
            _clientFactory = _factory(configuration);
        }

        public IEnumerable<ReportingPoint> GetHeirarchyReportingPointsFor(IEnumerable<AmplaModules> modules)
        {
            var reportingPoints = new List<ReportingPoint>();

            var client = _clientFactory.GetClient();

            foreach (var module in modules)
            {
                var response = client.GetNavigationHierarchy(new GetNavigationHierarchyRequestMessage(new GetNavigationHierarchyRequest()
                {
                    Context = NavigationContext.Plant,
                    Mode = NavigationMode.Location,
                    Module = module, 
                    Credentials = _clientFactory.GetCredentials()
                }));

                var viewPoints = response.GetNavigationHierarchyResponse.Hierarchy.ViewPoints;
                reportingPoints.AddRange(ExtractReportingPointsFromHeirachyViewPoints(viewPoints, module));
            }

            return reportingPoints.ToArray();
        }

        private IEnumerable<ReportingPoint> ExtractReportingPointsFromHeirachyViewPoints(IEnumerable<ViewPoint> viewPoints, AmplaModules module)
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

        public ReportingPoint GetReportingPoint(string location, string module)
        {
            var reportingPoint = new ReportingPoint()
            {
                FullName = location,
                Module = module                
            };

            reportingPoint.Fields = GetReportingPointFieldInformation(reportingPoint);

            return reportingPoint;
        }

        public Dictionary<string, ReportingPointField> GetReportingPointFieldInformation(ReportingPoint reportingPoint)
        {
            var _client = _clientFactory.GetClient();

            var response = _client.GetViews(new GetViewsRequestMessage(new GetViewsRequest
            {
                Context = NavigationContext.Plant,
                Mode = NavigationMode.Location,
                Module = reportingPoint.Module.AsAmplaModule(),
                Credentials = _clientFactory.GetCredentials(),
                ViewPoint = reportingPoint.FullName
            }));

            var reportingPointView = response.GetViewsResponse.Views.Single();

            var fields = reportingPointView.Fields.Select(fieldView => new ReportingPointField()
            {
                Id = fieldView.name,
                DisplayName = fieldView.displayName,
                IsReadOnly = fieldView.readOnly,
                IsMandatory = fieldView.required,
                HasAllowedValues = fieldView.hasAllowedValues,
                FieldType = fieldView.type.FromAmplaType()
            })
            .ToDictionary(k => k.Id, v => v);

            // Get Allowed Values
            var fieldsWithAllowedValues = fields.Values.Where(x => x.HasAllowedValues).Select(x => x.Id).ToArray();

            if (!fieldsWithAllowedValues.Any())
            {
                return fields;   
            }

            var allowedValuesResponse = _client.GetAllowedValues(new GetAllowedValuesRequestMessage(new GetAllowedValuesRequest
            {
                Module = reportingPoint.Module.AsAmplaModule(),
                Location = reportingPoint.FullName,
                Credentials = _clientFactory.GetCredentials(),
                Fields = fieldsWithAllowedValues.ToArray()
            }));

            foreach (var result in allowedValuesResponse.GetAllowedValuesResponse.AllowedValueFields)
            {
                fields[result.Field].AllowedValues = result.AllowedValues;
            }

            return fields;
        }

        public IEnumerable<ReportingPointRecord> GetData(ReportingPoint reportingPoint, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            var _client = _clientFactory.GetClient();

            var request = new GetDataRequest
            {
                Credentials = _clientFactory.GetCredentials(),

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
                    Module = reportingPoint.Module.AsAmplaModule()
                }
            };

            var data = _client.GetData(new GetDataRequestMessage(request));

            var records = new List<ReportingPointRecord>();

            foreach (var rowSet in data.GetDataResponse.RowSets)
            {
                foreach (var dataRow in rowSet.Rows)
                {
                    records.Add(dataRow.CreateRecordFor(reportingPoint));
                }
            }

            return records.ToArray();
        }

        public RelationshipMatrix GetRelationshipMatrixFor(ReportingPoint reportingPoint, string causeLocation)
        {
            var _client = _clientFactory.GetClient();

            var response = _client.GetRelationshipMatrixValues(
                    new GetRelationshipMatrixValuesRequestMessage(new GetRelationshipMatrixValuesRequest()
                    {
                        Credentials = _clientFactory.GetCredentials(),
                        Location = reportingPoint.FullName,
                        Module = reportingPoint.Module.AsAmplaModule(),
                        DependentFieldValues = new[] { new DependentFieldValue() { name = "Cause Location", Value = causeLocation }, }
                    }));

            var relationshipMatrix = new RelationshipMatrix(causeLocation);

            foreach (var rmv in response.GetRelationshipMatrixValuesResponse.RelationshipMatrixValues)
            {
                var cause = rmv.MatrixValues.FirstOrDefault(x => x.name == "Cause");

                int? causeCode = null;
                int? classificationCode = null;

                if (cause != null)
                {
                    causeCode = Convert.ToInt32(cause.id);
                    relationshipMatrix.UpsertCause(causeCode.Value, cause.Value);
                }

                var classification = rmv.MatrixValues.FirstOrDefault(x => x.name == "Classification");

                if (classification != null)
                {
                    classificationCode = Convert.ToInt32(classification.id);
                    relationshipMatrix.UpsertClassification(classificationCode.Value, classification.Value);
                }

                if (causeCode.HasValue || classificationCode.HasValue)
                {
                    relationshipMatrix.AddEntry(causeCode, classificationCode);
                }
            }

            return relationshipMatrix;
        }
    }
}