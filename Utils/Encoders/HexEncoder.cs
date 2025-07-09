using System.Text;

namespace SecretVaultManager.Utils.Encoders
{
    public static class HexEncoder
    {
        /// <summary>
        /// Converts a byte array to a hexadecimal string
        /// </summary>
        /// <param name="bytes">Byte array to convert</param>
        /// <param name="uppercase">Whether to use uppercase letters (default: false)</param>
        /// <returns>Hexadecimal string representation</returns>
        public static string ToHexString(byte[] bytes, bool uppercase = false)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            var format = uppercase ? "X2" : "x2";
            var sb = new StringBuilder(bytes.Length * 2);

            foreach (byte b in bytes)
            {
                sb.Append(b.ToString(format));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts a hexadecimal string to a byte array
        /// </summary>
        /// <param name="hexString">Hexadecimal string to convert</param>
        /// <returns>Byte array representation</returns>
        public static byte[] FromHexString(string hexString)
        {
            if (hexString == null)
                throw new ArgumentNullException(nameof(hexString));

            if (hexString.Length % 2 != 0)
                throw new ArgumentException("Hexadecimal string must have even length", nameof(hexString));

            // Handle optional prefixes
            if (hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                hexString = hexString.Substring(2);
            }

            var bytes = new byte[hexString.Length / 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                string byteValue = hexString.Substring(i * 2, 2);
                bytes[i] = Convert.ToByte(byteValue, 16);
            }

            return bytes;
        }

        /// <summary>
        /// Checks if a string is a valid hexadecimal representation
        /// </summary>
        /// <param name="hexString">String to validate</param>
        /// <returns>True if valid hex string</returns>
        public static bool IsValidHexString(string hexString)
        {
            if (string.IsNullOrEmpty(hexString))
                return false;

            // Check for optional prefix
            if (hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                hexString = hexString.Substring(2);
            }

            // Empty string after prefix is invalid
            if (hexString.Length == 0)
                return false;

            // Must have even number of characters
            if (hexString.Length % 2 != 0)
                return false;

            return hexString.All(c => IsHexDigit(c));
        }

        private static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'a' && c <= 'f') ||
                   (c >= 'A' && c <= 'F');
        }
    }
}
