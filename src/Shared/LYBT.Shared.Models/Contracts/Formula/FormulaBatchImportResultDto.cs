using System.ComponentModel;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Models.Contracts.Formula
{
    /// <summary>
    /// 批量导入结果DTO - 继承自通用导入结果基类
    /// </summary>
    public class FormulaBatchImportResultDto : ImportResultDto
    {
        /// <summary>导入批次号（兼容别名）</summary>
        [DisplayName("导入批次号")]
        public string ImportBatch => ImportBatchId;

        /// <summary>导入开始时间</summary>
        [DisplayName("导入开始时间")]
        public DateTime StartTime { get; set; }

        /// <summary>导入结束时间</summary>
        [DisplayName("导入结束时间")]
        public DateTime EndTime { get; set; }

        /// <summary>成功匹配的药材数量（自动匹配到药材库）</summary>
        [DisplayName("成功匹配药材数")]
        public int MatchedHerbsCount { get; set; }

        /// <summary>未匹配的药材数量（需要手动校验）</summary>
        [DisplayName("未匹配药材数")]
        public int UnmatchedHerbsCount { get; set; }

        /// <summary>成功的验方列表</summary>
        [DisplayName("成功的验方列表")]
        public List<FormulaDetailDto> SuccessfulFormulas { get; set; } = new();

        /// <summary>失败的记录</summary>
        [DisplayName("失败的记录")]
        public List<FormulaImportErrorDto> FailedItems { get; set; } = new();
    }
}
