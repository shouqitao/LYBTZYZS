using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Models.TreatmentPlan
{
    /// <summary>
    /// 治疗方案实体
    /// </summary>
    [Table("TreatmentPlans")]
    public class TreatmentPlanModel
    {
        /// <summary>治疗方案ID</summary>
        [Key]
        [DisplayName("治疗方案ID")]
        public Guid Id { get; set; }

        /// <summary>看诊ID</summary>
        [Required]
        [DisplayName("看诊ID")]
        public Guid ConsultationId { get; set; }

        /// <summary>处方信息</summary>
        [DisplayName("处方信息")]
        public PrescriptionModel? Prescription { get; set; }

        /// <summary>理疗项目列表</summary>
        [DisplayName("理疗项目列表")]
        public List<PhysiotherapyItemModel> PhysiotherapyItems { get; set; } = new();

        /// <summary>处方费用</summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("处方费用")]
        public decimal PrescriptionAmount { get; set; }

        /// <summary>理疗费用</summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("理疗费用")]
        public decimal PhysiotherapyAmount { get; set; }

        /// <summary>总费用</summary>
        [NotMapped]
        [DisplayName("总费用")]
        public decimal TotalAmount => PrescriptionAmount + PhysiotherapyAmount;

        /// <summary>备注</summary>
        [StringLength(500)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>是否有效</summary>
        [DisplayName("是否有效")]
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// 处方实体（作为治疗方案的一部分）
    /// </summary>
    [Owned]
    public class PrescriptionModel
    {
        /// <summary>处方药材列表</summary>
        [DisplayName("处方药材列表")]
        public List<PrescriptionHerbModel> Herbs { get; set; } = new();

        /// <summary>付数</summary>
        [DisplayName("付数")]
        public int DosageCount { get; set; } = 1;

        /// <summary>用法说明</summary>
        [StringLength(200)]
        [DisplayName("用法说明")]
        public string? Instructions { get; set; }

        /// <summary>特殊煎法</summary>
        [StringLength(100)]
        [DisplayName("特殊煎法")]
        public string? SpecialInstructions { get; set; }

        /// <summary>处方总价</summary>
        [NotMapped]
        [DisplayName("处方总价")]
        public decimal TotalPrice => Herbs?.Sum(x => x.SubTotal * DosageCount) ?? 0;
    }

    /// <summary>
    /// 处方药材实体
    /// </summary>
    [Owned]
    public class PrescriptionHerbModel
    {
        /// <summary>药材ID</summary>
        [DisplayName("药材ID")]
        public Guid HerbId { get; set; }

        /// <summary>药材名称</summary>
        [StringLength(100)]
        [DisplayName("药材名称")]
        public string HerbName { get; set; } = string.Empty;

        /// <summary>数量</summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("数量")]
        public decimal Quantity { get; set; }

        /// <summary>单位</summary>
        [StringLength(10)]
        [DisplayName("单位")]
        public string Unit { get; set; } = "g";

        /// <summary>单价</summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("单价")]
        public decimal UnitPrice { get; set; }

        /// <summary>小计</summary>
        [NotMapped]
        [DisplayName("小计")]
        public decimal SubTotal => Quantity * UnitPrice;

        /// <summary>特殊用法</summary>
        [StringLength(50)]
        [DisplayName("特殊用法")]
        public string? SpecialUsage { get; set; }

        /// <summary>排序</summary>
        [DisplayName("排序")]
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// 理疗项目实体
    /// </summary>
    [Owned]
    public class PhysiotherapyItemModel
    {
        /// <summary>项目名称</summary>
        [StringLength(50)]
        [DisplayName("项目名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>项目类型</summary>
        [StringLength(20)]
        [DisplayName("项目类型")]
        public string Type { get; set; } = string.Empty;

        /// <summary>治疗部位</summary>
        [StringLength(100)]
        [DisplayName("治疗部位")]
        public string? TreatmentArea { get; set; }

        /// <summary>次数</summary>
        [DisplayName("次数")]
        public int Count { get; set; } = 1;

        /// <summary>单价</summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("单价")]
        public decimal UnitPrice { get; set; }

        /// <summary>小计</summary>
        [NotMapped]
        [DisplayName("小计")]
        public decimal SubTotal => Count * UnitPrice;

        /// <summary>执行状态</summary>
        [StringLength(20)]
        [DisplayName("执行状态")]
        public string Status { get; set; } = "待执行";

        /// <summary>备注</summary>
        [StringLength(200)]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}