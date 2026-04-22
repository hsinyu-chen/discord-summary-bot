using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SummaryAndCheck.Models
{
    public class SystemConfig
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        [Required]
        [MaxLength(100)]
        public required string Scope { get; set; }
        [Required]
        [MaxLength(100)]
        public required string Key { get; set; }
        [MaxLength(1024)]
        public string? Value { get; set; }
    }
}
