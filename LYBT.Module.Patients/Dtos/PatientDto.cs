using LYBT.Common.Enums;
using System.ComponentModel;

namespace LYBT.Module.Patients.Dtos {
    /// <summary>
    /// 病人列表展示 DTO（用于病人列表）
    /// </summary>
    public class PatientDto {
        /// <summary>病人ID</summary>
        public Guid Id { get; set; }

        /// <summary>姓名</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        public Gender Gender { get; set; } = Gender.Unknown;

        /// <summary>年龄</summary>
        public int? Age { get; set; }

        /// <summary>手机号</summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>地址</summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>拼音码（姓名拼音首字母，便于快速模糊检索）</summary>
        public string PinyinCode { get; set; } = string.Empty;
    }
}
