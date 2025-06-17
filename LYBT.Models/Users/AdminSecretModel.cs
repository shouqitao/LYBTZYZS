using System;

namespace LYBT.Module.Users.Models {
    /// <summary>
    /// Stores administrator password hashes separately
    /// to prevent tampering of the Users table.
    /// </summary>
    public class AdminSecretModel {
        /// <summary>Primary key</summary>
        public Guid Id { get; set; }

        /// <summary>Administrator username</summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>Password hash</summary>
        public string PasswordHash { get; set; } = string.Empty;
    }
}
