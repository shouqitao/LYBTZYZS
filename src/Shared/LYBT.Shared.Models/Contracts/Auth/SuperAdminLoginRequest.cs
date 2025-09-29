using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Auth
{
    /// <summary>
    /// 超级管理员登录请求
    /// 安全设计：不包含用户名字段，用户名从配置文件读取
    /// 防止SQL注入攻击时暴露超级管理员用户名
    /// </summary>
    public class SuperAdminLoginRequest
    {
        /// <summary>
        /// 超级管理员密码
        /// </summary>
        [Required(ErrorMessage = "密码不能为空")]
        [DisplayName("密码")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 登录IP地址（可选，用于审计）
        /// </summary>
        [DisplayName("IP地址")]
        public string? IpAddress { get; set; }

        /// <summary>
        /// 登录时间戳（可选，用于审计）
        /// </summary>
        [DisplayName("时间戳")]
        public DateTime? Timestamp { get; set; } = DateTime.UtcNow;
    }
}