using LYBT.Models.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Core;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Doctors {

    /// <summary>
    /// 医生信息实体 - 医生基础信息管理，关联用户系统，支持软删除策略
    /// </summary>
    public class DoctorModel : BaseDoctorModel {

        /// <summary>
        /// 身份证号码（后端特有，敏感信息）
        /// </summary>
        [StringLength(18)]
        [DisplayName("身份证号码")]
        public string? IdNumber { get; set; }

        /// <summary>
        /// 关联的用户实体（导航属性）
        /// </summary>
        [Required]
        [DisplayName("关联用户")]
        public virtual UserModel User { get; set; } = null!;
    }
}