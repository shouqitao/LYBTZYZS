using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Auth
{

    /// <summary>
    /// 用户登出请求 - 前后端共享API契约
    /// Issue #1864 AUTH-008: 支持过期Token登出
    /// </summary>
    /// <remarks>
    /// 登出请求必须提供以下信息之一：
    /// - RefreshToken: 用于精确定位并撤销会话
    /// - UserName: 用于审计日志（当RefreshToken不可用时）
    /// </remarks>
    public class LogoutRequest
    {
        /// <summary>用户名（可选，用于审计日志）</summary>
        /// <remarks>Issue #1864: 改为可选，允许仅通过RefreshToken登出</remarks>
        [StringLength(32, ErrorMessage = "用户名长度不能超过32个字符")]
        [DisplayName("用户名")]
        public string? UserName { get; set; }

        /// <summary>刷新令牌（用于撤销会话）</summary>
        /// <remarks>Issue #1864: 推荐提供，用于精确撤销Token</remarks>
        [DisplayName("刷新令牌")]
        public string? RefreshToken { get; set; }

        /// <summary>设备ID（可选，用于撤销特定设备）</summary>
        [DisplayName("设备ID")]
        public string? DeviceId { get; set; }
    }
}
