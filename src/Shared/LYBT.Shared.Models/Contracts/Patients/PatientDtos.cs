using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Patients
{

    /// <summary>
    /// 患者信息DTO - UltraThink架构优化：统一PatientDto和PatientDetailDto
    /// 包含核心字段和详细字段，Age为计算属性，字段名统一使用BirthDate、IdNumber
    /// </summary>
    public class PatientDto : StatusDto
    {

        /// <summary>患者姓名</summary>
        [Required(ErrorMessage = "患者姓名不能为空")]
        [StringLength(50, ErrorMessage = "患者姓名长度不能超过50个字符")]
        [DisplayName("患者姓名")]
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        [DisplayName("性别")]
        public Gender Gender { get; set; } = Gender.Unknown;

        /// <summary>出生日期</summary>
        [DisplayName("出生日期")]
        public DateTime? BirthDate { get; set; }

        /// <summary>年龄（计算属性）</summary>
        [DisplayName("年龄")]
        public int Age
        {
            get
            {
                if (BirthDate == null)
                {
                    return 0;
                }

                var today = DateTime.Today;
                var age = today.Year - BirthDate.Value.Year;
                if (BirthDate.Value.Date > today.AddYears(-age))
                {
                    age--;
                }

                return Math.Max(0, age);
            }
        }

        /// <summary>证件类型</summary>
        [DisplayName("证件类型")]
        public string? IdType { get; set; }

        /// <summary>身份证号</summary>
        [StringLength(18, ErrorMessage = "身份证号长度不能超过18个字符")]
        [DisplayName("身份证号")]
        public string? IdNumber { get; set; }

        /// <summary>手机号</summary>
        [StringLength(20, ErrorMessage = "手机号长度不能超过20个字符")]
        [DisplayName("手机号")]
        public string? PhoneNumber { get; set; }

        /// <summary>地址</summary>
        [StringLength(200, ErrorMessage = "地址长度不能超过200个字符")]
        [DisplayName("地址")]
        public string? Address { get; set; }

        /// <summary>过敏史</summary>
        [StringLength(500, ErrorMessage = "过敏史长度不能超过500个字符")]
        [DisplayName("过敏史")]
        public string? AllergyHistory { get; set; }

        /// <summary>既往病史</summary>
        [StringLength(1000, ErrorMessage = "既往病史长度不能超过1000个字符")]
        [DisplayName("既往病史")]
        public string? MedicalHistory { get; set; }

        /// <summary>家族史</summary>
        [StringLength(500, ErrorMessage = "家族史长度不能超过500个字符")]
        [DisplayName("家族史")]
        public string? FamilyHistory { get; set; }

        /// <summary>职业</summary>
        [StringLength(50, ErrorMessage = "职业长度不能超过50个字符")]
        [DisplayName("职业")]
        public string? Profession { get; set; }

        /// <summary>婚姻状况</summary>
        [StringLength(20, ErrorMessage = "婚姻状况长度不能超过20个字符")]
        [DisplayName("婚姻状况")]
        public string? MaritalStatus { get; set; }

        /// <summary>紧急联系人</summary>
        [StringLength(50, ErrorMessage = "紧急联系人长度不能超过50个字符")]
        [DisplayName("紧急联系人")]
        public string? EmergencyContact { get; set; }

        /// <summary>紧急联系电话</summary>
        [StringLength(20, ErrorMessage = "紧急联系电话长度不能超过20个字符")]
        [DisplayName("紧急联系电话")]
        public string? EmergencyPhone { get; set; }

        /// <summary>拼音码</summary>
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>是否激活（计算属性）</summary>
        [DisplayName("是否激活")]
        public bool IsActive => Status == CommonStatus.Enabled;
    }

    /// <summary>
    /// 患者创建DTO - UltraThink架构优化：统一字段名BirthDate、IdNumber
    /// 用于创建新患者档案的请求模型
    /// </summary>
    public class PatientCreateDto
    {

        /// <summary>患者姓名</summary>
        [Required(ErrorMessage = "患者姓名不能为空")]
        [StringLength(50, ErrorMessage = "患者姓名长度不能超过50个字符")]
        [DisplayName("患者姓名")]
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        [DisplayName("性别")]
        public Gender Gender { get; set; } = Gender.Unknown;

        /// <summary>出生日期</summary>
        [DisplayName("出生日期")]
        public DateTime? BirthDate { get; set; }

        /// <summary>年龄</summary>
        [Range(0, 200, ErrorMessage = "年龄必须在0-200之间")]
        [DisplayName("年龄")]
        public int Age { get; set; }

        /// <summary>身份证号</summary>
        [StringLength(18, ErrorMessage = "身份证号长度不能超过18个字符")]
        [DisplayName("身份证号")]
        public string? IdNumber { get; set; }

        /// <summary>手机号</summary>
        [StringLength(20, ErrorMessage = "手机号长度不能超过20个字符")]
        [DisplayName("手机号")]
        public string? PhoneNumber { get; set; }

        /// <summary>地址</summary>
        [StringLength(200, ErrorMessage = "地址长度不能超过200个字符")]
        [DisplayName("地址")]
        public string? Address { get; set; }

        /// <summary>过敏史</summary>
        [StringLength(500, ErrorMessage = "过敏史长度不能超过500个字符")]
        [DisplayName("过敏史")]
        public string? AllergyHistory { get; set; }

        /// <summary>既往病史</summary>
        [StringLength(1000, ErrorMessage = "既往病史长度不能超过1000个字符")]
        [DisplayName("既往病史")]
        public string? MedicalHistory { get; set; }

        /// <summary>家族史</summary>
        [StringLength(500, ErrorMessage = "家族史长度不能超过500个字符")]
        [DisplayName("家族史")]
        public string? FamilyHistory { get; set; }

        /// <summary>职业</summary>
        [StringLength(50, ErrorMessage = "职业长度不能超过50个字符")]
        [DisplayName("职业")]
        public string? Profession { get; set; }

        /// <summary>婚姻状况</summary>
        [StringLength(20, ErrorMessage = "婚姻状况长度不能超过20个字符")]
        [DisplayName("婚姻状况")]
        public string? MaritalStatus { get; set; }

        /// <summary>紧急联系人</summary>
        [StringLength(50, ErrorMessage = "紧急联系人长度不能超过50个字符")]
        [DisplayName("紧急联系人")]
        public string? EmergencyContact { get; set; }

        /// <summary>紧急联系电话</summary>
        [StringLength(20, ErrorMessage = "紧急联系电话长度不能超过20个字符")]
        [DisplayName("紧急联系电话")]
        public string? EmergencyPhone { get; set; }

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;
    }

    /// <summary>
    /// 患者更新DTO - UltraThink架构优化：统一字段名BirthDate、IdNumber
    /// 用于更新患者档案的请求模型
    /// </summary>
    public class PatientUpdateDto : BaseDto
    {

        /// <summary>患者姓名</summary>
        [Required(ErrorMessage = "患者姓名不能为空")]
        [StringLength(50, ErrorMessage = "患者姓名长度不能超过50个字符")]
        [DisplayName("患者姓名")]
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        [DisplayName("性别")]
        public Gender Gender { get; set; } = Gender.Unknown;

        /// <summary>出生日期</summary>
        [DisplayName("出生日期")]
        public DateTime? BirthDate { get; set; }

        /// <summary>年龄</summary>
        [Range(0, 200, ErrorMessage = "年龄必须在0-200之间")]
        [DisplayName("年龄")]
        public int Age { get; set; }

        /// <summary>身份证号</summary>
        [StringLength(18, ErrorMessage = "身份证号长度不能超过18个字符")]
        [DisplayName("身份证号")]
        public string? IdNumber { get; set; }

        /// <summary>手机号</summary>
        [StringLength(20, ErrorMessage = "手机号长度不能超过20个字符")]
        [DisplayName("手机号")]
        public string? PhoneNumber { get; set; }

        /// <summary>地址</summary>
        [StringLength(200, ErrorMessage = "地址长度不能超过200个字符")]
        [DisplayName("地址")]
        public string? Address { get; set; }

        /// <summary>过敏史</summary>
        [StringLength(500, ErrorMessage = "过敏史长度不能超过500个字符")]
        [DisplayName("过敏史")]
        public string? AllergyHistory { get; set; }

        /// <summary>既往病史</summary>
        [StringLength(1000, ErrorMessage = "既往病史长度不能超过1000个字符")]
        [DisplayName("既往病史")]
        public string? MedicalHistory { get; set; }

        /// <summary>家族史</summary>
        [StringLength(500, ErrorMessage = "家族史长度不能超过500个字符")]
        [DisplayName("家族史")]
        public string? FamilyHistory { get; set; }

        /// <summary>职业</summary>
        [StringLength(50, ErrorMessage = "职业长度不能超过50个字符")]
        [DisplayName("职业")]
        public string? Profession { get; set; }

        /// <summary>婚姻状况</summary>
        [StringLength(20, ErrorMessage = "婚姻状况长度不能超过20个字符")]
        [DisplayName("婚姻状况")]
        public string? MaritalStatus { get; set; }

        /// <summary>紧急联系人</summary>
        [StringLength(50, ErrorMessage = "紧急联系人长度不能超过50个字符")]
        [DisplayName("紧急联系人")]
        public string? EmergencyContact { get; set; }

        /// <summary>紧急联系电话</summary>
        [StringLength(20, ErrorMessage = "紧急联系电话长度不能超过20个字符")]
        [DisplayName("紧急联系电话")]
        public string? EmergencyPhone { get; set; }

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;
    }

    /// <summary>
    /// 快速创建患者DTO - 前后端共享API契约
    /// 用于快速创建患者档案（仅包含必要字段）
    /// </summary>
    public class QuickPatientCreateDto
    {

        /// <summary>患者姓名</summary>
        [Required(ErrorMessage = "患者姓名不能为空")]
        [StringLength(50, ErrorMessage = "患者姓名长度不能超过50个字符")]
        [DisplayName("患者姓名")]
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        [DisplayName("性别")]
        public Gender Gender { get; set; } = Gender.Unknown;

        /// <summary>年龄</summary>
        [Range(0, 200, ErrorMessage = "年龄必须在0-200之间")]
        [DisplayName("年龄")]
        public int Age { get; set; }

        /// <summary>手机号</summary>
        [StringLength(20, ErrorMessage = "手机号长度不能超过20个字符")]
        [DisplayName("手机号")]
        public string? PhoneNumber { get; set; }

        /// <summary>过敏史（重要信息）</summary>
        [StringLength(500, ErrorMessage = "过敏史长度不能超过500个字符")]
        [DisplayName("过敏史")]
        public string? AllergyHistory { get; set; }
    }
}
