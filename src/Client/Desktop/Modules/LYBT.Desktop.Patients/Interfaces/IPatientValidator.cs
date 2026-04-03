using FluentValidation.Results;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Interfaces
{
    /// <summary>
    /// 患者验证器接口
    /// 提供患者数据验证的组件级接口
    /// </summary>
    public interface IPatientValidator
    {
        /// <summary>
        /// 验证患者输入DTO
        /// </summary>
        Task<ValidationResult> ValidatePatientInputAsync(PatientInputDto inputDto);

        /// <summary>
        /// 验证患者基本信息
        /// </summary>
        ValidationResult ValidateBasicInfo(string name, string? phoneNumber);

        /// <summary>
        /// 验证身份证号
        /// </summary>
        ValidationResult ValidateIdNumber(string? idNumber);

        /// <summary>
        /// 验证年龄
        /// </summary>
        ValidationResult ValidateAge(int? age);

        /// <summary>
        /// 验证紧急联系人信息
        /// </summary>
        ValidationResult ValidateEmergencyContact(string? contactName, string? contactPhone);

        /// <summary>
        /// 检查验证结果是否有效
        /// </summary>
        bool IsValid(ValidationResult result, out string errorMessage);

        /// <summary>
        /// 从患者DTO转换为输入DTO（用于验证）
        /// </summary>
        PatientInputDto ConvertToInputDto(PatientDetailDto patient);
    }
}
