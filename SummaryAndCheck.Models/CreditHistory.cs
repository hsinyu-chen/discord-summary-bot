using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SummaryAndCheck.Models
{
    public abstract class CreditHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public required DateTimeOffset HistoryTime { get; set; }
        public required decimal ValueChanged { get; set; }
        public required ulong DiscordUserId { get; set; }
        public DiscordUser DiscordUser { get; set; } = default!;
        public CreditHistoryType HistoryType { get; set; }
    }
}
