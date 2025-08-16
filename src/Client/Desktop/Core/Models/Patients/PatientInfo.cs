using LYBT.Shared.Models.Contracts.Common;
using System;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Core;

namespace LYBT.Desktop.Core.Models.Patients
{
    /// <summary>
    /// 患者信息模型 - 前端专用，继承共享基础模型
    /// </summary>
    public class PatientInfo : BasePatient
    {
        /// <summary>紧急联系人（前端扩展字段）</summary>
        public string? EmergencyContact { get; set; }

        /// <summary>紧急联系电话（前端扩展字段）</summary>
        public string? EmergencyPhone { get; set; }

        /// <summary>是否激活（前端状态字段）</summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>电话号码（映射到PhoneNumber）</summary>
        public string? Phone 
        { 
            get => PhoneNumber; 
            set => PhoneNumber = value; 
        }
        
        /// <summary>性别显示文本</summary>
        public string GenderDisplay 
        {
            get
            {
                return Gender switch
                {
                    Gender.Male => "男",
                    Gender.Female => "女",
                    _ => "未知"
                };
            }
        }
    }
}