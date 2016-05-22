using System.Collections.Generic;
using System.IO;
using Serilog.Sinks.Amazon.Kinesis.Common;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.HttpLogShipperTests
{
    interface ILogShipperProtectedDelegator
    {
        string PrepareRecord(MemoryStream stream);
        string SendRecords(List<string> records, out bool successful);
        void HandleError(string response, int originalRecordCount);
    }

    class LogShipperSUT : HttpLogShipperBase<string, string>
    {
        private readonly ILogShipperProtectedDelegator _delegator;

        public LogShipperSUT(
            ILogShipperProtectedDelegator delegator,
            ILogShipperOptions options,
            ILogReaderFactory logReaderFactory,
            IPersistedBookmarkFactory persistedBookmarkFactory,
            ILogShipperFileManager fileManager
            ) : base(options, logReaderFactory, persistedBookmarkFactory, fileManager)
        {
            _delegator = delegator;
        }

        public void ShipIt()
        {
            base.ShipLogs();
        }

        protected override string PrepareRecord(MemoryStream stream)
        {
            return _delegator.PrepareRecord(stream);
        }

        protected override string SendRecords(List<string> records, out bool successful)
        {
            return _delegator.SendRecords(records, out successful);
        }

        protected override void HandleError(string response, int originalRecordCount)
        {
            _delegator.HandleError(response, originalRecordCount);
        }
    }
}
