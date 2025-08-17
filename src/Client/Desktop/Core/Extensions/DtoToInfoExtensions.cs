using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.Desktop.Core.Models;
using LYBT.Desktop.Core.Models.Consultation;
using LYBT.Desktop.Core.Models.Formulas;
using LYBT.Desktop.Core.Models.Herbs;
using LYBT.Desktop.Core.Models.MedicalCase;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Core.Models.Users;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Core.Extensions
{
    /// <summary>
    /// UltraThink架构重构 - 统一DTO转换扩展方法库
    /// 实现四层架构：BaseModel → EntityModel → Dto → Info
    /// 专门处理API传输层DTO到UI显示层Info的转换
    /// </summary>
    public static class DtoToInfoExtensions
    {
        #region User相关转换

        /// <summary>
        /// UserDto转换为UserInfo
        /// </summary>
        public static UserInfo ToUserInfo(this UserDto dto)
        {
            return new UserInfo
            {
                Id = dto.Id,
                Username = dto.Username,
                RealName = dto.RealName,
                Role = Enum.TryParse<UserRole>(dto.Role, out var role) ? role : UserRole.Receptionist,
                Status = dto.Status,
                Email = null, // UserDto没有Email属性
                PhoneNumber = dto.PhoneNumber,
                CreateTime = dto.CreateTime,
                UpdateTime = dto.UpdateTime,
                IsSelected = false  // UI状态，默认未选中
            };
        }

        /// <summary>
        /// UserDetailDto转换为UserInfo
        /// </summary>
        public static UserInfo ToUserInfo(this UserDetailDto dto)
        {
            return new UserInfo
            {
                Id = dto.Id,
                Username = "", // UserDetailDto没有Username属性
                RealName = dto.RealName,
                Role = Enum.TryParse<UserRole>(dto.Role, out var role) ? role : UserRole.Receptionist,
                Status = dto.IsActive ? CommonStatus.Enabled : CommonStatus.Disabled,
                Email = null, // UserDetailDto没有Email属性
                PhoneNumber = dto.PhoneNumber,
                CreateTime = DateTime.Now, // UserDetailDto(BaseDto)没有CreateTime属性，使用默认值
                UpdateTime = DateTime.Now, // UserDetailDto(BaseDto)没有UpdateTime属性，使用默认值
                IsSelected = false  // UI状态，默认未选中
            };
        }

        #endregion

        #region Patient相关转换

        /// <summary>
        /// PatientDetailDto转换为PatientInfo
        /// </summary>
        public static PatientInfo ToPatientInfo(this PatientDetailDto dto)
        {
            return new PatientInfo
            {
                // 基础属性映射
                Id = dto.Id,
                Name = dto.Name,
                Gender = dto.Gender,
                Age = dto.Age,
                BirthDate = dto.DateOfBirth,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                IdNumber = dto.IDNumber,
                AllergyHistory = dto.AllergyHistory,
                CreateTime = dto.CreateTime,
                UpdateTime = dto.UpdateTime,
                Status = dto.Status,
                
                // UI专用属性
                EmergencyContact = dto.EmergencyContact,
                EmergencyPhone = dto.EmergencyPhone,
                IsActive = dto.Status == CommonStatus.Enabled
            };
        }

        #endregion

        #region Consultation相关转换

        /// <summary>
        /// ConsultationDto转换为ConsultationInfo
        /// </summary>
        public static ConsultationInfo ToConsultationInfo(this ConsultationDto dto)
        {
            return new ConsultationInfo
            {
                Id = dto.Id,
                MedicalCaseId = dto.MedicalCaseId,
                PatientId = dto.PatientId,
                PatientName = dto.PatientName,
                UserId = dto.UserId,
                DoctorName = dto.DoctorName,
                Diagnosis = dto.Diagnosis,
                ConsultationTime = dto.ConsultationTime,
                Status = ParseStatus(dto.Status)
            };
        }

        /// <summary>
        /// ConsultationDetailDto转换为ConsultationInfo
        /// </summary>
        public static ConsultationInfo ToConsultationInfo(this ConsultationDetailDto dto)
        {
            return new ConsultationInfo
            {
                Id = dto.Id,
                MedicalCaseId = dto.MedicalCaseId,
                PatientId = dto.PatientId,
                PatientName = dto.PatientName,
                UserId = dto.UserId,
                DoctorName = dto.DoctorName,
                ConsultationTime = dto.ConsultationTime,

                // 中医四诊
                Inspection = dto.Inspection,
                AuscultationOlfaction = dto.AuscultationOlfaction,
                Inquiry = dto.Inquiry,
                Palpation = dto.Palpation,
                TongueInspection = dto.TongueInspection,
                PulseCondition = dto.PulseCondition,

                // 诊断信息
                TCMDiagnosis = dto.TCMDiagnosis,
                Diagnosis = dto.Diagnosis,

                // 其他信息
                Remark = dto.Remark,
                Status = CommonStatus.Enabled // 默认状态
            };
        }

        #endregion

        #region MedicalCase相关转换

        /// <summary>
        /// MedicalCaseDto转换为MedicalCaseInfo
        /// </summary>
        public static MedicalCaseInfo ToMedicalCaseInfo(this MedicalCaseDto dto)
        {
            // 解析状态字符串为枚举
            MedicalCaseStatus status = MedicalCaseStatus.Registered;
            if (!string.IsNullOrEmpty(dto.Status))
            {
                Enum.TryParse(dto.Status, out status);
            }

            return new MedicalCaseInfo
            {
                Id = dto.Id,
                PatientId = dto.PatientId,
                PatientName = dto.PatientName ?? "",
                UserId = dto.DoctorId, // DoctorId映射到UserId
                DoctorName = dto.DoctorName ?? "",
                Status = status,
                CreateTime = dto.CreateTime,
                CompleteTime = dto.CompleteTime,
                
                // UI专用字段
                IsSelected = false,
                Remark = "",
                PatientAge = null,
                PatientGender = "",
                UpdateTime = null,
                IsActive = true
            };
        }

        /// <summary>
        /// MedicalCaseDetailDto转换为MedicalCaseInfo
        /// </summary>
        public static MedicalCaseInfo ToMedicalCaseInfo(this MedicalCaseDetailDto dto)
        {
            // 解析状态字符串为枚举
            MedicalCaseStatus status = MedicalCaseStatus.Registered;
            if (!string.IsNullOrEmpty(dto.Status))
            {
                Enum.TryParse(dto.Status, out status);
            }

            return new MedicalCaseInfo
            {
                Id = dto.Id,
                PatientId = dto.PatientId,
                PatientName = dto.PatientName ?? "",
                UserId = dto.DoctorId,
                DoctorId = dto.DoctorId,
                DoctorName = dto.DoctorName ?? "",
                Status = status,
                CreateTime = dto.CreateTime,
                CompleteTime = dto.CompleteTime,
                
                // 详细信息字段
                ChiefComplaint = dto.ChiefComplaint,
                Diagnosis = dto.DiagnosisResult,
                
                // UI专用字段
                IsSelected = false,
                Remark = dto.TreatmentPlan ?? "",
                PatientAge = null,
                PatientGender = "",
                UpdateTime = null,
                IsActive = true
            };
        }

        #endregion

        #region 现有Formula和Herb转换方法保持不变
    /// <summary>
    /// FormulaDto转换为FormulaInfo
    /// </summary>
    public static FormulaInfo ToFormulaInfo(this FormulaDto dto)
    {
        if (dto == null) return new FormulaInfo();
        
        return new FormulaInfo
        {
            Id = dto.Id,
            Name = dto.Name,
            Category = "其他", // FormulaDto没有Category字段，使用默认值
            Effect = dto.Effect,
            Usage = dto.Usage,
            Remark = dto.Remark,
            IsShared = dto.IsShared,
            CreateTime = dto.CreateTime,
            UpdateTime = dto.UpdateTime,
            CreatedBy = dto.CreatedByName, // 映射到CreatedBy
            Indications = dto is FormulaDetailDto detailDto ? detailDto.Indications : null
        };
    }

    /// <summary>
    /// HerbDto转换为HerbInfo
    /// </summary>
    public static HerbInfo ToHerbInfo(this HerbDto dto)
    {
        if (dto == null) return new HerbInfo();
        
        return new HerbInfo
        {
            Id = dto.Id,
            Name = dto.Name,
            PinYinCode = dto.PinYinCode,
            Origin = dto.Origin,
            Spec = dto.Spec,
            Unit = dto.Unit,
            Price = dto.Price,
            Effect = dto.Effect,
            Usage = dto.Usage,
            Remark = dto.Remark,
            Status = dto.Status,
            Stock = 0 // HerbDto基础类型没有Stock字段，默认为0
        };
    }

    /// <summary>
    /// 批量转换FormulaDto列表为FormulaInfo列表
    /// </summary>
    public static List<FormulaInfo> ToFormulaInfoList(this List<FormulaDto> dtos)
    {
        return dtos?.Select(dto => dto.ToFormulaInfo()).ToList() ?? new List<FormulaInfo>();
    }

        #endregion

        #region 批量转换扩展方法

        /// <summary>
        /// 批量转换UserDto列表
        /// </summary>
        public static List<UserInfo> ToUserInfoList(this IEnumerable<UserDto> dtos)
        {
            return dtos.Select(dto => dto.ToUserInfo()).ToList();
        }

        /// <summary>
        /// 批量转换PatientDetailDto列表
        /// </summary>
        public static List<PatientInfo> ToPatientInfoList(this IEnumerable<PatientDetailDto> dtos)
        {
            return dtos.Select(dto => dto.ToPatientInfo()).ToList();
        }

        /// <summary>
        /// 批量转换ConsultationDto列表
        /// </summary>
        public static List<ConsultationInfo> ToConsultationInfoList(this IEnumerable<ConsultationDto> dtos)
        {
            return dtos.Select(dto => dto.ToConsultationInfo()).ToList();
        }

        /// <summary>
        /// 批量转换MedicalCaseDto列表
        /// </summary>
        public static List<MedicalCaseInfo> ToMedicalCaseInfoList(this IEnumerable<MedicalCaseDto> dtos)
        {
            return dtos.Select(dto => dto.ToMedicalCaseInfo()).ToList();
        }


        /// <summary>
        /// 批量转换HerbDto列表为HerbInfo列表
        /// </summary>
        public static List<HerbInfo> ToHerbInfoList(this List<HerbDto> dtos)
        {
            return dtos?.Select(dto => dto.ToHerbInfo()).ToList() ?? new List<HerbInfo>();
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 解析状态字符串为CommonStatus枚举
        /// </summary>
        private static CommonStatus ParseStatus(string status)
        {
            return status?.ToLower() switch
            {
                "enabled" => CommonStatus.Enabled,
                "disabled" => CommonStatus.Disabled,
                _ => CommonStatus.Enabled
            };
        }

        #endregion
    }
}