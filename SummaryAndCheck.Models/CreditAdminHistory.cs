using System.ComponentModel.DataAnnotations;

namespace SummaryAndCheck.Models
{
    public class CreditAdminHistory : CreditHistory
    {
        [Required]
        [MaxLength(1000)]
        public required string Reason { get; set; }

        public required int AdminId { get; set; } // ID of the AppUser who performed the action
    }
}
