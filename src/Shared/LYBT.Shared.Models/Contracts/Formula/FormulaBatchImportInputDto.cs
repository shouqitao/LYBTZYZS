using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Formula
{
    /// <summary>
    /// 验方导入请求DTO (Issue #1758)
    /// 用于API接收已解析的验方数据（Excel解析在Client端完成）
    /// </summary>
    public class FormulaBatchImportInputDto
    {
        /// <summary>已解析的验方列表</summary>
        [Required(ErrorMessage = "验方列表不能为空")]
        [DisplayName("验方列表")]
        public List<FormulaImportItemDto> Formulas { get; set; } = new();

        /// <summary>
        /// 重复处理策略（Skip/Update/Error），默认 Skip。
        /// 重复键 = 验方名称（对齐药材/患者批量导入的 DuplicateStrategy 约定）。
        /// </summary>
        [DisplayName("重复处理策略")]
        public DuplicateStrategy Strategy { get; set; } = DuplicateStrategy.Skip;

        /// <summary>原始文件名（可选）</summary>
        [DisplayName("文件名")]
        public string? FileName { get; set; }
    }
}
