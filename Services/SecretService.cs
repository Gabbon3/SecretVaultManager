using Microsoft.EntityFrameworkCore;
using SecretVaultManager.Crypto.Services;
using SecretVaultManager.Data;
using SecretVaultManager.DTOs.Secret;
using SecretVaultManager.Models;
using System.Text;

namespace SecretVaultManager.Services
{
    /// <summary>
    /// Service for managing secrets in the vault
    /// </summary>
    public interface ISecretService
    {
        /// <summary>
        /// Creates a new secret in the vault
        /// </summary>
        /// <param name="dto">Secret creation data</param>
        /// <returns>The created secret</returns>
        Task<SecretDto> CreateSecretAsync(CreateSecretDto dto);

        /// <summary>
        /// Updates an existing secret
        /// </summary>
        /// <param name="id">ID of the secret to update</param>
        /// <param name="dto">Update data</param>
        /// <returns>Updated secret</returns>
        Task<SecretDto> UpdateSecretAsync(Guid id, UpdateSecretDto dto);

        /// <summary>
        /// Retrieves a secret by its ID (without decryption)
        /// </summary>
        /// <param name="id">Secret ID</param>
        /// <returns>The secret or null if not found</returns>
        Task<SecretDto?> GetSecretByIdAsync(Guid id);

        /// <summary>
        /// Retrieves and decrypts a secret by its ID
        /// </summary>
        /// <param name="id">Secret ID</param>
        /// <returns>Decrypted secret or null if not found</returns>
        Task<DecryptedSecretDto?> GetDecryptedSecretByIdAsync(Guid id);

        /// <summary>
        /// Retrieves a secret by its name (without decryption)
        /// </summary>
        /// <param name="name">Secret name</param>
        /// <returns>The secret or null if not found</returns>
        Task<SecretDto?> GetSecretByNameAsync(string name);

        /// <summary>
        /// Retrieves all secrets (without decryption)
        /// </summary>
        /// <returns>List of all secrets</returns>
        Task<IEnumerable<SecretDto>> GetAllSecretsAsync();

        /// <summary>
        /// Deletes a secret
        /// </summary>
        /// <param name="id">ID of the secret to delete</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> DeleteSecretAsync(Guid id);
    }

    public class SecretService : ISecretService
    {
        private readonly SecretsVaultManagerDb _context;
        private readonly ISecretEncryptionService _encryptionService;
        private readonly ILogger<SecretService> _logger;

        public SecretService(
            SecretsVaultManagerDb context,
            ISecretEncryptionService encryptionService,
            ILogger<SecretService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SecretDto> CreateSecretAsync(CreateSecretDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            ValidateSecretName(dto.Name);
            ValidateSecretValue(dto.PlaintextValue);

            // Check for duplicate name
            if (await _context.Secrets.AnyAsync(s => s.Name == dto.Name))
            {
                throw new InvalidOperationException($"A secret with name '{dto.Name}' already exists");
            }

            try
            {
                // Encrypt the secret value
                var plaintextBytes = Encoding.UTF8.GetBytes(dto.PlaintextValue);
                var encryptedBytes = _encryptionService.EncryptSecret(plaintextBytes);

                var secret = new Secret
                {
                    Name = dto.Name,
                    Encrypted = encryptedBytes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Secrets.Add(secret);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new secret with ID {SecretId}", secret.Id);

                return MapToDto(secret);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create secret");
                throw;
            }
        }

        public async Task<SecretDto> UpdateSecretAsync(Guid id, UpdateSecretDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var secret = await _context.Secrets.FindAsync(id);
            if (secret == null)
            {
                throw new KeyNotFoundException($"Secret with ID {id} not found");
            }

            if (!string.IsNullOrEmpty(dto.Name))
            {
                ValidateSecretName(dto.Name);

                // Check if new name is already taken by another secret
                if (await _context.Secrets.AnyAsync(s => s.Name == dto.Name && s.Id != id))
                {
                    throw new InvalidOperationException($"A secret with name '{dto.Name}' already exists");
                }

                secret.Name = dto.Name;
            }

            if (!string.IsNullOrEmpty(dto.PlaintextValue))
            {
                ValidateSecretValue(dto.PlaintextValue);
                var plaintextBytes = Encoding.UTF8.GetBytes(dto.PlaintextValue);
                secret.Encrypted = _encryptionService.EncryptSecret(plaintextBytes);
            }

            secret.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated secret with ID {SecretId}", secret.Id);
            return MapToDto(secret);
        }

        public async Task<SecretDto?> GetSecretByIdAsync(Guid id)
        {
            var secret = await _context.Secrets.FindAsync(id);
            return secret != null ? MapToDto(secret) : null;
        }

        public async Task<DecryptedSecretDto?> GetDecryptedSecretByIdAsync(Guid id)
        {
            var secret = await _context.Secrets.FindAsync(id);
            if (secret == null)
                return null;

            try
            {
                var decryptedBytes = _encryptionService.DecryptSecret(secret.Encrypted);
                var plaintextValue = Encoding.UTF8.GetString(decryptedBytes);

                return new DecryptedSecretDto
                {
                    Id = secret.Id,
                    Name = secret.Name,
                    PlaintextValue = plaintextValue,
                    CreatedAt = secret.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt secret with ID {SecretId}", secret.Id);
                throw new InvalidOperationException("Failed to decrypt secret", ex);
            }
        }

        public async Task<SecretDto?> GetSecretByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Secret name cannot be empty", nameof(name));

            var secret = await _context.Secrets
                .FirstOrDefaultAsync(s => s.Name == name);

            return secret != null ? MapToDto(secret) : null;
        }

        public async Task<IEnumerable<SecretDto>> GetAllSecretsAsync()
        {
            var secrets = await _context.Secrets.ToListAsync();
            return secrets.Select(MapToDto);
        }

        public async Task<bool> DeleteSecretAsync(Guid id)
        {
            var secret = await _context.Secrets.FindAsync(id);
            if (secret == null)
            {
                return false;
            }

            _context.Secrets.Remove(secret);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted secret with ID {SecretId}", id);
            return true;
        }

        private static SecretDto MapToDto(Secret secret)
        {
            return new SecretDto
            {
                Id = secret.Id,
                Name = secret.Name,
                EncryptedValueBase64 = Convert.ToBase64String(secret.Encrypted),
                CreatedAt = secret.CreatedAt
            };
        }

        private static void ValidateSecretName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Secret name cannot be empty", nameof(name));

            if (name.Length > 100)
                throw new ArgumentException("Secret name cannot exceed 100 characters", nameof(name));
        }

        private static void ValidateSecretValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Secret value cannot be empty", nameof(value));
        }
    }
}