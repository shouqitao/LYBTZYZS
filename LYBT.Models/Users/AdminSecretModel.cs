using System.ComponentModel;
namespace LYBT.Module.Users.Models {

    /// <summary>
    /// Stores administrator password hashes separately
    /// to prevent tampering of the Users table.
    /// </summary>
    public class AdminSecretModel {

        /// <summary>Primary key</summary>
        [DisplayName("Primary key")]
        public Guid Id { get; set; }

        /// <summary>Administrator username</summary>
        [DisplayName("Administrator username")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>Password hash</summary>
        [DisplayName("Password hash")]
        public string PasswordHash { get; set; } = string.Empty;
    }
}