using System;
using LYBT.Shared.Models.Core;

namespace LYBT.WPF.Client.Core.Models.Doctors {
    /// <summary>
    /// 医生信息 - 前端专用，继承共享基础模型
    /// </summary>
    public class DoctorInfo : BaseDoctorModel {
        /// <summary>
        /// 医生编码（前端扩展字段）
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 科室（前端扩展字段）
        /// </summary>
        public string Department { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用（前端业务字段）
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 联系电话（前端显示字段，映射自ContactNumber）
        /// </summary>
        public string Phone { 
            get => ContactNumber ?? string.Empty; 
            set => ContactNumber = value; 
        }

        /// <summary>
        /// 专科特长（前端显示字段，映射自Specialty）
        /// </summary>
        public string Specialties { 
            get => Specialty ?? string.Empty; 
            set => Specialty = value; 
        }
    }
}