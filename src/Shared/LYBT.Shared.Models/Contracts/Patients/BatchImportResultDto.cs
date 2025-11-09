using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Patients
{
    /// <summary>
    /// 批量导入结果DTO
    /// FR-001: 批量导入患者数据的返回结果
    /// </summary>
    public class BatchImportResultDto
    {
        /// <summary>成功导入的患者数量</summary>
        [DisplayName("成功数量")]
        public int SuccessCount { get; set; }

        /// <summary>导入失败的患者数量</summary>
        [DisplayName("失败数量")]
        public int FailureCount { get; set; }

        /// <summary>跳过的患者数量（如重复数据）</summary>
        [DisplayName("跳过数量")]
        public int SkippedCount { get; set; }

        /// <summary>失败详情列表</summary>
        [DisplayName("失败详情")]
        public List<ImportFailureDetailDto> Failures { get; set; } = new();

        /// <summary>导入时间</summary>
        [DisplayName("导入时间")]
        public DateTime ImportTime { get; set; }

        /// <summary>总处理数量（成功+失败+跳过）</summary>
        [DisplayName("总处理数量")]
        public int TotalCount => SuccessCount + FailureCount + SkippedCount;

        /// <summary>成功率（百分比）</summary>
        [DisplayName("成功率")]
        public decimal SuccessRate => TotalCount > 0 ? (decimal)SuccessCount / TotalCount * 100 : 0;
    }
}
