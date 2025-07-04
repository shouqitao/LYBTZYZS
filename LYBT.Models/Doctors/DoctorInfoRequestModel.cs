using LYBT.Common.Enums;
using System;

namespace LYBT.Models.Doctors {
    /// <summary>
    /// 医生信息修改申请
    /// </summary>
    public class DoctorInfoRequestModel {
        /// <summary>申请ID</summary>
        public Guid Id { get; set; }
        /// <summary>医生ID</summary>
        public Guid DoctorId { get; set; }
        /// <summary>姓名</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>联系电话</summary>
        public string Phone { get; set; } = string.Empty;
        /// <summary>性别</summary>
        public Gender Gender { get; set; } = Gender.Unknown;
        /// <summary>出生日期</summary>
        public DateTime Birthday { get; set; }
        /// <summary>姓名拼音码</summary>
        public string PinyinCode { get; set; } = string.Empty;
        /// <summary>执业证书号</summary>
        public string? LicenseNumber { get; set; }
        /// <summary>职称</summary>
        public DoctorTitle Title { get; set; } = DoctorTitle.Junior;
        /// <summary>医生状态</summary>
        public DoctorStatus DoctorStatus { get; set; } = DoctorStatus.Active;
        /// <summary>备注</summary>
        public string Remark { get; set; } = string.Empty;
        /// <summary>申请状态</summary>
        public DoctorInfoRequestStatus Status { get; set; } = DoctorInfoRequestStatus.Pending;
        /// <summary>提交时间</summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;
    }
}
