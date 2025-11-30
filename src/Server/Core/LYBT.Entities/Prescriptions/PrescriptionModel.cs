using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Common;

namespace LYBT.Entities.Prescriptions
{

    /// <summary>
    /// 处方实体 - UltraThink v2.0架构简化版
    /// 合并了原BasePrescription和PrescriptionModel
    /// 价格计算在DTO层处理，实体只存储基础数据和折扣信息
    /// 作为MedicalCase的可选组成部分（一对零或一关系）
    /// </summary>
    [Table("Prescriptions")]
    public class Prescription : BaseEntity
    {
        // Id字段继承自BaseEntity

        /// <summary>医疗案例ID（外键）</summary>
        [Required]
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>
        /// 处方编号（格式：RX-YYYYMMDD-NNNN，例如：RX-20251021-0001）
        /// 可为空以兼容旧数据，新建处方时自动生成
        /// Issue #1551: 处方自动编号功能
        /// </summary>
        [StringLength(20)]
        [DisplayName("处方编号")]
        public string? PrescriptionNumber { get; set; }

        // PatientId和UserId通过MedicalCase获取，保留以保持兼容性
        /// <summary>患者ID（冗余，通过MedicalCase获取）</summary>
        [DisplayName("患者ID")]
        public Guid? PatientId { get; set; }

        /// <summary>关联用户ID（医生，冗余，通过MedicalCase获取）</summary>
        [DisplayName("关联用户ID")]
        public Guid? UserId { get; set; }

        /// <summary>创建人ID（医生用户ID）</summary>
        // 审计字段（CreatedBy等）继承自BaseEntity

        /// <summary>主治（适应症/主要症状描述）</summary>
        [StringLength(500)]
        [DisplayName("主治")]
        public string? Indication { get; set; }

        /// <summary>处方帖数</summary>
        [DisplayName("处方帖数")]
        public int DosageCount { get; set; } = 7;

        /// <summary>折扣（0-1之间，0.8表示8折）</summary>
        [Column(TypeName = "decimal(5,4)")]
        [DisplayName("折扣")]
        public decimal Discount { get; set; } = 1.0m;

        /// <summary>医嘱</summary>
        [StringLength(500)]
        [DisplayName("医嘱")]
        public string? Advice { get; set; }

        /// <summary>验方来源（自动填写：调用验方时自动根据验方名称填写，多个验方用逗号分隔）</summary>
        [StringLength(200)]
        [DisplayName("验方来源")]
        public string? FormulaSource { get; set; }

        /// <summary>
        /// 引用的验方名称列表，逗号分隔 (Issue #1365 ENTRY-7)
        /// 用于记录从哪些验方导入了药材，例如："逍遥散,六味地黄丸"
        /// </summary>
        [StringLength(500)]
        [DisplayName("引用验方")]
        public string? ReferencedFormulas { get; set; }

        /// <summary>备注</summary>
        [StringLength(500)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

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

        // 导航属性

        /// <summary>
        /// 所属医疗案例
        /// </summary>
        public virtual MedicalCases.MedicalCase? MedicalCase { get; set; }

        /// <summary>
        /// 打印日志记录
        /// </summary>
        public virtual ICollection<PrescriptionPrintLog> PrintLogs { get; set; } = new List<PrescriptionPrintLog>();
    }
}
