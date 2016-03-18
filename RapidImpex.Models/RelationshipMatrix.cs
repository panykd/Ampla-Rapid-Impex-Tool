using System;
using System.Collections.Generic;

namespace RapidImpex.Models
{
    public class RelationshipMatrix
    {
        public RelationshipMatrix(string causeLocation)
        {
            CauseLocation = causeLocation;
            Entries = new List<RelationshipMatrixEntry>();
        }

        public string CauseLocation { get; private set; }

        public List<RelationshipMatrixEntry> Entries { get; set; }

        private readonly Dictionary<string, int> _causeMappings = new Dictionary<string, int>();

        private readonly Dictionary<string, int> _classificationMappings = new Dictionary<string, int>();

        public void UpsertCause(int code, string name)
        {
            _causeMappings[name] = code;
        }

        public void UpsertClassification(int code, string name)
        {
            _classificationMappings[name] = code;
        }

        public void AddEntry(int? causeCode, int? classificationCode)
        {
            if (causeCode.HasValue && !_causeMappings.ContainsValue(causeCode.Value))
            {
                throw new NotImplementedException();
            }

            if (classificationCode.HasValue && !_classificationMappings.ContainsValue(classificationCode.Value))
            {
                throw new NotImplementedException();
            }

            Entries.Add(new RelationshipMatrixEntry() { CauseCode = causeCode, ClassificationCode = classificationCode });
        }

        public int GetCauseCode(string name)
        {
            return _causeMappings[name];
        }

        public int GetClassificationCode(string name)
        {
            return _classificationMappings[name];
        }
    }

    public class RelationshipMatrixEntry
    {
        public int? CauseCode { get; set; }
        public int? ClassificationCode { get; set; }
    }
}