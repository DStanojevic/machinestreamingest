using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MachineDataApi.Instrumentation
{
    public static class InstrumentationConstants
    {
        public const string AppSource = "MachineDataApi";
        public static readonly ActivitySource DefaultActivitySource = new ActivitySource(AppSource);
        public static readonly Meter DefaultMeter = new Meter(AppSource);
        public static readonly Counter<long> ProcessedMessagesCounter = DefaultMeter.CreateCounter<long>("processed_messages_counter");
    }
}
