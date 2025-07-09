using System.Text;

namespace SecretVaultManager.Utils.Encoders
{
    public static class Base64Encoder
    {
        /// <summary>
        /// Encodes byte array to Base64 string
        /// </summary>
        /// <param name="bytes">Data to encode</param>
        /// <returns>Base64 encoded string</returns>
        public static string ToBase64String(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Encodes byte array to URL-safe Base64 string
        /// </summary>
        /// <param name="bytes">Data to encode</param>
        /// <returns>URL-safe Base64 encoded string</returns>
        public static string ToUrlSafeBase64String(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        /// <summary>
        /// Encodes plain text string to Base64 string
        /// </summary>
        /// <param name="text">Text to encode</param>
        /// <param name="encoding">Text encoding (default: UTF-8)</param>
        /// <returns>Base64 encoded string</returns>
        public static string EncodeText(string text, Encoding encoding = null)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            encoding ??= Encoding.UTF8;
            byte[] bytes = encoding.GetBytes(text);
            return ToBase64String(bytes);
        }

        /// <summary>
        /// Encodes plain text string to URL-safe Base64 string
        /// </summary>
        /// <param name="text">Text to encode</param>
        /// <param name="encoding">Text encoding (default: UTF-8)</param>
        /// <returns>URL-safe Base64 encoded string</returns>
        public static string EncodeUrlSafeText(string text, Encoding encoding = null)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            encoding ??= Encoding.UTF8;
            byte[] bytes = encoding.GetBytes(text);
            return ToUrlSafeBase64String(bytes);
        }

        /// <summary>
        /// Decodes Base64 string to byte array
        /// </summary>
        /// <param name="base64String">Base64 encoded string</param>
        /// <returns>Decoded byte array</returns>
        public static byte[] FromBase64String(string base64String)
        {
            if (base64String == null)
                throw new ArgumentNullException(nameof(base64String));

            // Handle URL-safe Base64
            string s = base64String
                .Replace('-', '+')
                .Replace('_', '/');

            // Pad with '=' to make length a multiple of 4
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }

            return Convert.FromBase64String(s);
        }

        /// <summary>
        /// Decodes Base64 string to plain text
        /// </summary>
        /// <param name="base64String">Base64 encoded string</param>
        /// <param name="encoding">Text encoding (default: UTF-8)</param>
        /// <returns>Decoded text</returns>
        public static string DecodeText(string base64String, Encoding encoding = null)
        {
            if (base64String == null)
                throw new ArgumentNullException(nameof(base64String));

            encoding ??= Encoding.UTF8;
            byte[] bytes = FromBase64String(base64String);
            return encoding.GetString(bytes);
        }

        /// <summary>
        /// Checks if a string is valid Base64
        /// </summary>
        /// <param name="base64String">String to validate</param>
        /// <returns>True if valid Base64</returns>
        public static bool IsValidBase64(string base64String)
        {
            if (string.IsNullOrEmpty(base64String))
                return false;

            // Handle URL-safe Base64 for validation
            string s = base64String
                .Replace('-', '+')
                .Replace('_', '/');

            // Check length (must be multiple of 4)
            if (s.Length % 4 != 0)
                return false;

            // Check for invalid characters
            try
            {
                Convert.FromBase64String(s);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
