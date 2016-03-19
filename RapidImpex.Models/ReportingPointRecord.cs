using System;
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

    public class RapidImpexConfiguration
    {
        public string WorkingDirectory { get; set; }
        public string[] Modules { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsImport { get; set; }
    }
}