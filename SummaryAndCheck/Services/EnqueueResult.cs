namespace SummaryAndCheck.Services
{
    record EnqueueResult(bool IsNewRequest, SummaryRequest ActiveRequest, SummaryRequest UserRequest, int RequestQueuePosition);
}
