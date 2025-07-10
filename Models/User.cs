using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SecretVaultManager.Models
{
    public class User : IdentityUser
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [EmailAddress]
        [Required]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        [StringLength(100)]
        public string PasswordHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<RefreshToken> RefreshTokens { get; } = [];
    }
}
