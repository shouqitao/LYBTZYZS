using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Core.Models.Pharmacy {
    /// <summary>
    /// 药房任务信息模型 - 前端专用，继承共享基础模型
    /// </summary>
    public class PharmacyInfo : BasePharmacyModel {
        /// <summary>
        /// 处方编号
        /// </summary>
        public string PrescriptionNo { get; set; } = string.Empty;

        /// <summary>
        /// 患者姓名
        /// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// 患者电话
        /// </summary>
        public string PatientPhone { get; set; } = string.Empty;

        /// <summary>
        /// 药材列表
        /// </summary>
        public List<PharmacyHerbInfo> Herbs { get; set; } = new();

        /// <summary>
        /// 状态名称
        /// </summary>
        public string StatusName => GetStatusName();

        /// <summary>
        /// 医生姓名
        /// </summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>
        /// 操作员姓名
        /// </summary>
        public string OperatorName { get; set; } = string.Empty;

        /// <summary>
        /// 药材数量
        /// </summary>
        public int HerbCount => Herbs?.Count ?? 0;

        /// <summary>
        /// 总价格
        /// </summary>
        public decimal TotalPrice => Herbs?.Sum(h => h.SubTotal) ?? 0;

        /// <summary>
        /// 是否选中（用于批量操作）
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// 是否可配药
        /// </summary>
        public bool CanDispense => Status == PharmacyStatus.Pending;

        /// <summary>
        /// 是否可完成
        /// </summary>
        public bool CanComplete => Status == PharmacyStatus.InProgress;

        /// <summary>
        /// 获取状态名称
        /// </summary>
        private string GetStatusName() {
            return Status switch {
                PharmacyStatus.Pending => "待配药",
                PharmacyStatus.InProgress => "配药中",
                PharmacyStatus.Completed => "已完成",
                PharmacyStatus.Cancelled => "已取消",
                _ => "未知"
            };
        }
    }

    /// <summary>
    /// 药房药材信息
    /// </summary>
    public class PharmacyHerbInfo {
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
        public string Unit { get; set; } = "g";

        /// <summary>
        /// 单价
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 小计
        /// </summary>
        public decimal SubTotal => Quantity * UnitPrice;

        /// <summary>
        /// 特殊煎法
        /// </summary>
        public string? ProcessingMethod { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
    }
}