using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SummaryAndCheck.Models
{
    public class DiscordUserAccountBinding
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        [Required]
        [MaxLength(100)]
        public required string PlatformName { get; set; }
        /// <summary>
        /// email or other
        /// </summary>
        [Required]
        [MaxLength(1024)]
        public required string Identity { get; set; }

        public required DateTimeOffset CreatedTime { get; set; }
        public ICollection<CreditTopUpHistory> CreditTopUpHistories { get; set; } = [];
    }
}
