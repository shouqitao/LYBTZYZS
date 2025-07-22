using LYBT.Common.Enums;
using System.ComponentModel;

namespace LYBT.Module.Patients.Dtos {

    /// <summary>
    /// 病人列表展示 DTO（用于病人列表）
    /// </summary>
    public class PatientDto {

        /// <summary>病人ID</summary>
        [DisplayName("病人ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>姓名</summary>
        [DisplayName("姓名")]
/// <summary>
/// Name 属性。
/// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        [DisplayName("性别")]
/// <summary>
/// Gender 属性。
/// </summary>
        public Gender Gender { get; set; } = Gender.Unknown;

        /// <summary>年龄</summary>
        [DisplayName("年龄")]
/// <summary>
/// Age 属性。
/// </summary>
        public int Age { get; set; }

        /// <summary>过敏史</summary>
        [DisplayName("过敏史")]
/// <summary>
/// AllergyHistory 属性。
/// </summary>
        public string AllergyHistory { get; set; } = string.Empty;

        /// <summary>民族</summary>
        [DisplayName("民族")]
/// <summary>
/// Ethnicity 属性。
/// </summary>
        public string Ethnicity { get; set; } = string.Empty;

        /// <summary>地址</summary>
        [DisplayName("地址")]
/// <summary>
/// Address 属性。
/// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>手机号</summary>
        [DisplayName("手机号")]
/// <summary>
/// PhoneNumber 属性。
/// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>学历</summary>
        [DisplayName("学历")]
/// <summary>
/// Education 属性。
/// </summary>
        public string Education { get; set; } = string.Empty;

        /// <summary>职业</summary>
        [DisplayName("职业")]
/// <summary>
/// Profession 属性。
/// </summary>
        public string Profession { get; set; } = string.Empty;

        /// <summary>证件类型</summary>
        [DisplayName("证件类型")]
/// <summary>
/// IDType 属性。
/// </summary>
        public string IDType { get; set; } = string.Empty;

        /// <summary>证件号</summary>
        [DisplayName("证件号")]
/// <summary>
/// IDNumber 属性。
/// </summary>
        public string IDNumber { get; set; } = string.Empty;

        /// <summary>婚姻状况</summary>
        [DisplayName("婚姻状况")]
/// <summary>
/// MaritalStatus 属性。
/// </summary>
        public string MaritalStatus { get; set; } = string.Empty;

        /// <summary>拼音码</summary>
        [DisplayName("拼音码")]
/// <summary>
/// PinyinCode 属性。
/// </summary>
        public string PinyinCode { get; set; } = string.Empty;

        /// <summary>
        /// 是否特殊患者（仅有权限的医生可见/可创建）
        /// </summary>
        [DisplayName("是否特殊患者（仅有权限的医生可见/可创建）")]
/// <summary>
/// IsSpecial 属性。
/// </summary>
        public bool IsSpecial { get; set; }
    }
}
