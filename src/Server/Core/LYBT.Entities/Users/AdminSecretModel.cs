using System.ComponentModel;

namespace LYBT.Entities.Users {

    /// <summary>
    /// Stores administrator password hashes separately
    /// to prevent tampering of the Users table.
    /// </summary>
    public class AdminSecretModel {

        /// <summary>主键</summary>
        [DisplayName("Primary key")]
        public Guid Id { get; set; }

        /// <summary>管理员用户名</summary>
        [DisplayName("Administrator username")]
        public string Username { get; set; } = string.Empty;

        /// <summary>密码哈希</summary>
        [DisplayName("Password hash")]
        public string PasswordHash { get; set; } = string.Empty;
    }
}
