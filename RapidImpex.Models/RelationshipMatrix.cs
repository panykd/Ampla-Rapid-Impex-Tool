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

        private readonly Dictionary<string, string> _effectMappings = new Dictionary<string, string>();

        public void UpsertCause(int code, string name)
        {
            _causeMappings[name] = code;
        }

        public void UpsertClassification(int code, string name)
        {
            _classificationMappings[name] = code;
        }

        public void UpsertEffect(string code, string name)
        {
            _effectMappings[name] = code;
        }

        public void AddEntry(int? causeCode, int? classificationCode, string effectCode)
        {
            if (causeCode.HasValue && !_causeMappings.ContainsValue(causeCode.Value))
            {
                throw new NotImplementedException();
            }

            if (classificationCode.HasValue && !_classificationMappings.ContainsValue(classificationCode.Value))
            {
                throw new NotImplementedException();
            }

            if (!string.IsNullOrWhiteSpace(effectCode) && !_effectMappings.ContainsValue(effectCode))
            {
                throw new NotImplementedException();
            }

            Entries.Add(new RelationshipMatrixEntry() { CauseCode = causeCode, ClassificationCode = classificationCode, EffectCode = effectCode });
        }

        public int? GetCauseCode(string name)
        {
            int causeCode;

            return _causeMappings.TryGetValue(name, out causeCode) ? causeCode : (int?)null;
        }

        public int? GetClassificationCode(string name)
        {
            int classificationCode;

            return _causeMappings.TryGetValue(name, out classificationCode) ? classificationCode : (int?)null;
        }

        public string GetEffectCode(string name)
        {
            string effectCode;
            return _effectMappings.TryGetValue(name, out effectCode) ? effectCode : null;
        }
    }

    public class RelationshipMatrixEntry
    {
        public int? CauseCode { get; set; }
        public int? ClassificationCode { get; set; }
        public string EffectCode { get; set; }
    }
}