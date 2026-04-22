using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace SummaryAndCheck.Services
{
    class SummaryQueue(IMemoryCache memoryCache)
    {
        readonly ConcurrentDictionary<SummaryContext, QueuedRequest> summaries = new();
        readonly ConcurrentQueue<QueuedRequest> requests = new();
        public bool FindFromCache(SummaryRequest request, out EnqueueResult? cachedResult)
        {
            if (memoryCache.TryGetValue(request.SummaryContext, out var cached) && cached is SummaryRequest cachedRequest)
            {
                cachedResult = new(false, cachedRequest, request, -1);
                return true;
            }
            cachedResult = default;
            return false;
        }
        public EnqueueResult Enqueue(SummaryRequest request)
        {
            var context = request.SummaryContext;
            //search for archived
            if (FindFromCache(request, out var cachedResult))
            {
                return cachedResult!;
            }
            var result = summaries.GetOrAdd(context, context =>
            {
                var req = new QueuedRequest(request, () =>
                {
                    if (summaries.TryRemove(context, out var finishedRequests))
                    {
                        if (finishedRequests.State == SummaryState.Success)
                        {
                            memoryCache.Set(finishedRequests.SummaryRequest.SummaryContext, finishedRequests.SummaryRequest, TimeSpan.FromHours(1));
                        }
                    }
                });
                requests.Enqueue(req);
                return req;
            });
            if (ReferenceEquals(result.SummaryRequest, request))
            {
                return new(true, result.SummaryRequest, request, requests.Count);
            }
            else
            {
                return new(false, result.SummaryRequest, request, -1);
            }
        }
        public bool TryGetNext(out QueuedRequest task)
        {
            return requests.TryDequeue(out task!);
        }
    }
}
