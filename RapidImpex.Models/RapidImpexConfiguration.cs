using System;

namespace RapidImpex.Models
{
    public class RapidImpexConfiguration
    {
        public string WorkingDirectory { get; set; }
        public string File { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        
        public bool IsImport { get; set; }
        public bool UseSimpleAuthentication { get; set; }
        
        public string Username { get; set; }
        public string Password { get; set; }
        public bool UseBasicHttp { get; set; }

        public string Location { get; set; }
        public string[] Modules { get; set; }
        public string Module { get; set; }

        //Prasanta  :: added this to control the number of records to be submitted on a batch during submit operation
        public int BatchRecord { get; set; }
        
    }
}