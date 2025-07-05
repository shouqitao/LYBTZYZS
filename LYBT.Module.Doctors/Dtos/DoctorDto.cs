using LYBT.Common.Enums;

namespace LYBT.Module.Doctors.Dtos {
    /// <summary>
    /// 医生列表 DTO
    /// </summary>
    public class DoctorDto {
        /// <summary>医生ID</summary>
        public Guid Id { get; set; }

        /// <summary>医生姓名(来自用户)</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        public Gender Gender { get; set; } = Gender.Unknown;

        /// <summary>出生日期</summary>
        public DateTime Birthday { get; set; }

        /// <summary>职称</summary>
        public DoctorTitle Title { get; set; }

        /// <summary>联系电话(来自用户)</summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>姓名拼音码</summary>
        public string PinyinCode { get; set; } = string.Empty;

        /// <summary>执业证书号</summary>
        public string? LicenseNumber { get; set; }

        /// <summary>状态</summary>
        public DoctorStatus Status { get; set; } = DoctorStatus.Active;

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}