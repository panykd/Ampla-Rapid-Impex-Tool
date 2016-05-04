using System.Collections.Generic;

namespace RapidImpex.Models
{
    public class ReportingPointRecord
    {
        public ReportingPoint ReportingPoint { get; set; }

        public long Id { get; set; }

        public bool IsDeleted { get; set; }

        public bool IsConfirmed { get; set; }

        public Dictionary<string, object> Values { get; set; }  
    }
}