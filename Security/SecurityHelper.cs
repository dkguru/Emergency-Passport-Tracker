using System;
using System.Security.Cryptography;

namespace Emergency_Passport_Tracker.Security
{
    /// <summary>
    /// Key derivation and PIN hashing helpers.
    /// </summary>
    /// <remarks>
    /// Uses the static Rfc2898DeriveBytes.Pbkdf2 method. The instance constructors of
    /// Rfc2898DeriveBytes are obsolete as of .NET 9 (SYSLIB0060). The derived bytes are identical
    /// to the previous constructor-based code, so data files written by earlier builds still
    /// decrypt correctly.
    /// </remarks>
    public static class SecurityHelper
    {
        public const int Iterations = 100_000;
        public const int SaltSize = 16;
        public const int KeySize = 32;

        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        public static (byte[] hash, byte[] salt) HashPin(string pin)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, Algorithm, KeySize);
            return (hash, salt);
        }

        public static bool VerifyPin(string pin, byte[] hash, byte[] salt)
        {
            byte[] candidate = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, Algorithm, KeySize);
            try
            {
                return CryptographicOperations.FixedTimeEquals(hash, candidate);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(candidate);
            }
        }

        /// <summary>Derives the 32-byte AES key used by the legacy / EPT1 file formats.</summary>
        public static byte[] DeriveKey(string pin, byte[] salt)
        {
            return Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, Algorithm, KeySize);
        }

        /// <summary>
        /// Derives 64 bytes: the first 32 are the AES key, the last 32 the HMAC key.
        /// The first 32 bytes are identical to <see cref="DeriveKey"/>, so a single derivation
        /// pass serves both the old and new file formats.
        /// </summary>
        public static byte[] DeriveKeys(string pin, byte[] salt)
        {
            return Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, Algorithm, KeySize * 2);
        }
    }
}
