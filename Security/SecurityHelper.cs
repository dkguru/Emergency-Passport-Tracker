using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Emergency_Passport_Tracker.Security
{
    public static class SecurityHelper
    {
        public static (byte[] hash, byte[] salt) HashPin(string pin)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);

            using var pbkdf2 = new Rfc2898DeriveBytes(pin, salt, 100000, HashAlgorithmName.SHA256);
            return (pbkdf2.GetBytes(32), salt);
        }

        public static bool VerifyPin(string pin, byte[] hash, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(pin, salt, 100000, HashAlgorithmName.SHA256);
            return CryptographicOperations.FixedTimeEquals(hash, pbkdf2.GetBytes(32));
        }

        public static byte[] DeriveKey(string pin, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(pin, salt, 100000, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(32);
        }
    }
}