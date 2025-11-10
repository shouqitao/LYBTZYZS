namespace LYBT.Shared.Models.Contracts.Users
{
    /// <summary>
    /// 用户批量导入结果DTO
    /// Issue #2003 Task 2.10: Desktop主导批量导入模式
    /// </summary>
    public class UserBatchImportResultDto
    {
        /// <summary>成功导入数量</summary>
        public int SuccessCount { get; set; }

        /// <summary>失败数量</summary>
        public int FailureCount { get; set; }

        /// <summary>跳过数量（重复且策略为Skip）</summary>
        public int SkippedCount { get; set; }

        /// <summary>失败详情列表</summary>
        public List<UserImportFailureDetailDto> Failures { get; set; } = new();

        /// <summary>导入时间</summary>
        public DateTime ImportTime { get; set; }

        /// <summary>总数量</summary>
        public int TotalCount => SuccessCount + FailureCount + SkippedCount;

        /// <summary>成功率</summary>
        public decimal SuccessRate => TotalCount > 0 ? (decimal)SuccessCount / TotalCount * 100 : 0;
    }

    /// <summary>
    /// 用户导入失败详情DTO
    /// Issue #2003 Task 2.10
    /// </summary>
    public class UserImportFailureDetailDto
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
