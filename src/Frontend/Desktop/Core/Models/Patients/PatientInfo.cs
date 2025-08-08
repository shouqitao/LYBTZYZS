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
        /// <summary>紧急联系人（前端扩展字段）</summary>
        public string? EmergencyContact { get; set; }

        /// <summary>紧急联系电话（前端扩展字段）</summary>
        public string? EmergencyPhone { get; set; }

        /// <summary>是否激活（前端状态字段）</summary>
        public bool IsActive { get; set; } = true;
    }
}