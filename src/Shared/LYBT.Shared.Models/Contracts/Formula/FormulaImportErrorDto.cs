using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Formula
{
    /// <summary>
    /// 导入错误信息DTO
    /// </summary>
    public class FormulaImportErrorDto
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
