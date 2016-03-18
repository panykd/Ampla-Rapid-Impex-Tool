using System;

namespace RapidImpex.Models
{
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
}