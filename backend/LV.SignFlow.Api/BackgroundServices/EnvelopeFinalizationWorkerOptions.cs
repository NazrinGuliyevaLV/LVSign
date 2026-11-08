namespace LV.SignFlow.Api.BackgroundServices
{
    public sealed class EnvelopeFinalizationWorkerOptions
    {
        public const string SectionName =
            "FinalizationWorker";

        public bool Enabled { get; set; } =
            true;

        public int PollIntervalSeconds { get; set; } =
            5;

        public int LeaseDurationSeconds { get; set; } =
            300;

        public int LeaseRenewIntervalSeconds { get; set; } =
            60;
    }
}
