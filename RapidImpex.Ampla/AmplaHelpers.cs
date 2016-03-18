using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using RapidImpex.Ampla.AmplaData200806;
using RapidImpex.Models;

namespace RapidImpex.Ampla
{
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
            var element = row.Any.FirstOrDefault(x => XmlConvert.DecodeName(x.Name) == name);

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

            if (Enum.TryParse(value, true, out module))
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
}