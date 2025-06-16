using LYBT.Common.Enums;

namespace LYBT.Module.Patients.Dtos {
    /// <summary>
    /// 病人详情Dto，用于患者详情展示
    /// </summary>
    public class PatientDetailDto {
        /// <summary>病人ID</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>姓名</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        public Gender Gender { get; set; } = Gender.Unknown;

        /// <summary>年龄</summary>
        public int Age { get; set; }

        /// <summary>过敏史</summary>
        public string AllergyHistory { get; set; } = string.Empty;

        /// <summary>民族</summary>
        public string Ethnicity { get; set; } = string.Empty;

        /// <summary>地址</summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>手机号</summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>学历</summary>
        public string Education { get; set; } = string.Empty;

        /// <summary>职业</summary>
        public string Profession { get; set; } = string.Empty;

        /// <summary>证件类型</summary>
        public string IDType { get; set; } = string.Empty;

        /// <summary>证件号</summary>
        public string IDNumber { get; set; } = string.Empty;

        /// <summary>婚姻状况</summary>
        public string MaritalStatus { get; set; } = string.Empty;

        /// <summary>拼音码</summary>
        public string PinyinCode { get; set; } = string.Empty;
    }
}
