using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NLog;

namespace Audiobookshelf.Common
{
    public static class CryptographicVerifier
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Placeholder string replaced during CI build with the repository public key secret.
        /// </summary>
        public const string INJECTED_KEY_PLACEHOLDER = "__INJECTED_PUBLIC_KEY_XML__";

        /// <summary>
        /// Embedded RSA public key XML injected at build time by the CI/CD workflow.
        /// </summary>
        public static string PUBLIC_KEY_XML = "__INJECTED_PUBLIC_KEY_XML__";

        /// <summary>
        /// Returns the active RSA public key XML string from injected code or environment variable.
        /// </summary>
        public static string GetActivePublicKeyXml()
        {
            if (!string.IsNullOrWhiteSpace(PUBLIC_KEY_XML) && PUBLIC_KEY_XML != INJECTED_KEY_PLACEHOLDER)
            {
                return PUBLIC_KEY_XML.Trim();
            }

            string envKey = Environment.GetEnvironmentVariable("RELEASE_SIGNING_PUBLIC_KEY");
            if (!string.IsNullOrWhiteSpace(envKey))
            {
                return envKey.Trim();
            }

            return string.Empty;
        }

        /// <summary>
        /// Computes the SHA-256 hash of a file as a lowercase hexadecimal string.
        /// </summary>
        public static string ComputeSha256(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return string.Empty;

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(fs);
                return ToHex(hash);
            }
        }

        /// <summary>
        /// Computes the SHA-256 hash of a byte array as a lowercase hexadecimal string.
        /// </summary>
        public static string ComputeSha256(byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;

            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(data);
                return ToHex(hash);
            }
        }

        /// <summary>
        /// Verifies that a file matches an expected SHA-256 hex string.
        /// </summary>
        public static bool VerifyFileHash(string filePath, string expectedSha256Hex)
        {
            if (string.IsNullOrWhiteSpace(expectedSha256Hex))
                return false;

            string computed = ComputeSha256(filePath);
            return string.Equals(computed, expectedSha256Hex.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies the RSA-SHA256 signature of a data buffer.
        /// </summary>
        public static bool VerifySignature(byte[] data, byte[] signatureBytes, string publicKeyXml = null)
        {
            if (data == null || signatureBytes == null || signatureBytes.Length == 0)
                return false;

            string keyXml = !string.IsNullOrWhiteSpace(publicKeyXml) ? publicKeyXml : GetActivePublicKeyXml();
            if (string.IsNullOrWhiteSpace(keyXml))
            {
                _logger.Error("[Security] No active RSA public key configured for signature verification.");
                return false;
            }

            try
            {
                using (var rsa = new RSACryptoServiceProvider(2048))
                {
                    rsa.FromXmlString(keyXml);
                    using (var sha256 = SHA256.Create())
                    {
                        byte[] hash = sha256.ComputeHash(data);
                        return rsa.VerifyHash(hash, CryptoConfig.MapNameToOID("SHA256"), signatureBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error verifying cryptographic signature: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Verifies the RSA-SHA256 signature of a pre-computed SHA-256 hash.
        /// </summary>
        public static bool VerifyHashSignature(byte[] hashBytes, byte[] signatureBytes, string publicKeyXml = null)
        {
            if (hashBytes == null || signatureBytes == null || signatureBytes.Length == 0)
                return false;

            string keyXml = !string.IsNullOrWhiteSpace(publicKeyXml) ? publicKeyXml : GetActivePublicKeyXml();
            if (string.IsNullOrWhiteSpace(keyXml))
            {
                _logger.Error("[Security] No active RSA public key configured for hash signature verification.");
                return false;
            }

            try
            {
                using (var rsa = new RSACryptoServiceProvider(2048))
                {
                    rsa.FromXmlString(keyXml);
                    return rsa.VerifyHash(hashBytes, CryptoConfig.MapNameToOID("SHA256"), signatureBytes);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error verifying hash signature: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Verifies a file against both an expected SHA-256 hash and an RSA digital signature.
        /// </summary>
        public static bool VerifyFileIntegrityAndAuthenticity(string filePath, string expectedSha256Hex, byte[] signatureBytes, string publicKeyXml = null)
        {
            if (!File.Exists(filePath))
            {
                _logger.Error($"[Security] File to verify not found: {filePath}");
                return false;
            }

            try
            {
                byte[] hashBytes;
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var sha256 = SHA256.Create())
                {
                    hashBytes = sha256.ComputeHash(fs);
                }

                string computedHex = ToHex(hashBytes);

                // 1. Verify SHA-256 Checksum
                if (!string.IsNullOrWhiteSpace(expectedSha256Hex))
                {
                    string cleanedExpected = expectedSha256Hex.Trim().ToLowerInvariant();
                    if (!string.Equals(computedHex, cleanedExpected, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.Error($"[Security] SHA-256 Checksum Mismatch! Expected: '{cleanedExpected}', Computed: '{computedHex}'. Binary may be corrupt or modified.");
                        return false;
                    }
                    _logger.Info($"[Security] SHA-256 checksum matched: {computedHex}");
                }

                // 2. Verify Cryptographic Signature
                if (signatureBytes != null && signatureBytes.Length > 0)
                {
                    bool signatureValid = VerifyHashSignature(hashBytes, signatureBytes, publicKeyXml);
                    if (!signatureValid)
                    {
                        _logger.Error("[Security] Cryptographic digital signature verification FAILED! Binary was not compiled by authentic GitHub workflow.");
                        return false;
                    }
                    _logger.Info("[Security] Cryptographic digital signature successfully verified against trusted workflow key.");
                    return true;
                }
                else
                {
                    _logger.Warn("[Security] No cryptographic signature provided to verify authenticity.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"[Security] Exception during file verification: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Parses SHA-256 from a checksum file content (supports raw hash, sha256sum single line, or multi-line SHA256SUMS).
        /// </summary>
        public static string ExtractHashForFile(string checksumText, string targetFilename)
        {
            if (string.IsNullOrWhiteSpace(checksumText)) return string.Empty;

            string[] lines = checksumText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                // Format: "<hash>  <filename>" or "<hash> *<filename>" or just "<hash>"
                string[] parts = line.Split(new[] { ' ', '\t', '*' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1 && parts[0].Length == 64)
                {
                    return parts[0].ToLowerInvariant();
                }
                else if (parts.Length >= 2)
                {
                    string hash = parts[0].Trim();
                    string fileName = parts[parts.Length - 1].Trim();

                    if (hash.Length == 64)
                    {
                        if (string.IsNullOrWhiteSpace(targetFilename) || 
                            string.Equals(Path.GetFileName(fileName), Path.GetFileName(targetFilename), StringComparison.OrdinalIgnoreCase))
                        {
                            return hash.ToLowerInvariant();
                        }
                    }
                }
            }

            return string.Empty;
        }

        private static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
