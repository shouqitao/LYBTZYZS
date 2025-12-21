using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Common;

namespace LYBT.Entities.Prescriptions
{

    /// <summary>
    /// 处方实体
    /// OpenSpec: simplify-medicalcase-dataflow
    /// 作为MedicalCase的可选组成部分（1:0..1关系）
    /// </summary>
    [Table("Prescriptions")]
    public class Prescription : BaseEntity
    {
        /// <summary>医疗案例ID（外键）</summary>
        [Required]
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>
        /// 处方编号（格式：RX-YYYYMMDD-NNNN）
        /// </summary>
        [StringLength(20)]
        [DisplayName("处方编号")]
        public string? PrescriptionNumber { get; set; }

        /// <summary>处方帖数</summary>
        [DisplayName("处方帖数")]
        public int DosageCount { get; set; } = 7;

        /// <summary>折扣（0-1之间，0.8表示8折）</summary>
        [Column(TypeName = "decimal(5,4)")]
        [DisplayName("折扣")]
        public decimal Discount { get; set; } = 1.0m;

        /// <summary>处方用法（如"每日一剂，水煎服"）</summary>
        [StringLength(500)]
        [DisplayName("处方用法")]
        public string? Usage { get; set; }

        /// <summary>医嘱</summary>
        [StringLength(500)]
        [DisplayName("医嘱")]
        public string? Advice { get; set; }

        /// <summary>
        /// 引用的验方名称列表，逗号分隔
        /// 用于记录从哪些验方导入了药材，例如："逍遥散,六味地黄丸"
        /// </summary>
        [StringLength(500)]
        [DisplayName("引用验方")]
        public string? ReferencedFormulas { get; set; }

        /// <summary>备注</summary>
        [StringLength(500)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        // Indication已删除，打印时从Consultation.TCMDiagnosis获取
        // FormulaSource已删除，与ReferencedFormulas功能重复

        // 打印版本管理字段

        /// <summary>当前打印版本号</summary>
        [DisplayName("打印版本号")]
        public int PrintVersion { get; set; } = 1;

        /// <summary>最后打印时间</summary>
        [DisplayName("最后打印时间")]
        public DateTime? LastPrintedAt { get; set; }

        /// <summary>打印次数</summary>
        [DisplayName("打印次数")]
        public int PrintCount { get; set; } = 0;

        /// <summary>是否已打印</summary>
        [DisplayName("是否已打印")]
        public bool IsPrinted { get; set; } = false;

        // 关联数据

        /// <summary>
        /// 处方项目（药材明细）
        /// </summary>
        [DisplayName("处方项目")]
        public virtual ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();

        // MedicalCase导航属性已移除 - OpenSpec: refactor-server-ddd-aggregates
        // Prescription作为MedicalCase聚合的内部实体，不应有反向导航
        // 需要MedicalCase信息时，通过MedicalCaseId查询或使用Query Service

        /// <summary>
        /// 打印日志记录
        /// </summary>
        public virtual ICollection<PrescriptionPrintLog> PrintLogs { get; set; } = new List<PrescriptionPrintLog>();
    }
}
