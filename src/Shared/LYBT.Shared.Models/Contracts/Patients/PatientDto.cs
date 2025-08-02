using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Patients {

    /// <summary>
    /// 病人列表展示 DTO（用于病人列表）
    /// </summary>
    public class PatientDto {

        /// <summary>病人ID</summary>
        [DisplayName("病人ID")]
        public Guid Id { get; set; }

        /// <summary>姓名</summary>
        [DisplayName("姓名")]
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        [DisplayName("性别")]
        public Gender Gender { get; set; } = Gender.Unknown;

        /// <summary>年龄</summary>
        [DisplayName("年龄")]
        public int Age { get; set; }

        /// <summary>过敏史</summary>
        [DisplayName("过敏史")]
        public string AllergyHistory { get; set; } = string.Empty;

        /// <summary>民族</summary>
        [DisplayName("民族")]
        public string Ethnicity { get; set; } = string.Empty;

        /// <summary>地址</summary>
        [DisplayName("地址")]
        public string Address { get; set; } = string.Empty;

        /// <summary>手机号</summary>
        [DisplayName("手机号")]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>学历</summary>
        [DisplayName("学历")]
        public string Education { get; set; } = string.Empty;

        /// <summary>职业</summary>
        [DisplayName("职业")]
        public string Profession { get; set; } = string.Empty;

        /// <summary>证件类型</summary>
        [DisplayName("证件类型")]
        public string IDType { get; set; } = string.Empty;

        /// <summary>证件号</summary>
        [DisplayName("证件号")]
        public string IDNumber { get; set; } = string.Empty;

        /// <summary>婚姻状况</summary>
        [DisplayName("婚姻状况")]
        public string MaritalStatus { get; set; } = string.Empty;

        /// <summary>拼音码</summary>
        [DisplayName("拼音码")]
        public string PinyinCode { get; set; } = string.Empty;

        /// <summary>五笔码</summary>
        [DisplayName("五笔码")]
        public string WuBiCode { get; set; } = string.Empty;
    }
}