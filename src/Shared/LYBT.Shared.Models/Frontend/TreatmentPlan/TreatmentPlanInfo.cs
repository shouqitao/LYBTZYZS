using System;
using System.Collections.Generic;

namespace LYBT.Shared.Models.Frontend.TreatmentPlan
{
    /// <summary>
    /// 治疗方案前端模型
    /// </summary>
    public class TreatmentPlanInfo
    {
        /// <summary>
        /// ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 医疗案例ID
        /// </summary>
        public Guid MedicalCaseId { get; set; }

        /// <summary>
        /// 患者姓名
        /// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// 医生姓名
        /// </summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>
        /// 处方信息
        /// </summary>
        public PrescriptionInfo? Prescription { get; set; }

        /// <summary>
        /// 理疗项目列表
        /// </summary>
        public List<PhysiotherapyItemInfo> PhysiotherapyItems { get; set; } = new List<PhysiotherapyItemInfo>();

        /// <summary>
        /// 总金额
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; } = string.Empty;

        /// <summary>
        /// 是否有处方
        /// </summary>
        public bool HasPrescription { get; set; }

        /// <summary>
        /// 是否有理疗
        /// </summary>
        public bool HasPhysiotherapy { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// 处方前端模型
    /// </summary>
    public class PrescriptionInfo
    {
        /// <summary>
        /// ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 处方类型
        /// </summary>
        public string PrescriptionType { get; set; } = string.Empty;

        /// <summary>
        /// 剂数
        /// </summary>
        public int DosageCount { get; set; }

        /// <summary>
        /// 用法
        /// </summary>
        public string Usage { get; set; } = string.Empty;

        /// <summary>
        /// 频次
        /// </summary>
        public string Frequency { get; set; } = string.Empty;

        /// <summary>
        /// 处方项目列表
        /// </summary>
        public List<PrescriptionItemInfo> Items { get; set; } = new List<PrescriptionItemInfo>();

        /// <summary>
        /// 总金额
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; } = string.Empty;
    }

    /// <summary>
    /// 处方项目前端模型
    /// </summary>
    public class PrescriptionItemInfo
    {
        /// <summary>
        /// ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 药材ID
        /// </summary>
        public Guid HerbId { get; set; }

        /// <summary>
        /// 药材名称
        /// </summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>
        /// 数量
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// 单价
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 小计
        /// </summary>
        public decimal Subtotal { get; set; }

        /// <summary>
        /// 特殊说明
        /// </summary>
        public string SpecialInstructions { get; set; } = string.Empty;
    }

    /// <summary>
    /// 理疗项目前端模型
    /// </summary>
    public class PhysiotherapyItemInfo
    {
        /// <summary>
        /// ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 项目名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 项目类型
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 部位
        /// </summary>
        public string BodyPart { get; set; } = string.Empty;

        /// <summary>
        /// 时长（分钟）
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// 次数
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// 单价
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 小计
        /// </summary>
        public decimal Subtotal => Price * Quantity;

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; } = string.Empty;
    }
}