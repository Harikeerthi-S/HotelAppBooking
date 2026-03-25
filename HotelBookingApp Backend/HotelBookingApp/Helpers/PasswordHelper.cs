using System.Security.Cryptography;
using System.Text;
using HotelBookingApp.Interfaces.IServices;

namespace HotelBookingApp.Helpers
{
    /// <summary>SHA-256 password hashing and verification.</summary>
    public class PasswordHelper : IPasswordService
    {
        public byte[] HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        public bool VerifyPassword(string password, byte[] storedHash)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;
            if (storedHash is null || storedHash.Length == 0) return false;

            var hash = HashPassword(password);
            return CryptographicOperations.FixedTimeEquals(hash, storedHash);
        }
    }
}
