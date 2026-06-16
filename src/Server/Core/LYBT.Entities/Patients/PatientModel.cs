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
        [StringLength(50)] // 统一为50，与Fluent API配置一致
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>性别</summary>
        [DisplayName("性别")]
        public Gender Gender { get; set; } = Gender.Unknown;

        /// <summary>出生日期</summary>
        [DisplayName("出生日期")]
        public DateTime? BirthDate { get; set; }

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

        /// <summary>患者状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;

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
