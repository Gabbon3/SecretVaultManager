using MessagePack;

namespace SecretVaultManager.Crypto.Models
{
    [MessagePackObject]
    public class EncryptedPackage
    {
        [Key(0)]
        public string Alg { get; set; } = "AES-256-GCM";

        [Key(1)]
        public int Version { get; set; } = 1;

        [Key(2)]
        public string DekId { get; set; } = "1";

        [Key(3)]
        public byte[] Encrypted { get; set; }
    }
}
