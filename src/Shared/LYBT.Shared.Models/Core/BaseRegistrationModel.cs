using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Shared.Models.Core {

    /// <summary>
    /// 挂号基础模型 - 前后端共享核心字段
    /// 包含所有通用的挂号信息字段，各层可基于此模型扩展
    /// </summary>
    public class BaseRegistrationModel {

        /// <summary>挂号唯一标识</summary>
        [DisplayName("挂号ID")]
        public Guid Id { get; set; }

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>患者姓名</summary>
        [DisplayName("患者姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>医生姓名</summary>
        [DisplayName("医生姓名")]
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>挂号类型</summary>
        [DisplayName("挂号类型")]
        public RegistrationType RegistrationType { get; set; } = RegistrationType.Regular;

        /// <summary>是否从医生直接挂号</summary>
        [DisplayName("是否从医生直接挂号")]
        public bool IsFromDoctor { get; set; } = false;

        /// <summary>挂号状态</summary>
        [DisplayName("挂号状态")]
        public RegistrationStatus Status { get; set; } = RegistrationStatus.Expired;

        /// <summary>挂号时间</summary>
        [DisplayName("挂号时间")]
        public DateTime RegistrationTime { get; set; } = DateTime.Now;

        /// <summary>备注信息</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>创建时间（统一命名）</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }
    }
}