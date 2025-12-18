using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Patients
{
    /// <summary>
    /// 患者导入失败详情DTO
    /// BR-002: 失败恢复机制的核心数据结构
    /// 包含原始行号、失败原因、修复建议和数据快照
    /// OpenSpec: optimize-batch-operations - DTO命名标准化
    /// </summary>
    public class PatientImportFailureDto
    {
        /// <summary>Excel原始行号（从2开始，第1行为标题）</summary>
        [DisplayName("行号")]
        public int OriginalRowNumber { get; set; }

        /// <summary>失败原因（验证错误消息）</summary>
        [DisplayName("失败原因")]
        public string FailureReason { get; set; } = string.Empty;

        /// <summary>失败字段名称</summary>
        [DisplayName("失败字段")]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>原始值（用户输入的值）</summary>
        [DisplayName("原始值")]
        public string OriginalValue { get; set; } = string.Empty;

        /// <summary>修复建议（帮助用户快速修正数据）</summary>
        [DisplayName("修复建议")]
        public string SuggestedFix { get; set; } = string.Empty;

        /// <summary>数据快照（完整的患者输入数据）</summary>
        [DisplayName("数据快照")]
        public PatientInputDto DataSnapshot { get; set; } = new();
    }
}
