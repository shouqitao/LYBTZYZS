using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 导入行错误DTO - 用于记录Excel导入时具体行的错误
    /// Issue #1165: 患者批量导入功能
    /// </summary>
    public class ImportRowErrorDto
    {
        /// <summary>Excel行号（从1开始，1为表头）</summary>
        [DisplayName("行号")]
        public int Row { get; set; }

        /// <summary>错误消息</summary>
        [DisplayName("错误消息")]
        public string Error { get; set; } = string.Empty;

        /// <summary>字段名（可选）</summary>
        [DisplayName("字段名")]
        public string? FieldName { get; set; }
    }
}
