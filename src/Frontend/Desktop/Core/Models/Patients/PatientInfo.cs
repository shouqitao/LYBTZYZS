using System;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Core;

namespace LYBT.WPF.Client.Core.Models.Patients
{
    /// <summary>
    /// 患者信息模型 - 前端专用，继承共享基础模型
    /// </summary>
    public class PatientInfo : BasePatientModel
    {
        /// <summary>患者状态（前端扩展字段）</summary>
        public string? Status { get; set; }

        /// <summary>最后就诊时间（前端业务字段）</summary>
        public DateTime? LastVisitTime { get; set; }

        /// <summary>就诊次数（前端统计字段）</summary>
        public int VisitCount { get; set; }

        /// <summary>紧急联系人（前端扩展字段）</summary>
        public string? EmergencyContact { get; set; }

        /// <summary>紧急联系电话（前端扩展字段）</summary>
        public string? EmergencyPhone { get; set; }
    }
}