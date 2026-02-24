using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 打印完成回写请求
    /// T2-X8-04~08: 打印后更新 IsPrinted/PrintCount/LastPrintedAt/PrintVersion
    /// </summary>
    public class PrintCompletedRequest
    {
        /// <summary>打印类型（处方/验方）</summary>
        [Required]
        public PrintType PrintType { get; set; } = PrintType.Prescription;

        /// <summary>打印机名称（可选）</summary>
        [StringLength(100)]
        public string? PrinterName { get; set; }
    }
}
