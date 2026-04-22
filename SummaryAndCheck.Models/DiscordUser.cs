using System.ComponentModel.DataAnnotations;

namespace SummaryAndCheck.Models
{
    public class DiscordUser
    {
        [Key]
        public required ulong Id { get; set; }
        [Required]
        [MaxLength(500)]
        public required string Name { get; set; }
        public bool IsEnabled {  get; set; }
        public decimal Credit { get; set; }
        public DateTimeOffset? LastFreeUse { get; set; }
        public ICollection<CreditHistory> CreditHistories { get; set; } = [];
    }
}
