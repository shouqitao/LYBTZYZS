using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 批量操作结果的基础 DTO — 用于 API 响应层。
    /// 注意：这是 API 批量操作（导入/删除）的响应结构，非领域操作结果。
    /// 领域操作结果请使用 <see cref="Result{T}"/>。
    /// </summary>
    public class OperationResultDto
    {
        /// <summary>操作是否成功</summary>
        [DisplayName("操作成功")]
        public bool IsSuccess { get; set; } = true;

        /// <summary>操作消息</summary>
        [DisplayName("操作消息")]
        public string Message { get; set; } = string.Empty;

        /// <summary>错误代码</summary>
        [DisplayName("错误代码")]
        public string? ErrorCode { get; set; }

        /// <summary>操作时间</summary>
        [DisplayName("操作时间")]
        public DateTime OperationTime { get; set; } = DateTime.UtcNow;
    }
}
