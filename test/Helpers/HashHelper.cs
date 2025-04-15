using System;
using System.Security.Cryptography;
using System.Text;

namespace test.Helpers
{
    public static class HashHelper
    {
        public static string GenerateSalt(int size = 32)
        {
            var saltBytes = new byte[size];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        public static string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var combinedBytes = Encoding.UTF8.GetBytes(password + salt);
                var hashBytes = sha256.ComputeHash(combinedBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            var computedHash = HashPassword(password, storedSalt);
            return storedHash == computedHash;
        }
    }
}