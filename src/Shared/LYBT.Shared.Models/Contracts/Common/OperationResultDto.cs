using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 操作结果基础DTO - 用于所有操作结果的基类
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
        public DateTime OperationTime { get; set; } = DateTime.Now;
    }
}
