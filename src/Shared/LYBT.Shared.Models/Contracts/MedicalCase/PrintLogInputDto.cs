using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 打印日志输入DTO
    /// T4-S5-01: 记录打印成功/失败日志
    /// </summary>
    public class PrintLogInputDto
    {
        /// <summary>打印类型（处方/验方）</summary>
        [Required]
        public PrintType PrintType { get; set; } = PrintType.Prescription;

        /// <summary>是否打印成功</summary>
        public bool IsSuccess { get; set; }

        /// <summary>打印机名称（可选）</summary>
        [StringLength(100)]
        public string? PrinterName { get; set; }

        /// <summary>错误信息（失败时填写）</summary>
        [StringLength(500)]
        public string? ErrorMessage { get; set; }
    }
}
