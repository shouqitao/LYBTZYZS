using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Models.Pharmacy
{
    /// <summary>
    /// 药房模型
    /// </summary>
    [Table("Pharmacy")]
    public class PharmacyModel
    {
        /// <summary>
        /// 药房单ID
        /// </summary>
        [Key]
        [Display(Name = "药房单ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 医疗案例ID
        /// </summary>
        [Display(Name = "医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>
        /// 处方ID
        /// </summary>
        [Display(Name = "处方ID")]
        public Guid PrescriptionId { get; set; }

        /// <summary>
        /// 患者ID
        /// </summary>
        [Display(Name = "患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>
        /// 配药状态
        /// </summary>
        [Display(Name = "配药状态")]
        public PharmacyStatus Status { get; set; }

        /// <summary>
        /// 配药师ID
        /// </summary>
        [Display(Name = "配药师ID")]
        public Guid? PharmacistId { get; set; }

        /// <summary>
        /// 配药时间
        /// </summary>
        [Display(Name = "配药时间")]
        public DateTime? DispensingTime { get; set; }

        /// <summary>
        /// 发药时间
        /// </summary>
        [Display(Name = "发药时间")]
        public DateTime? DispenseTime { get; set; }

        /// <summary>
        /// 领药人姓名
        /// </summary>
        [Display(Name = "领药人姓名")]
        [StringLength(50)]
        public string? ReceiverName { get; set; }

        /// <summary>
        /// 领药人电话
        /// </summary>
        [Display(Name = "领药人电话")]
        [StringLength(20)]
        public string? ReceiverPhone { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Display(Name = "备注")]
        [StringLength(500)]
        public string? Remark { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Display(Name = "创建时间")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [Display(Name = "更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        [Display(Name = "是否有效")]
        public bool IsActive { get; set; }

        /// <summary>
        /// 药材列表
        /// </summary>
        public virtual ICollection<PharmacyHerbModel> Herbs { get; set; } = new List<PharmacyHerbModel>();

        /// <summary>
        /// 医疗案例
        /// </summary>
        [ForeignKey(nameof(MedicalCaseId))]
        public virtual MedicalCase.MedicalCaseModel? MedicalCase { get; set; }

        /// <summary>
        /// 处方
        /// </summary>
        [ForeignKey(nameof(PrescriptionId))]
        public virtual Prescriptions.PrescriptionModel? Prescription { get; set; }
    }

    /// <summary>
    /// 药房状态枚举
    /// </summary>
    public enum PharmacyStatus
    {
        /// <summary>
        /// 待配药
        /// </summary>
        Pending = 0,

        /// <summary>
        /// 配药中
        /// </summary>
        Dispensing = 1,

        /// <summary>
        /// 已配药
        /// </summary>
        Dispensed = 2,

        /// <summary>
        /// 已发药
        /// </summary>
        Issued = 3,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = 4
    }
}