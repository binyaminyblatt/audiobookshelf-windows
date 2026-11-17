using System;
using System.Security.Cryptography;
using System.Text;
using NLog;

namespace Audiobookshelf.Common
{
    /// <summary>
    /// Encrypts secrets (network drive passwords, admin API key) before they are written to config.json,
    /// using Windows DPAPI with machine scope. Machine scope is used so the service account and the
    /// tray user can both read what the other wrote. The encrypted value is useless if config.json is
    /// copied to another machine, but any process running on this machine can still decrypt it.
    /// </summary>
    public static class SecretProtector
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private const string Prefix = "dpapi:";
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Audiobookshelf.Common.Secrets.v1");

        public static string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            try
            {
                byte[] encrypted = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(plainText), Entropy, DataProtectionScope.LocalMachine);
                return Prefix + Convert.ToBase64String(encrypted);
            }
            catch (CryptographicException ex)
            {
                _logger.Error($"Could not encrypt secret: {ex.Message}");
                return string.Empty; // never fall back to writing plain text
            }
        }

        public static string Unprotect(string stored)
        {
            if (string.IsNullOrEmpty(stored)) return string.Empty;

            // Not encrypted (hand-edited or legacy value): return as-is, it is re-encrypted on the next save
            if (!stored.StartsWith(Prefix, StringComparison.Ordinal)) return stored;

            try
            {
                byte[] plain = ProtectedData.Unprotect(
                    Convert.FromBase64String(stored.Substring(Prefix.Length)), Entropy, DataProtectionScope.LocalMachine);
                return Encoding.UTF8.GetString(plain);
            }
            catch (Exception ex) when (ex is CryptographicException || ex is FormatException)
            {
                _logger.Error($"Could not decrypt secret (was config.json copied from another machine?): {ex.Message}");
                return string.Empty;
            }
        }
    }
}
