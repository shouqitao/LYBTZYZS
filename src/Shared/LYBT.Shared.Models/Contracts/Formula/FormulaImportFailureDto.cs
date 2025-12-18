using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Formula
{
    /// <summary>
    /// 方剂导入失败详情DTO
    /// OpenSpec: optimize-batch-operations - DTO命名标准化
    /// </summary>
    public class FormulaImportFailureDto
    {

        [DisplayName("行号")]
        public int RowIndex { get; set; }

        [DisplayName("验方名称")]
        public string FormulaName { get; set; } = string.Empty;

        [DisplayName("错误原因")]
        public string ErrorMessage { get; set; } = string.Empty;

        [DisplayName("错误详情")]
        public string? ErrorDetails { get; set; }

        [DisplayName("原始数据")]
        public string? OriginalData { get; set; }
    }
}
