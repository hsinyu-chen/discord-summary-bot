using SummaryAndCheck.Models;

namespace SummaryAndCheck.Services
{
    record SummaryContext
    {
        public required string TargetLocale { get; init; }
        public required ulong ShareIdentifier { get; init; }
        public required string Content { get; init; }
        public required UseType Type { get; init; }

    }
}
