using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Common;

namespace LYBT.Entities.Prescriptions
{
    /// <summary>
    /// 处方打印日志实体
    /// 记录每次处方打印的详细信息
    /// </summary>
    [Table("PrescriptionPrintLogs")]
    public class PrescriptionPrintLog : BaseEntity
    {
        // Id字段继承自BaseEntity

        /// <summary>处方ID（外键）</summary>
        [Required]
        [DisplayName("处方ID")]
        public Guid PrescriptionId { get; set; }

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

        /// <summary>
        /// 关联的处方
        /// </summary>
        public virtual Prescription? Prescription { get; set; }
    }
}
