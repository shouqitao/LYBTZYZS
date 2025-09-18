using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 日志创建DTO - P4-Fix临时创建用于测试编译通过
    /// 最小化字段定义，仅包含测试所需属性
    /// </summary>
    public class LogCreateDto
    {
        /// <summary>用户ID</summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        public Guid UserId { get; set; }

        /// <summary>操作类型</summary>
        [Required(ErrorMessage = "操作类型不能为空")]
        public ActionType ActionType { get; set; }

        /// <summary>日志消息</summary>
        [Required(ErrorMessage = "日志消息不能为空")]
        [StringLength(500, ErrorMessage = "日志消息长度不能超过500个字符")]
        [DisplayName("日志消息")]
        public string Message { get; set; } = string.Empty;

        /// <summary>详细信息</summary>
        [StringLength(2000, ErrorMessage = "详细信息长度不能超过2000个字符")]
        [DisplayName("详细信息")]
        public string? Details { get; set; }
    }
}