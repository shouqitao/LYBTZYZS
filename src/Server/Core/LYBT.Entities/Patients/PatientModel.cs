using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Attributes;
using LYBT.Entities.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.Patients
{

    /// <summary>
    /// 患者实体 - UltraThink v2.0架构简化版
    /// 合并了原BasePatient和PatientModel，包含完整患者档案信息
    /// 删除五笔码字段，保留拼音码用于快速搜索
    /// 继承BaseEntity实现审计字段自动化
    /// </summary>
    [Table("Patients")]
    public class Patient : BaseEntity
    {

        // Id字段继承自BaseEntity

        /// <summary>患者姓名</summary>
        [Required]
        [StringLength(100)] // 匹配数据库的 nvarchar(100)
        [DisplayName("姓名")]
        public string Name { get; set; } = string.Empty;

        /// <summary>拼音码（用于快速搜索）</summary>
        [StringLength(20)] // 匹配数据库的 nvarchar(20)
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>性别</summary>
        [DisplayName("性别")]
        public Gender Gender { get; set; } = Gender.Unknown;

        /// <summary>婚姻状态（数据库中存在的字段）</summary>
        [DisplayName("婚姻状态")]
        public int MaritalStatus { get; set; } = 0;

        /// <summary>出生日期</summary>
        [DisplayName("出生日期")]
        public DateTime? BirthDate { get; set; }

        /// <summary>证件类型（匹配数据库int类型）</summary>
        [DisplayName("证件类型")]
        public int IdType { get; set; } = 0;  // 修复：从string?改为int

        /// <summary>证件号码 - Epic 05-P0-03: 敏感数据，需加密存储</summary>
        [StringLength(50)]
        [DisplayName("证件号码")]

        // Epic 05-P0-03: 标记为身份敏感数据需要加密
        [SensitiveData(SensitiveDataType.IdentityInfo, MaskingMode = MaskingMode.Partial)]
        public string? IdNumber { get; set; }

        /// <summary>手机号码 - Epic 05-P0-03: 敏感数据，需加密存储</summary>
        [StringLength(20)]
        [DisplayName("手机号码")]

        // Epic 05-P0-03: 标记为联系敏感数据需要加密
        [SensitiveData(SensitiveDataType.ContactInfo, MaskingMode = MaskingMode.Partial)]
        public string? PhoneNumber { get; set; }

        /// <summary>地址 - Epic 05-P0-03: 敏感数据，需加密存储</summary>
        [StringLength(256)] // 匹配数据库的 nvarchar(256)
        [DisplayName("地址")]

        // Epic 05-P0-03: 标记为个人信息敏感数据需要加密
        [SensitiveData(SensitiveDataType.PersonalInfo, MaskingMode = MaskingMode.Default)]
        public string? Address { get; set; }

        /// <summary>过敏史 - Epic 05-P0-03: 医疗敏感数据，需加密存储</summary>
        [StringLength(500)]
        [DisplayName("过敏史")]

        // Epic 05-P0-03: 标记为医疗敏感数据需要加密
        [SensitiveData(SensitiveDataType.MedicalInfo, MaskingMode = MaskingMode.Hash)]
        public string? AllergyHistory { get; set; }

        /// <summary>血型（数据库中存在的字段）</summary>
        [DisplayName("血型")]
        public int BloodType { get; set; } = 0;

        /// <summary>紧急联系人姓名（数据库中存在的字段）</summary>
        [DisplayName("紧急联系人姓名")]
        public string? EmergencyContactName { get; set; }

        /// <summary>紧急联系人电话（数据库中存在的字段）</summary>
        [DisplayName("紧急联系人电话")]
        public string? EmergencyContactPhone { get; set; }

        /// <summary>紧急联系人关系（数据库中存在的字段）</summary>
        [DisplayName("紧急联系人关系")]
        public string? EmergencyContactRelation { get; set; }

        /// <summary>患者状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;

        /// <summary>禁用原因</summary>
        [StringLength(128)] // 匹配数据库的 nvarchar(128)
        [DisplayName("禁用原因")]
        public string? DisableReason { get; set; }

        /// <summary>最后就诊时间</summary>
        [DisplayName("最后就诊时间")]
        public DateTime? LastVisitTime { get; set; }

        /// <summary>就诊次数</summary>
        [DisplayName("就诊次数")]
        public int VisitCount { get; set; } = 0;

        // 审计字段（CreatedAt、UpdatedAt、CreatedBy、UpdatedBy）和并发控制字段（RowVersion、IsDeleted）继承自BaseEntity

        /// <summary>年龄（计算属性）</summary>
        [NotMapped]
        [DisplayName("年龄")]
        public int? Age
        {
            get
            {
                if (BirthDate.HasValue)
                {
                    var today = DateTime.Today;
                    var age = today.Year - BirthDate.Value.Year;
                    if (BirthDate.Value.Date > today.AddYears(-age))
                    {
                        age--;
                    }

                    return age;
                }

                return null;
            }
        }
    }
}
