using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.MedicalCases
{
    /// <summary>
    /// 医案打印日志实体
    /// 记录每次打印的详细信息，FK 关联到 MedicalCase（聚合根）
    /// </summary>
    [Table("MedicalCasePrintLogs")]
    public class MedicalCasePrintLog : BaseEntity
    {
        /// <summary>医案ID（外键）</summary>
        [Required]
        [DisplayName("医案ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>打印类型（处方/验方）</summary>
        [DisplayName("打印类型")]
        public PrintType PrintType { get; set; } = PrintType.Prescription;

        /// <summary>打印版本号</summary>
        [DisplayName("打印版本号")]
        public int PrintVersion { get; set; }

        /// <summary>打印时间</summary>
        [DisplayName("打印时间")]
        public DateTime PrintedAt { get; set; } = DateTime.UtcNow;

        /// <summary>打印人ID</summary>
        [DisplayName("打印人ID")]
        public Guid? PrintedBy { get; set; }

        /// <summary>打印人姓名</summary>
        [StringLength(50)]
        [DisplayName("打印人姓名")]
        public string? PrintedByName { get; set; }

        /// <summary>打印机名称或IP</summary>
        [StringLength(100)]
        [DisplayName("打印机")]
        public string? PrinterName { get; set; }

        /// <summary>打印状态（成功/失败）</summary>
        [DisplayName("打印状态")]
        public bool IsSuccess { get; set; } = true;

        /// <summary>打印错误信息（如果失败）</summary>
        [StringLength(500)]
        [DisplayName("错误信息")]
        public string? ErrorMessage { get; set; }

        /// <summary>备注</summary>
        [StringLength(200)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        // 导航属性

        /// <summary>关联的医案</summary>
        public virtual MedicalCase? MedicalCase { get; set; }
    }
}
