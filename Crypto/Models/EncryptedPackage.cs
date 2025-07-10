using MessagePack;

namespace SecretVaultManager.Crypto.Models
{
    [MessagePackObject]
    public class EncryptedPackage
    {
        [Key(0)]
        public AesHeader Header { get; set; }

        [Key(1)]
        public byte[] Payload { get; set; }
    }
}
