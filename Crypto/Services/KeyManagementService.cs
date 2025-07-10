using SecretVaultManager.Utils.Encoders;

namespace SecretVaultManager.Crypto.Services
{
    public interface IKeyManagementService
    {
        /// <summary>
        /// Retrieves a Data Encryption Key (DEK) by its identifier
        /// </summary>
        /// <param name="keyId">The DEK identifier</param>
        /// <returns>The requested encryption key</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the specified key is not found</exception>
        byte[] GetKey(string keyId);

        /// <summary>
        /// Lists all available key identifiers
        /// </summary>
        /// <returns>Enumerable collection of key IDs</returns>
        IEnumerable<string> ListKeyIds();

        /// <summary>
        /// Gets the current default key ID
        /// </summary>
        string DefaultKeyId { get; }
    }

    public class KeyManagementService : IKeyManagementService
    {
        private readonly Dictionary<string, byte[]> _keys;
        private readonly string _defaultKeyId;

        public KeyManagementService(IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _keys = LoadKeysFromConfiguration(configuration);
            _defaultKeyId = DetermineDefaultKeyId(configuration);
        }

        public string DefaultKeyId => _defaultKeyId;

        public byte[] GetKey(string keyId)
        {
            if (string.IsNullOrWhiteSpace(keyId))
                throw new ArgumentException("Key ID cannot be null or whitespace", nameof(keyId));

            if (_keys.TryGetValue(keyId, out var key))
                return key;

            throw new KeyNotFoundException($"Encryption key with ID '{keyId}' not found. Available keys: {string.Join(", ", _keys.Keys)}");
        }

        public IEnumerable<string> ListKeyIds() => _keys.Keys;

        private Dictionary<string, byte[]> LoadKeysFromConfiguration(IConfiguration configuration)
        {
            Dictionary<string, byte[]> deks = new()
            {
                { "1", HexEncoder.FromHexString(configuration["Crypto:DEK"]) }
            };
            return deks;
        }

        private string DetermineDefaultKeyId(IConfiguration configuration)
        {
            // Try to get default from config, fall back to first available key
            return configuration["DefaultKeyId"] ?? _keys.Keys.First();
        }
    }
}
