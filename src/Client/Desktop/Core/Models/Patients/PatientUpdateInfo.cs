using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Core.Models.Patients
{
    /// <summary>
    /// 更新患者信息模型
    /// UltraThink四层架构：Layer 4 (Info) - UI专用的更新数据模型
    /// </summary>
    public class PatientUpdateInfo
    {
        /// <summary>
        /// 患者ID
        /// </summary>
        [Required(ErrorMessage = "患者ID不能为空")]
        public Guid Id { get; set; }
        
        /// <summary>
        /// 患者姓名
        /// </summary>
        [Required(ErrorMessage = "患者姓名不能为空")]
        [StringLength(50, ErrorMessage = "患者姓名长度不能超过50个字符")]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 性别
        /// </summary>
        [Required(ErrorMessage = "请选择性别")]
        public Gender Gender { get; set; } = Gender.Male;
        
        /// <summary>
        /// 年龄
        /// </summary>
        [Range(0, 150, ErrorMessage = "年龄必须在0到150之间")]
        public int Age { get; set; }
        
        /// <summary>
        /// 出生日期
        /// </summary>
        public DateTime? BirthDate { get; set; }
        
        /// <summary>
        /// 电话号码
        /// </summary>
        [Phone(ErrorMessage = "请输入有效的电话号码")]
        [StringLength(20, ErrorMessage = "电话号码长度不能超过20个字符")]
        public string? PhoneNumber { get; set; }
        
        /// <summary>
        /// 身份证号
        /// </summary>
        [StringLength(18, ErrorMessage = "身份证号长度不能超过18个字符")]
        [RegularExpression(@"^[0-9Xx]{15,18}$", ErrorMessage = "请输入有效的身份证号")]
        public string? IdCard { get; set; }
        
        /// <summary>
        /// 地址
        /// </summary>
        [StringLength(200, ErrorMessage = "地址长度不能超过200个字符")]
        public string? Address { get; set; }
        
        /// <summary>
        /// 过敏史
        /// </summary>
        [StringLength(500, ErrorMessage = "过敏史长度不能超过500个字符")]
        public string? AllergyHistory { get; set; }
        
        /// <summary>
        /// 既往病史
        /// </summary>
        [StringLength(500, ErrorMessage = "既往病史长度不能超过500个字符")]
        public string? MedicalHistory { get; set; }
        
        /// <summary>
        /// 家族病史
        /// </summary>
        [StringLength(500, ErrorMessage = "家族病史长度不能超过500个字符")]
        public string? FamilyHistory { get; set; }
        
        /// <summary>
        /// 职业
        /// </summary>
        [StringLength(50, ErrorMessage = "职业长度不能超过50个字符")]
        public string? Occupation { get; set; }
        
        /// <summary>
        /// 紧急联系人
        /// </summary>
        [StringLength(50, ErrorMessage = "紧急联系人姓名长度不能超过50个字符")]
        public string? EmergencyContact { get; set; }
        
        /// <summary>
        /// 紧急联系人电话
        /// </summary>
        [Phone(ErrorMessage = "请输入有效的紧急联系人电话")]
        [StringLength(20, ErrorMessage = "紧急联系人电话长度不能超过20个字符")]
        public string? EmergencyPhone { get; set; }
        
        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(1000, ErrorMessage = "备注长度不能超过1000个字符")]
        public string? Remark { get; set; }
        
        /// <summary>
        /// 版本号（用于并发控制）
        /// </summary>
        public string? Version { get; set; }
        
        #region UI状态属性
        
        /// <summary>
        /// 是否正在提交
        /// </summary>
        public bool IsSubmitting { get; set; }
        
        /// <summary>
        /// 是否有未保存的更改
        /// </summary>
        public bool HasUnsavedChanges { get; set; }
        
        /// <summary>
        /// 验证错误信息
        /// </summary>
        public Dictionary<string, string> ValidationErrors { get; set; } = new();
        
        /// <summary>
        /// 原始数据（用于检测更改）
        /// </summary>
        public PatientInfo? OriginalData { get; set; }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 从PatientInfo创建更新信息
        /// </summary>
        public static PatientUpdateInfo FromPatientInfo(PatientInfo patientInfo)
        {
            if (patientInfo == null)
                throw new ArgumentNullException(nameof(patientInfo));
                
            return new PatientUpdateInfo
            {
                Id = patientInfo.Id,
                Name = patientInfo.Name,
                Gender = patientInfo.Gender,
                Age = patientInfo.Age,
                BirthDate = patientInfo.BirthDate,
                PhoneNumber = patientInfo.PhoneNumber,
                IdCard = patientInfo.IdCard,
                Address = patientInfo.Address,
                AllergyHistory = patientInfo.AllergyHistory,
                MedicalHistory = patientInfo.MedicalHistory,
                FamilyHistory = patientInfo.FamilyHistory,
                Occupation = patientInfo.Occupation,
                EmergencyContact = patientInfo.EmergencyContact,
                EmergencyPhone = patientInfo.EmergencyPhone,
                Remark = patientInfo.Remark,
                OriginalData = patientInfo
            };
        }
        
        /// <summary>
        /// 验证数据有效性
        /// </summary>
        public bool IsValid()
        {
            ValidationErrors.Clear();
            
            if (Id == Guid.Empty)
            {
                ValidationErrors[nameof(Id)] = "患者ID不能为空";
            }
            
            if (string.IsNullOrWhiteSpace(Name))
            {
                ValidationErrors[nameof(Name)] = "患者姓名不能为空";
            }
            else if (Name.Length > 50)
            {
                ValidationErrors[nameof(Name)] = "患者姓名长度不能超过50个字符";
            }
            
            if (Age < 0 || Age > 150)
            {
                ValidationErrors[nameof(Age)] = "年龄必须在0到150之间";
            }
            
            if (!string.IsNullOrEmpty(PhoneNumber) && PhoneNumber.Length > 20)
            {
                ValidationErrors[nameof(PhoneNumber)] = "电话号码长度不能超过20个字符";
            }
            
            if (!string.IsNullOrEmpty(IdCard))
            {
                if (IdCard.Length > 18)
                {
                    ValidationErrors[nameof(IdCard)] = "身份证号长度不能超过18个字符";
                }
                else if (!System.Text.RegularExpressions.Regex.IsMatch(IdCard, @"^[0-9Xx]{15,18}$"))
                {
                    ValidationErrors[nameof(IdCard)] = "请输入有效的身份证号";
                }
            }
            
            if (!string.IsNullOrEmpty(Address) && Address.Length > 200)
            {
                ValidationErrors[nameof(Address)] = "地址长度不能超过200个字符";
            }
            
            if (!string.IsNullOrEmpty(AllergyHistory) && AllergyHistory.Length > 500)
            {
                ValidationErrors[nameof(AllergyHistory)] = "过敏史长度不能超过500个字符";
            }
            
            return ValidationErrors.Count == 0;
        }
        
        /// <summary>
        /// 检测是否有更改
        /// </summary>
        public bool DetectChanges()
        {
            if (OriginalData == null)
            {
                HasUnsavedChanges = true;
                return true;
            }
            
            HasUnsavedChanges = Name != OriginalData.Name ||
                               Gender != OriginalData.Gender ||
                               Age != OriginalData.Age ||
                               BirthDate != OriginalData.BirthDate ||
                               PhoneNumber != OriginalData.PhoneNumber ||
                               IdCard != OriginalData.IdCard ||
                               Address != OriginalData.Address ||
                               AllergyHistory != OriginalData.AllergyHistory ||
                               MedicalHistory != OriginalData.MedicalHistory ||
                               FamilyHistory != OriginalData.FamilyHistory ||
                               Occupation != OriginalData.Occupation ||
                               EmergencyContact != OriginalData.EmergencyContact ||
                               EmergencyPhone != OriginalData.EmergencyPhone ||
                               Remark != OriginalData.Remark;
            
            return HasUnsavedChanges;
        }
        
        /// <summary>
        /// 根据身份证号计算年龄和出生日期
        /// </summary>
        public void CalculateAgeFromIdCard()
        {
            if (string.IsNullOrEmpty(IdCard) || IdCard.Length < 15)
                return;
                
            try
            {
                string birthStr;
                if (IdCard.Length == 15)
                {
                    // 15位身份证
                    birthStr = "19" + IdCard.Substring(6, 6);
                }
                else
                {
                    // 18位身份证
                    birthStr = IdCard.Substring(6, 8);
                }
                
                if (DateTime.TryParseExact(birthStr, "yyyyMMdd", null, 
                    System.Globalization.DateTimeStyles.None, out DateTime birthDate))
                {
                    BirthDate = birthDate;
                    Age = DateTime.Now.Year - birthDate.Year;
                    if (DateTime.Now.DayOfYear < birthDate.DayOfYear)
                    {
                        Age--;
                    }
                    
                    // 从身份证号判断性别（倒数第二位，奇数为男，偶数为女）
                    if (IdCard.Length >= 17)
                    {
                        var genderDigit = int.Parse(IdCard.Substring(IdCard.Length - 2, 1));
                        Gender = genderDigit % 2 == 1 ? Gender.Male : Gender.Female;
                    }
                    
                    DetectChanges();
                }
            }
            catch
            {
                // 解析失败，忽略
            }
        }
        
        /// <summary>
        /// 根据出生日期计算年龄
        /// </summary>
        public void CalculateAgeFromBirthDate()
        {
            if (BirthDate.HasValue)
            {
                Age = DateTime.Now.Year - BirthDate.Value.Year;
                if (DateTime.Now.DayOfYear < BirthDate.Value.DayOfYear)
                {
                    Age--;
                }
                DetectChanges();
            }
        }
        
        /// <summary>
        /// 重置到原始状态
        /// </summary>
        public void Reset()
        {
            if (OriginalData != null)
            {
                Name = OriginalData.Name;
                Gender = OriginalData.Gender;
                Age = OriginalData.Age;
                BirthDate = OriginalData.BirthDate;
                PhoneNumber = OriginalData.PhoneNumber;
                IdCard = OriginalData.IdCard;
                Address = OriginalData.Address;
                AllergyHistory = OriginalData.AllergyHistory;
                MedicalHistory = OriginalData.MedicalHistory;
                FamilyHistory = OriginalData.FamilyHistory;
                Occupation = OriginalData.Occupation;
                EmergencyContact = OriginalData.EmergencyContact;
                EmergencyPhone = OriginalData.EmergencyPhone;
                Remark = OriginalData.Remark;
                HasUnsavedChanges = false;
                ValidationErrors.Clear();
            }
        }
        
        #endregion
    }
}