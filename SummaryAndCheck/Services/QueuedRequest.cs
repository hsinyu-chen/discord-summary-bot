namespace SummaryAndCheck.Services
{
    class QueuedRequest(SummaryRequest request, Action markAsDone)
    {
        public SummaryState State { get; set; } = SummaryState.Queued;
        public SummaryRequest SummaryRequest { get; } = request;
        public void MarkAsDone()
        {
            markAsDone();
        }
    }
}
