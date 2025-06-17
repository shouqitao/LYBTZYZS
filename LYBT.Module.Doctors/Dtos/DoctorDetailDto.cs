using System;

namespace LYBT.Module.Doctors.Dtos {
    /// <summary>
    /// 医生详情 DTO
    /// </summary>
    public class DoctorDetailDto {
        /// <summary>医生ID</summary>
        public Guid Id { get; set; }

        /// <summary>医生姓名</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        public string Gender { get; set; } = string.Empty;

        /// <summary>出生日期</summary>
        public DateTime Birthday { get; set; }

        /// <summary>联系电话</summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>姓名拼音码</summary>
        public string PinyinCode { get; set; } = string.Empty;

        /// <summary>执业证书号</summary>
        public string? LicenseNumber { get; set; }

        /// <summary>职称</summary>
        public string? Title { get; set; }

        /// <summary>医生当前状态</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}
