using LYBT.Common.Enums.Diagnostics;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Prescriptions.Models {

    /// <summary>
    /// 处方主表实体
    /// </summary>
    public class PrescriptionModel {

        [Key]
        [DisplayName("Id")]
        public Guid Id { get; set; }

        [Required]
        [DisplayName("PatientId")]
        public Guid PatientId { get; set; }

        [Required]
        [DisplayName("DoctorId")]
        public Guid DoctorId { get; set; }

        [Required]
        [DisplayName("CreateTime")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        [StringLength(256)]
        [DisplayName("Diagnosis")]
        public string? Diagnosis { get; set; }

        [StringLength(256)]
        [DisplayName("Remark")]
        public string? Remark { get; set; }

        [Required]
        [DisplayName("Status")]
        public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;

        #region 根据系统总结文档新增字段

        /// <summary>
        /// 处方帖数（如：7帖、14帖）
        /// </summary>
        [Required]
        [DisplayName("处方帖数")]
        public int DosageCount { get; set; } = 7;

        /// <summary>
        /// 处方总重量（克）
        /// </summary>
        [DisplayName("处方总重量")]
        public decimal TotalWeight { get; set; }

        /// <summary>
        /// 该单单价（单帖处方价格）
        /// </summary>
        [DisplayName("单帖价格")]
        public decimal SingleDosePrice { get; set; }

        /// <summary>
        /// 该单总价（处方总价格：单价 × 帖数）
        /// </summary>
        [DisplayName("处方总价")]
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// 医嘱（医生对该处方的用药指导和注意事项）
        /// </summary>
        [StringLength(1000)]
        [DisplayName("医嘱")]
        public string? MedicalAdvice { get; set; }

        /// <summary>
        /// 验方来源记录（多个验方以斜杠"/"分隔，如：逍遥散/六味地黄丸/甘草汤）
        /// </summary>
        [StringLength(500)]
        [DisplayName("验方来源")]
        public string? FormulaTemplateNames { get; set; }

        /// <summary>
        /// 缺药提醒标记（需要自备药材的情况）
        /// </summary>
        [DisplayName("缺药提醒")]
        public DrugAvailabilityStatus DrugAvailability { get; set; } = DrugAvailabilityStatus.FullyAvailable;

        /// <summary>
        /// 缺药药材列表（需要自备的药材名称，用逗号分隔）
        /// </summary>
        [StringLength(500)]
        [DisplayName("缺药药材")]
        public string? MissingHerbs { get; set; }

        /// <summary>
        /// 重复药材提醒信息
        /// </summary>
        [StringLength(1000)]
        [DisplayName("重复药材提醒")]
        public string? DuplicateHerbWarning { get; set; }

        #endregion

        [DisplayName("Items")]
        public List<PrescriptionItemModel> Items { get; set; } = new();

        /// <summary>
        /// 处方修改历史记录
        /// </summary>
        [DisplayName("修改历史")]
        public List<PrescriptionModificationHistory> ModificationHistory { get; set; } = new();
    }

    /// <summary>
    /// 药材供应状态枚举
    /// </summary>
    public enum DrugAvailabilityStatus {
        /// <summary>
        /// 完全库存
        /// </summary>
        [Description("完全库存")]
        FullyAvailable = 0,

        /// <summary>
        /// 部分缺药
        /// </summary>
        [Description("部分药材需自备")]
        PartiallyMissing = 1,

        /// <summary>
        /// 完全缺药
        /// </summary>
        [Description("全部药材需自备")]
        FullyMissing = 2
    }

    /// <summary>
    /// 处方修改历史记录
    /// </summary>
    public class PrescriptionModificationHistory {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// 处方ID
        /// </summary>
        [Required]
        public Guid PrescriptionId { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [Required]
        public DateTime ModificationTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 修改人ID
        /// </summary>
        [Required]
        public Guid ModifiedById { get; set; }

        /// <summary>
        /// 修改人姓名
        /// </summary>
        [StringLength(100)]
        [Required]
        public string ModifiedByName { get; set; } = string.Empty;

        /// <summary>
        /// 修改类型（如：添加药材、修改剂量、调整帖数等）
        /// </summary>
        [StringLength(100)]
        [Required]
        public string ModificationType { get; set; } = string.Empty;

        /// <summary>
        /// 修改描述
        /// </summary>
        [StringLength(500)]
        public string? ModificationDescription { get; set; }

        /// <summary>
        /// 修改前数据快照（JSON格式）
        /// </summary>
        [StringLength(2000)]
        public string? BeforeSnapshot { get; set; }

        /// <summary>
        /// 修改后数据快照（JSON格式）
        /// </summary>
        [StringLength(2000)]
        public string? AfterSnapshot { get; set; }
    }
}