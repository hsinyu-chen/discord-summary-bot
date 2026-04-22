namespace SummaryAndCheck.Services
{
    record EnqueueMessage
    {
        public EnqueueResult? EnqueueResult { get; init; }
        public required bool Success { get; init; }
        public required int QueueNumber { get; init; }
        public required string Message { get; init; }
        public object MessageArgs { get; init; } = new { };
    }
}
