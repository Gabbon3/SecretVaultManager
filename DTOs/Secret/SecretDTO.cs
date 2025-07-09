using System.ComponentModel.DataAnnotations;

namespace SecretVaultManager.DTOs.Secret
{
    public class CreateSecretDto
    {
        public string Name { get; set; } = null!;
        public string PlaintextValue { get; set; } = null!;
    }

    public class UpdateSecretDto
    {
        [StringLength(100, MinimumLength = 1)]
        public string? Name { get; set; }

        public string? PlaintextValue { get; set; }
    }

    public class SecretDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string EncryptedValueBase64 { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class DecryptedSecretDto : SecretDto
    {
        public string PlaintextValue { get; set; } = null!;
    }
}
