using System.Security.Cryptography;

namespace SecretVaultManager.Crypto.Symmetric
{
    public class AES256GCM
    {
        // Tag size in bytes (recommended 16 for GCM)
        private const int TagSize = 16;
        // Nonce size in bytes (recommended 12 for GCM)
        private const int NonceSize = 12;

        /// <summary>
        /// Encrypts data using AES-256-GCM
        /// </summary>
        /// <param name="plaintext">Data to encrypt</param>
        /// <param name="key">Encryption key (must be 32 bytes for AES-256)</param>
        /// <param name="nonce">Optional nonce (if null, generates a random one)</param>
        /// <param name="aad">Optional Additional Authenticated Data</param>
        /// <returns>Combined byte array of [nonce, encryptedData, authTag]</returns>
        /// <example>
        /// Encrypting with AAD (Additional Authenticated Data):
        /// <code>
        /// byte[] key = AES256GCM.GenerateKey();
        /// byte[] data = Encoding.UTF8.GetBytes("Sensitive data");
        /// byte[] aad = Encoding.UTF8.GetBytes("Contextual metadata");
        /// byte[] encrypted = AES256GCM.Encrypt(data, key, null, aad);
        /// </code>
        /// </example>
        public static byte[] Encrypt(byte[] plaintext, byte[] key, byte[] nonce = null, byte[] aad = null)
        {
            if (key == null || key.Length != 32)
                throw new ArgumentException("Key must be 32 bytes for AES-256", nameof(key));

            // Generate nonce if not provided
            nonce ??= GenerateRandomBytes(NonceSize);
            if (nonce.Length != NonceSize)
                throw new ArgumentException($"Nonce must be {NonceSize} bytes", nameof(nonce));

            // Create buffers for encrypted data and tag
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TagSize];

            using (var aesGcm = new AesGcm(key, TagSize))
            {
                aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, aad);
            }

            // Combine nonce, encrypted data and tag into single array
            var result = new byte[nonce.Length + ciphertext.Length + tag.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(ciphertext, 0, result, nonce.Length, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length + ciphertext.Length, tag.Length);

            return result;
        }

        /// <summary>
        /// Decrypts data encrypted with AES-256-GCM
        /// </summary>
        /// <param name="ciphertextWithMetadata">Combined byte array of [nonce, encryptedData, authTag]</param>
        /// <param name="key">Encryption key (must be 32 bytes for AES-256)</param>
        /// <param name="aad">Optional Additional Authenticated Data</param>
        /// <returns>Decrypted data</returns>
        /// <example>
        /// Decrypting with AAD (Additional Authenticated Data):
        /// <code>
        /// byte[] key = ...; // Stessa chiave usata per l'encrypt
        /// byte[] encryptedData = ...; // Dati cifrati (con nonce e tag inclusi)
        /// byte[] aad = Encoding.UTF8.GetBytes("Metadata123"); // Deve essere lo stesso usato in Encrypt
        /// byte[] decrypted = AES256GCM.Decrypt(encryptedData, key, aad);
        /// string originalMessage = Encoding.UTF8.GetString(decrypted);
        /// </code>
        /// </example>
        public static byte[] Decrypt(byte[] ciphertextWithMetadata, byte[] key, byte[] aad = null)
        {
            if (key == null || key.Length != 32)
                throw new ArgumentException("Key must be 32 bytes for AES-256", nameof(key));

            if (ciphertextWithMetadata.Length < NonceSize + TagSize)
                throw new ArgumentException("Ciphertext is too short", nameof(ciphertextWithMetadata));

            // Extract components from the combined array
            var nonce = new byte[NonceSize];
            var tag = new byte[TagSize];
            var ciphertext = new byte[ciphertextWithMetadata.Length - NonceSize - TagSize];

            Buffer.BlockCopy(ciphertextWithMetadata, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(ciphertextWithMetadata, NonceSize, ciphertext, 0, ciphertext.Length);
            Buffer.BlockCopy(ciphertextWithMetadata, NonceSize + ciphertext.Length, tag, 0, TagSize);

            var plaintext = new byte[ciphertext.Length];

            using (var aesGcm = new AesGcm(key, TagSize))
            {
                aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, aad);
            }

            return plaintext;
        }

        /// <summary>
        /// Generates a cryptographically secure random byte array
        /// </summary>
        /// <param name="length">Length of the byte array</param>
        /// <returns>Random bytes</returns>
        public static byte[] GenerateRandomBytes(int length)
        {
            var bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);
            return bytes;
        }

        /// <summary>
        /// Generates a random key suitable for AES-256
        /// </summary>
        /// <returns>32-byte encryption key</returns>
        public static byte[] GenerateKey()
        {
            return GenerateRandomBytes(32);
        }
    }
}
