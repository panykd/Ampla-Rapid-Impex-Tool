using System.Collections.Generic;
using System.Threading;
using RapidImpex.Models;

namespace RapidImpex.Data
{
    public class ThreadLockedReportingPointDataReadWriteStrategyAdapter : IReportingPointDataReadWriteStrategy
    {
        private readonly IReportingPointDataReadWriteStrategy _instance;

        readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        public ThreadLockedReportingPointDataReadWriteStrategyAdapter(IReportingPointDataReadWriteStrategy instance)
        {
            _instance = instance;
        }

        public IEnumerable<ReportingPointRecord> Read(string inputPath)
        {
            _locker.EnterReadLock();

            var results = _instance.Read(inputPath);

            _locker.ExitReadLock();

            return results;
        }

        public Dictionary<ReportingPoint, IEnumerable<ReportingPointRecord>> ReadFromFile(string inputPath)
        {
            _locker.EnterReadLock();

            var results = _instance.ReadFromFile(inputPath);

            _locker.ExitReadLock();

            return results;
        }

        public void Write(string outputPath, IEnumerable<ReportingPointRecord> records)
        {
            _locker.EnterWriteLock();

            _instance.Write(outputPath, records);

            _locker.ExitWriteLock();
        }

        public void WriteToFile(string filePath, string worksheetName, ReportingPoint reportingPoint, IEnumerable<ReportingPointRecord> records)
        {
            _locker.EnterWriteLock();

            _instance.WriteToFile(filePath, worksheetName, reportingPoint, records);

            _locker.ExitWriteLock();
        }
    }
}