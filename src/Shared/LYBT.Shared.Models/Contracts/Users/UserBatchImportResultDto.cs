using System.ComponentModel;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Models.Contracts.Users
{
    /// <summary>
    /// 用户批量导入结果DTO - 继承自通用导入结果基类
    /// Issue #2003 Task 2.10: Desktop主导批量导入模式
    /// OpenSpec: optimize-batch-operations - DTO继承规范化
    /// </summary>
    public class UserBatchImportResultDto : ImportResultDto
    {
        /// <summary>失败详情列表（用户特定类型）</summary>
        [DisplayName("失败详情")]
        public List<UserImportFailureDto> Failures { get; set; } = new();
    }

    /// <summary>
    /// 用户导入失败详情DTO
    /// Issue #2003 Task 2.10
    /// OpenSpec: optimize-batch-operations - DTO命名标准化
    /// </summary>
    public class UserImportFailureDto
    {
        /// <summary>原始行号（Excel行号，从1开始）</summary>
        public int OriginalRowNumber { get; set; }

        /// <summary>用户名</summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>失败原因</summary>
        public string FailureReason { get; set; } = string.Empty;

        /// <summary>详细错误信息</summary>
        public List<string> ErrorDetails { get; set; } = new();
    }
}
