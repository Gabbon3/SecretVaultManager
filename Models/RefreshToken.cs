using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecretVaultManager.Models
{
    public class RefreshToken
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // 32 random bytes
        [StringLength(64)]
        public string Token { get; set; }

        public DateTime Expires { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool Revoked { get; set; } = false;

        // Computed properties

        public bool IsExpired => DateTime.UtcNow >= Expires;
        public bool IsRevoked => Revoked == false;
        public bool IsActive => !IsRevoked && !IsExpired;

        // Foreign Keys

        [ForeignKey(nameof(User))]
        public string UserId { get; set; }
        public User User { get; }
    }
}
