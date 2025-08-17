using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Core.Models.Prescriptions
{
    /// <summary>
    /// 处方信息模型 - 前端专用，继承共享基础模型
    /// </summary>
    public class PrescriptionInfo : BasePrescription
    {
        /// <summary>医疗案例ID</summary>
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者姓名（前端显示字段）</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生姓名（前端显示字段）</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>处方编号（前端业务字段）</summary>
        public string PrescriptionNumber { get; set; } = string.Empty;

        /// <summary>处方编号别名（兼容性）</summary>
        public string PrescriptionNo 
        { 
            get => PrescriptionNumber; 
            set => PrescriptionNumber = value; 
        }

        /// <summary>医嘱别名（兼容性）</summary>
        public string? MedicalAdvice 
        { 
            get => Advice; 
            set => Advice = value; 
        }

        /// <summary>折扣率（1.0表示无折扣）</summary>
        public decimal Discount { get; set; } = 1.0m;

        /// <summary>处方项目（药材明细）</summary>
        public List<PrescriptionItemInfo> Items { get; set; } = new();

        /// <summary>是否已支付（前端业务字段）</summary>
        public bool IsPaid { get; set; }

        /// <summary>是否已发药（前端业务字段）</summary>
        public bool IsDispensed { get; set; }

        /// <summary>是否选中（用于批量操作）</summary>
        public bool IsSelected { get; set; }

        /// <summary>总金额</summary>
        public decimal TotalAmount => Items?.Sum(x => x.Subtotal) ?? 0;

        /// <summary>用法说明</summary>
        public string? Usage { get; set; }

        /// <summary>剂型</summary>
        public string? DosageForm { get; set; }

        /// <summary>状态名称（前端显示字段）</summary>
        public string StatusName => GetStatusName();

        /// <summary>状态颜色（前端显示字段）</summary>
        public string StatusColor => GetStatusColor();

        /// <summary>中药材数量（前端显示字段）</summary>
        public int HerbCount { get; set; }

        /// <summary>患者信息（前端显示字段，如：男 35岁）</summary>
        public string PatientInfo { get; set; } = string.Empty;

        /// <summary>是否可编辑（前端权限字段）</summary>
        public bool CanEdit { get; set; }

        /// <summary>是否可作废（前端权限字段）</summary>
        public bool CanVoid { get; set; }

        private string GetStatusName()
        {
            return Status switch
            {
                PrescriptionStatus.Draft => "草稿",
                PrescriptionStatus.Completed => "已完成",
                _ => "未知状态"
            };
        }

        private string GetStatusColor()
        {
            return Status switch
            {
                PrescriptionStatus.Draft => "#9E9E9E",      // 灰色
                PrescriptionStatus.Completed => "#4CAF50",   // 绿色
                _ => "#757575"
            };
        }
    }
}