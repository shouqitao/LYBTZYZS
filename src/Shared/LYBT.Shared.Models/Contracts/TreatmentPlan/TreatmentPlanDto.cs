using LYBT.Shared.Models.Contracts.Prescriptions;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.TreatmentPlan
{
    /// <summary>
    /// 治疗方案DTO
    /// </summary>
    public class TreatmentPlanDto
    {
        /// <summary>治疗方案ID</summary>
        [DisplayName("治疗方案ID")]
        public Guid Id { get; set; }

        /// <summary>看诊ID</summary>
        [DisplayName("看诊ID")]
        public Guid ConsultationId { get; set; }

        /// <summary>处方信息</summary>
        [DisplayName("处方信息")]
        public PrescriptionDto? Prescription { get; set; }

        /// <summary>理疗项目列表</summary>
        [DisplayName("理疗项目列表")]
        public List<PhysiotherapyItemDto> PhysiotherapyItems { get; set; } = new();

        /// <summary>处方费用</summary>
        [DisplayName("处方费用")]
        public decimal PrescriptionAmount { get; set; }

        /// <summary>理疗费用</summary>
        [DisplayName("理疗费用")]
        public decimal PhysiotherapyAmount { get; set; }

        /// <summary>总费用</summary>
        [DisplayName("总费用")]
        public decimal TotalAmount => PrescriptionAmount + PhysiotherapyAmount;

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 理疗项目DTO
    /// </summary>
    public class PhysiotherapyItemDto
    {
        /// <summary>项目ID</summary>
        [DisplayName("项目ID")]
        public Guid Id { get; set; }

        /// <summary>项目名称</summary>
        [DisplayName("项目名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>项目类型</summary>
        [DisplayName("项目类型")]
        public string Type { get; set; } = string.Empty; // 针灸、推拿、拔罐、艾灸等

        /// <summary>治疗部位</summary>
        [DisplayName("治疗部位")]
        public string? TreatmentArea { get; set; }

        /// <summary>次数</summary>
        [DisplayName("次数")]
        public int Count { get; set; } = 1;

        /// <summary>单价</summary>
        [DisplayName("单价")]
        public decimal UnitPrice { get; set; }

        /// <summary>小计</summary>
        [DisplayName("小计")]
        public decimal SubTotal => Count * UnitPrice;

        /// <summary>执行状态</summary>
        [DisplayName("执行状态")]
        public string Status { get; set; } = "待执行";

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}