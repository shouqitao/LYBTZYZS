using System.ComponentModel;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Models.Contracts.Herbs
{
    /// <summary>
    /// 药材批量导入结果DTO - 继承自通用导入结果基类
    /// Epic #1962 Task 2.2: 批量导入返回结果
    /// OpenSpec: optimize-batch-operations - DTO继承规范化
    /// </summary>
    public class HerbBatchImportResultDto : ImportResultDto
    {
        /// <summary>失败详情列表（药材特定类型）</summary>
        [DisplayName("失败详情")]
        public List<HerbImportFailureDto> Failures { get; set; } = new();
    }

    /// <summary>
    /// 药材导入失败详情DTO
    /// Epic #1962 Task 2.2
    /// OpenSpec: optimize-batch-operations - DTO命名标准化
    /// </summary>
    public class HerbImportFailureDto
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
