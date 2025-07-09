using System.ComponentModel.DataAnnotations;

namespace SecretVaultManager.Models
{
    public class Secret
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public byte[] Encrypted { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
    }
}
