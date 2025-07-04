using LYBT.Common.Enums;
using LYBT.Module.Users.Models;

namespace LYBT.Models.Doctors {

    /// <summary>
    /// 医生领域实体
    /// </summary>
    public class DoctorModel {

        /// <summary>
        /// 医生ID
        /// </summary>
        public Guid Id { get; set; }


        /// <summary>
        /// 性别
        /// </summary>
        public Gender Gender { get; set; } = Gender.Unknown;

        /// <summary>
        /// 年龄
        /// </summary>
        public int Age { get; set; } = 0;


        /// <summary>
        /// 职称
        /// </summary>
        public DoctorTitle Title { get; set; } = DoctorTitle.Junior;

        /// <summary>
        /// 擅长领域
        /// </summary>
        public string Specialty { get; set; } = string.Empty;

        /// <summary>
        /// 是否在职
        /// </summary>
        public DoctorStatus Status { get; set; } = DoctorStatus.Active;

        /// <summary>
        /// 工作状态（如休假、外出等）
        /// </summary>
        public DoctorWorkStatus WorkStatus { get; set; } = DoctorWorkStatus.Clinic;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; } = string.Empty;

        public DateTime Birthday { get; set; }

        /// <summary>执业证书号</summary>
        public string? LicenseNumber { get; set; }

        /// <summary>
        /// 医生姓名拼音码，用于快捷搜索
        /// </summary>
        public string PinyinCode { get; set; } = string.Empty;

        /// <summary>
        /// 对应的用户ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 导航属性：关联的用户
        /// </summary>
        public UserModel User { get; set; } = null!;
    }
}