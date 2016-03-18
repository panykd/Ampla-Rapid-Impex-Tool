using System.Collections.Generic;

namespace RapidImpex.Models
{
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