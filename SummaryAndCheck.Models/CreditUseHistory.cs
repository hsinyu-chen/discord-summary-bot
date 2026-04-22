namespace SummaryAndCheck.Models
{
    public class CreditUseHistory : CreditHistory
    {
        public required ulong ServerId { get; set; }
        public required ulong ChannelId { get; set; }
        public required ulong MessageId { get; set; }
        public required UseType UseType { get; set; }
    }
}
