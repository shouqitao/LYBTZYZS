namespace LYBT.Shared.Models.Contracts.Herbs
{
    /// <summary>
    /// 药材批量导入结果DTO
    /// Epic #1962 Task 2.2: 批量导入返回结果
    /// </summary>
    public class HerbBatchImportResultDto
    {
        /// <summary>成功导入数量</summary>
        public int SuccessCount { get; set; }

        /// <summary>失败数量</summary>
        public int FailureCount { get; set; }

        /// <summary>跳过数量（重复且策略为Skip）</summary>
        public int SkippedCount { get; set; }

        /// <summary>失败详情列表</summary>
        public List<HerbImportFailureDetailDto> Failures { get; set; } = new();

        /// <summary>导入时间</summary>
        public DateTime ImportTime { get; set; }

        /// <summary>总数量</summary>
        public int TotalCount => SuccessCount + FailureCount + SkippedCount;

        /// <summary>成功率</summary>
        public decimal SuccessRate => TotalCount > 0 ? (decimal)SuccessCount / TotalCount * 100 : 0;
    }

    /// <summary>
    /// 药材导入失败详情DTO
    /// Epic #1962 Task 2.2
    /// </summary>
    public class HerbImportFailureDetailDto
    {
        /// <summary>行号（Excel行号）</summary>
        public int RowNumber { get; set; }

        /// <summary>药材名称</summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>失败原因</summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>详细错误信息</summary>
        public List<string> ErrorDetails { get; set; } = new();
    }
}
