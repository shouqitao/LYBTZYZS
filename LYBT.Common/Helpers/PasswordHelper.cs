using Microsoft.AspNetCore.Identity;

using System.ComponentModel;

namespace LYBT.Common.Helpers {

    /// <summary>
    /// Provides password hashing utilities using ASP.NET Core Identity.
    /// </summary>
    [Description("密码工具类")]
    public static class PasswordHelper {
        private static readonly PasswordHasher<object> _hasher = new();

        /// <summary>
        /// Hash a plain text password.
        /// </summary>
        public static string Hash(string password) {
            return _hasher.HashPassword(null, password);
        }

        /// <summary>
        /// Verify a password against the stored hash.
        /// </summary>
        public static bool Verify(string hash, string password) {
            var result = _hasher.VerifyHashedPassword(null, hash, password);
            return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}