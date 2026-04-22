using System.ComponentModel.DataAnnotations;

namespace SummaryAndCheck.Models
{
    public class CreditTopUpHistory : CreditHistory
    {
        [Required]
        [MaxLength(500)]
        public required string OrderId { get; set; }
        [Required]
        public required long AccountBindingId { get; set; }
        public required DiscordUserAccountBinding AccountBinding { get; set; }
    }
}
