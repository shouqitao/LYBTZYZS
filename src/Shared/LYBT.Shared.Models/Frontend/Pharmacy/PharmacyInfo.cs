using System;
using System.Collections.Generic;

namespace LYBT.Shared.Models.Frontend.Pharmacy
{
    /// <summary>
    /// 药房前端模型
    /// </summary>
    public class PharmacyInfo
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
        /// 处方ID
        /// </summary>
        public Guid PrescriptionId { get; set; }

        /// <summary>
        /// 患者姓名
        /// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// 配药状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 配药状态显示名称
        /// </summary>
        public string StatusName { get; set; } = string.Empty;

        /// <summary>
        /// 配药时间
        /// </summary>
        public DateTime? DispensingTime { get; set; }

        /// <summary>
        /// 配药师姓名
        /// </summary>
        public string PharmacistName { get; set; } = string.Empty;

        /// <summary>
        /// 发药时间
        /// </summary>
        public DateTime? DispenseTime { get; set; }

        /// <summary>
        /// 领药人姓名
        /// </summary>
        public string ReceiverName { get; set; } = string.Empty;

        /// <summary>
        /// 领药人电话
        /// </summary>
        public string ReceiverPhone { get; set; } = string.Empty;

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}