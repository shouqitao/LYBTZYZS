using System.ComponentModel;

namespace LYBT.Entities.Users
{
    /// <summary>
    /// 存储超级管理员密码哈希，与普通用户分离存储以增强安全性
    /// 设计理念：不存储用户名，通过配置文件指定，防止SQL注入后暴露管理员账户名
    /// </summary>
    public class AdminSecretModel
    {
        /// <summary>主键</summary>
        [DisplayName("Primary key")]
        public Guid Id { get; set; }

        /// <summary>密码哈希</summary>
        [DisplayName("Password hash")]
        public string PasswordHash { get; set; } = string.Empty;
    }
}
