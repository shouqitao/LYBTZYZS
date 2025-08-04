using System;
using System.Collections.Generic;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;

namespace LYBT.WPF.Client.Core.Models.Billing
{
    /// <summary>
    /// 账单信息模型 - 前端专用，继承共享基础模型
    /// </summary>
    public class BillingInfo : BaseBillingModel
    {
        /// <summary>患者姓名（前端扩展字段）</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>患者性别（前端扩展字段）</summary>
        public string PatientGender { get; set; } = string.Empty;

        /// <summary>患者年龄（前端扩展字段）</summary>
        public int PatientAge { get; set; }

        /// <summary>患者电话（前端扩展字段）</summary>
        public string? PatientPhone { get; set; }

        /// <summary>医生姓名（前端扩展字段）</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>收费员姓名（前端扩展字段）</summary>
        public string? CashierName { get; set; }

        /// <summary>账单类型（前端扩展字段）</summary>
        public string BillingType { get; set; } = string.Empty;

        /// <summary>账单明细项目（前端扩展字段）</summary>
        public List<BillingItemInfo> Items { get; set; } = new();

        /// <summary>未付金额（计算属性）</summary>
        public decimal UnpaidAmount => PayableAmount - PaidAmount;

        /// <summary>是否已付清（计算属性）</summary>
        public bool IsPaidInFull => PaidAmount >= PayableAmount;

        /// <summary>状态显示文本（计算属性）</summary>
        public string StatusText => Status.GetDescription();

        /// <summary>状态颜色（计算属性）</summary>
        public string StatusColor => Status switch
        {
            BillingStatus.Pending => "#FFA500",        // 橙色
            BillingStatus.Paid => "#4CAF50",           // 绿色
            BillingStatus.PartiallyPaid => "#2196F3",  // 蓝色
            BillingStatus.Refunded => "#9C27B0",       // 紫色
            BillingStatus.Cancelled => "#F44336",      // 红色
            _ => "#757575"                             // 灰色
        };

        /// <summary>是否可以收费（计算属性）</summary>
        public bool CanCharge => Status == BillingStatus.Pending || Status == BillingStatus.PartiallyPaid;

        /// <summary>是否可以退费（计算属性）</summary>
        public bool CanRefund => Status == BillingStatus.Paid && !IsDeleted;

        /// <summary>是否可以打印（计算属性）</summary>
        public bool CanPrint => Status == BillingStatus.Paid || Status == BillingStatus.PartiallyPaid;

        /// <summary>是否可以取消（计算属性）</summary>
        public bool CanCancel => Status == BillingStatus.Pending && !IsDeleted;

        /// <summary>是否可以修改（计算属性）</summary>
        public bool CanEdit => Status == BillingStatus.Pending && !IsDeleted;

        /// <summary>账单类型显示文本（计算属性）</summary>
        public string BillingTypeText => BillingType switch
        {
            "Registration" => "挂号费",
            "Consultation" => "诊疗费",
            "Prescription" => "药品费",
            "Treatment" => "理疗费",
            "Examination" => "检查费",
            "Other" => "其他费用",
            _ => BillingType
        };
    }

    /// <summary>
    /// 账单明细项目信息 - 前端专用
    /// </summary>
    public class BillingItemInfo
    {
        /// <summary>明细项ID</summary>
        public Guid ItemId { get; set; } = Guid.NewGuid();

        /// <summary>项目类型（药品、诊疗、理疗等）</summary>
        public string ItemType { get; set; } = string.Empty;

        /// <summary>项目编码</summary>
        public string ItemCode { get; set; } = string.Empty;

        /// <summary>项目名称</summary>
        public string ItemName { get; set; } = string.Empty;

        /// <summary>规格</summary>
        public string? Specification { get; set; }

        /// <summary>单位</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>单价</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>数量</summary>
        public decimal Quantity { get; set; }

        /// <summary>折扣率（0-1）</summary>
        public decimal DiscountRate { get; set; } = 1;

        /// <summary>折扣金额</summary>
        public decimal DiscountAmount => UnitPrice * Quantity * (1 - DiscountRate);

        /// <summary>小计（计算属性）</summary>
        public decimal SubTotal => UnitPrice * Quantity * DiscountRate;

        /// <summary>备注</summary>
        public string? Remark { get; set; }

        /// <summary>关联ID（如药品ID、理疗项目ID等）</summary>
        public Guid? RelatedId { get; set; }

        /// <summary>项目类型显示文本（计算属性）</summary>
        public string ItemTypeText => ItemType switch
        {
            "Medicine" => "药品",
            "Treatment" => "理疗",
            "Consultation" => "诊疗",
            "Examination" => "检查",
            "Material" => "材料",
            "Other" => "其他",
            _ => ItemType
        };
    }
}