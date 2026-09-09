using System;
using System.Security.Cryptography;

namespace WebAPI.Helpers
{
    public static class PasswordHelper
    {
        private const int SaltSize = 16; // 128 bits
        private const int KeySize = 32; // 256 bits
        private const int Iterations = 10000;
        private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;
    
        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithm, KeySize);
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }
    
        public static bool VerifyPassword(string? password, string? hashString)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashString)) return false;
            var parts = hashString.Split(':');
            if (parts.Length != 2) return false;
            try
            {
                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] expectedHash = Convert.FromBase64String(parts[1]);
                if (salt.Length != SaltSize || expectedHash.Length != KeySize) return false;
                byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithm, KeySize);
                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
