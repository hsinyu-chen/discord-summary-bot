using System.ComponentModel.DataAnnotations;

namespace SummaryAndCheck.Models
{
    public class AppUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        public byte[]? StoredCredential { get; set; }

        public AppUserStatus Status { get; set; } = AppUserStatus.Pending;
    }

    public enum AppUserStatus
    {
        Pending,
        Active,
        Disabled
    }
}
