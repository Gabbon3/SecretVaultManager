using MessagePack;
using SecretVaultManager.Crypto.Models;
using SecretVaultManager.Crypto.Symmetric;
using System.Security.Cryptography;

namespace SecretVaultManager.Crypto.Services
{
    /// <summary>
    /// Service for encrypting and decrypting secrets using symmetric encryption
    /// </summary>
    public interface ISecretEncryptionService
    {
        /// <summary>
        /// Encrypts secret data
        /// </summary>
        /// <param name="plaintext">Data to encrypt</param>
        /// <param name="dekId">Optional Data Encryption Key ID (uses default if not specified)</param>
        /// <returns>Encrypted package as byte array</returns>
        /// <exception cref="ArgumentNullException">Thrown when plaintext is null</exception>
        /// <exception cref="KeyNotFoundException">Thrown when specified DEK is not found</exception>
        /// <exception cref="CryptographicException">Thrown when encryption fails</exception>
        byte[] EncryptSecret(byte[] plaintext, string dekId = null);

        /// <summary>
        /// Decrypts an encrypted secret package
        /// </summary>
        /// <param name="encryptedPackageBytes">Encrypted package bytes</param>
        /// <returns>Decrypted data as byte array</returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null</exception>
        /// <exception cref="NotSupportedException">Thrown when package uses unsupported algorithm</exception>
        /// <exception cref="KeyNotFoundException">Thrown when DEK is not found</exception>
        /// <exception cref="CryptographicException">Thrown when decryption fails</exception>
        byte[] DecryptSecret(byte[] encryptedPackageBytes);
    }

    /// <summary>
    /// Implementation of secret encryption service using AES-256-GCM
    /// </summary>
    public sealed class SecretEncryptionService : ISecretEncryptionService
    {
        private const string SupportedAlgorithm = "AES-256-GCM";
        private const int CurrentVersion = 1;

        private readonly IKeyManagementService _keyManagementService;

        /// <summary>
        /// Initializes a new instance of the SecretEncryptionService
        /// </summary>
        /// <param name="keyManagementService">Key management service</param>
        /// <exception cref="ArgumentNullException">Thrown when keyManagementService is null</exception>
        public SecretEncryptionService(IKeyManagementService keyManagementService)
        {
            _keyManagementService = keyManagementService ?? throw new ArgumentNullException(nameof(keyManagementService));
        }

        /// <inheritdoc />
        public byte[] EncryptSecret(byte[] plaintext, string dekId = null)
        {
            // Validate input
            if (plaintext == null)
                throw new ArgumentNullException(nameof(plaintext));
            if (plaintext.Length == 0)
                throw new ArgumentException("Plaintext cannot be empty", nameof(plaintext));

            try
            {
                // Use default key if not specified
                dekId ??= _keyManagementService.DefaultKeyId;
                var dek = _keyManagementService.GetKey(dekId);

                // Encrypt data
                var encryptedBytes = AES256GCM.Encrypt(plaintext, dek);

                // Create package with metadata
                var package = new EncryptedPackage
                {
                    Alg = SupportedAlgorithm,
                    Version = CurrentVersion,
                    DekId = dekId,
                    Encrypted = encryptedBytes
                };

                // Serialize with MessagePack
                return MessagePackSerializer.Serialize(package);
            }
            catch (Exception ex) when (ex is not ArgumentNullException && ex is not ArgumentException)
            {
                throw new CryptographicException("Failed to encrypt secret", ex);
            }
        }

        /// <inheritdoc />
        public byte[] DecryptSecret(byte[] encryptedPackageBytes)
        {
            // Validate input
            if (encryptedPackageBytes == null)
                throw new ArgumentNullException(nameof(encryptedPackageBytes));
            if (encryptedPackageBytes.Length == 0)
                throw new ArgumentException("Encrypted package cannot be empty", nameof(encryptedPackageBytes));

            try
            {
                // Deserialize the package
                var package = MessagePackSerializer.Deserialize<EncryptedPackage>(encryptedPackageBytes);

                // Validate package
                if (package.Alg != SupportedAlgorithm)
                    throw new NotSupportedException($"Unsupported algorithm: {package.Alg}. Only {SupportedAlgorithm} is supported.");

                if (package.Version > CurrentVersion)
                    throw new NotSupportedException($"Package version {package.Version} is not supported. Maximum supported version is {CurrentVersion}.");

                // Get the encryption key
                var dek = _keyManagementService.GetKey(package.DekId);

                // Decrypt data
                return AES256GCM.Decrypt(package.Encrypted, dek);
            }
            catch (MessagePackSerializationException ex)
            {
                throw new CryptographicException("Failed to deserialize encrypted package", ex);
            }
            catch (Exception ex) when (ex is not NotSupportedException)
            {
                throw new CryptographicException("Failed to decrypt secret", ex);
            }
        }
    }
}
