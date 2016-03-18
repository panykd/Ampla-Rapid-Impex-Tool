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

        public IEnumerable<ReportingPointRecord> Read(string inputPath, ReportingPoint reportingPoint)
        {
            _locker.EnterReadLock();

            var results = _instance.Read(inputPath, reportingPoint);

            _locker.ExitReadLock();

            return results;
        }

        public void Write(string outputPath, ReportingPoint reportingPoint, IEnumerable<ReportingPointRecord> records)
        {
            _locker.EnterWriteLock();

            _instance.Write(outputPath, reportingPoint, records);

            _locker.ExitWriteLock();
        }
    }
}