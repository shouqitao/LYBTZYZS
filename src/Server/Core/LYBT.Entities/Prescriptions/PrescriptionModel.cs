using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Entities.Prescriptions
{
    /// <summary>
    /// 处方实体 - UltraThink v2.0架构简化版
    /// 合并了原BasePrescription和PrescriptionModel
    /// 价格计算在DTO层处理，实体只存储基础数据和折扣信息
    /// </summary>
    [Table("Prescriptions")]
    public class Prescription
    {
        /// <summary>处方唯一标识</summary>
        [Key]
        [DisplayName("处方ID")]
        public Guid Id { get; set; }

        /// <summary>医疗案例ID</summary>
        [Required]
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者ID</summary>
        [Required]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>关联用户ID（医生）</summary>
        [Required]
        [DisplayName("关联用户ID")]
        public Guid UserId { get; set; }

        /// <summary>主治（适应症/主要症状描述）</summary>
        [StringLength(500)]
        [DisplayName("主治")]
        public string? Indication { get; set; }

        /// <summary>处方帖数</summary>
        [DisplayName("处方帖数")]
        public int DosageCount { get; set; } = 7;

        /// <summary>折扣（0-1之间，0.8表示8折）</summary>
        [Column(TypeName = "decimal(3,2)")]
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

        /// <summary>处方状态</summary>
        [DisplayName("处方状态")]
        public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;

        /// <summary>备注</summary>
        [StringLength(500)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        // 关联数据
        /// <summary>
        /// 处方项目（药材明细）
        /// </summary>
        [DisplayName("处方项目")]
        public List<PrescriptionItemModel> Items { get; set; } = new();
    }

}