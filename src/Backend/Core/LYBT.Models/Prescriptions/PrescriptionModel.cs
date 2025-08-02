using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Models.Prescriptions {

    /// <summary>
    /// 处方实体 - 中医处方管理，支持多验方组合、重复药材检测和价格计算
    /// </summary>
    public class PrescriptionModel {

        /// <summary>
        /// 处方唯一标识（主键）
        /// </summary>
        [Key]
        [DisplayName("处方ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 关联患者ID
        /// </summary>
        [Required]
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>
        /// 开方医生ID
        /// </summary>
        [Required]
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>
        /// 诊断信息
        /// </summary>
        [StringLength(256)]
        [DisplayName("诊断")]
        public string? Diagnosis { get; set; }

        /// <summary>
        /// 处方帖数（该处方的服用帖数，如：7帖、14帖）
        /// </summary>
        [DisplayName("处方帖数")]
        public int DosageCount { get; set; } = 7;

        /// <summary>
        /// 该单单价（单帖处方价格）
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("单帖价格")]
        public decimal SingleDosePrice { get; set; } = 0;

        /// <summary>
        /// 该单总价（处方总价格：单价 × 帖数）
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("处方总价")]
        public decimal TotalPrice { get; set; } = 0;

        /// <summary>
        /// 处方总重量统计
        /// </summary>
        [Column(TypeName = "decimal(10,3)")]
        [DisplayName("处方重量")]
        public decimal TotalWeight { get; set; } = 0;

        /// <summary>
        /// 医嘱（医生对该处方的用药指导和注意事项）
        /// </summary>
        [StringLength(1000)]
        [DisplayName("医嘱")]
        public string? Advice { get; set; }

        /// <summary>
        /// 验方来源记录（多个验方以斜杠"/"分隔，如：逍遥散/六味地黄丸/甘草汤）
        /// </summary>
        [StringLength(500)]
        [DisplayName("验方来源")]
        public string? FormulaSource { get; set; }

        /// <summary>
        /// 重复药材提醒信息
        /// </summary>
        [StringLength(1000)]
        [DisplayName("重复药材提醒")]
        public string? DuplicateWarning { get; set; }

        /// <summary>
        /// 缺药提醒信息（需要自备药材提醒）
        /// </summary>
        [StringLength(500)]
        [DisplayName("缺药提醒")]
        public string? MissingDrugWarning { get; set; }

        /// <summary>
        /// 处方状态
        /// </summary>
        [Required]
        [DisplayName("处方状态")]
        public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后修改时间
        /// </summary>
        [DisplayName("修改时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 备注信息
        /// </summary>
        [StringLength(256)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>
        /// 处方项目（药材明细）
        /// </summary>
        [DisplayName("处方项目")]
        public List<PrescriptionItemModel> Items { get; set; } = new();
    }
}