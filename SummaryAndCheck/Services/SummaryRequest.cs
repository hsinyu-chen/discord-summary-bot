// GeminiService.cs
using Discord.WebSocket;

namespace SummaryAndCheck.Services
{
    class SummaryRequest
    {
        public required Task<string?> OriginalResponseLink { get; init; }
        public required SocketCommandBase SocketCommand { get; init; }
        public required SummaryContext SummaryContext { get; init; }
    }
}