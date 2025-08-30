using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Base;
using LYBT.Shared.Utilities.Helpers;

namespace LYBT.Module.Patients.Helpers
{
    /// <summary>
    /// PatientService验证助手类 - UltraThink Helper模式
    /// 负责所有验证、业务规则检查和数据完整性校验相关逻辑
    /// </summary>
    public class PatientValidationHelper : BaseValidationHelper
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<PatientValidationHelper> _logger;
        private readonly PatientValidationService _validationService;

        public PatientValidationHelper(
            IPatientRepository patientRepository,
            IMapper mapper,
            ILogger<PatientValidationHelper> logger,
            PatientValidationService validationService)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        }

        #region 创建验证

        /// <summary>
        /// 验证患者创建数据
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateForCreateAsync(PatientCreateDto dto)
        {
            try
            {
                // 转换为PatientDto进行验证
                var detailDto = _mapper.Map<PatientDto>(dto);
                await _validationService.ValidateForCreateAsync(detailDto);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证患者创建数据失败: {PatientName}", dto.Name);                return ServiceResult<bool>.Failure($"验证失败: {ex.Message}", ex);            }
        }

        /// <summary>
        /// 验证患者信息（简化实现）
        /// </summary>
        public async Task<ServiceResult<object>> ValidatePatientAsync(PatientCreateDto dto)
        {
            try
            {
                // 转换为PatientDto进行验证
                var detailDto = _mapper.Map<PatientDto>(dto);
                await _validationService.ValidateForCreateAsync(detailDto);
                var result = new { IsValid = true, Message = "验证通过" };                return ServiceResult<object>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证患者信息失败");                var result = new { IsValid = false, Message = $"验证失败: {ex.Message}" };                return ServiceResult<object>.Success(result);
            }
        }

        #endregion

        #region 更新验证

        /// <summary>
        /// 验证患者更新数据
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateForUpdateAsync(Guid id, PatientUpdateDto dto)
        {
            try
            {
                // 转换为PatientDto进行验证
                var detailDto = _mapper.Map<PatientDto>(dto);
                detailDto.Id = id;  // 确保ID正确传递
                await _validationService.ValidateForUpdateAsync(id, detailDto);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证患者更新数据失败: PatientId={PatientId}", id);                return ServiceResult<bool>.Failure($"验证失败: {ex.Message}", ex);            }
        }

        #endregion

        #region 数据完整性验证

        /// <summary>
        /// 验证患者ID是否有效
        /// </summary>
        public ServiceResult<bool> ValidatePatientId(Guid id)
        {
            return ValidateGuid(id, "患者ID");
        }

        /// <summary>
        /// 验证患者姓名
        /// </summary>
        public ServiceResult<bool> ValidatePatientName(string name)
        {
            var requiredValidation = ValidateRequiredString(name, "患者姓名");
            if (!requiredValidation.IsSuccess) return requiredValidation;
            
            return ValidateStringLength(name, "患者姓名", 50);
        }

        /// <summary>
        /// 验证手机号码格式  
        /// </summary>
        public ServiceResult<bool> ValidatePhoneNumber(string phoneNumber)
        {
            return ValidatePhoneNumber(phoneNumber, "手机号码");
        }

        /// <summary>
        /// 验证身份证号码格式
        /// </summary>
        public ServiceResult<bool> ValidateIdNumber(string idNumber)
        {
            return ValidateIdCard(idNumber, "身份证号码");
        }

        /// <summary>
        /// 验证患者年龄
        /// </summary>
        public ServiceResult<bool> ValidateAge(int? age)
        {
            if (!age.HasValue)
                return ServiceResult<bool>.Success(true); // 年龄可以为空

            return ValidateNumericRange(age.Value, "年龄", 0, 150);
        }

        /// <summary>
        /// 验证患者性别
        /// </summary>
        public ServiceResult<bool> ValidateGender(Gender? gender)
        {
            if (!gender.HasValue)
                return ServiceResult<bool>.Success(true); // 性别可以为空

            if (!Enum.IsDefined(typeof(Gender), gender.Value))
                return ServiceResult<bool>.Failure("性别值无效");            return ServiceResult<bool>.Success(true);
        }

        #endregion

        #region 业务规则验证

        /// <summary>
        /// 检查重复患者
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> CheckDuplicatePatientsAsync(string idNumber, string phoneNumber)
        {
            try
            {
                var duplicates = await _validationService.CheckDuplicatePatientsAsync(idNumber, phoneNumber);
                return ServiceResult<List<PatientDto>>.Success(duplicates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查重复患者失败");                return ServiceResult<List<PatientDto>>.Failure("检查重复患者失败", ex);            }
        }

        /// <summary>
        /// 验证患者是否可以删除
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateCanDeleteAsync(Guid id)
        {
            try
            {
                var patient = await _patientRepository.GetByIdAsync(id, true);
                if (patient == null)
                    return ServiceResult<bool>.Failure("患者不存在");                // 这里可以添加业务规则检查，比如是否有未完成的就诊记录等
                // 目前简化为总是允许删除（软删除）
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证患者删除权限失败: {PatientId}", id);                return ServiceResult<bool>.Failure("验证删除权限失败");            }
        }

        /// <summary>
        /// 验证患者是否可以更新
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateCanUpdateAsync(Guid id)
        {
            try
            {
                var patient = await _patientRepository.GetByIdAsync(id, true);
                if (patient == null)
                    return ServiceResult<bool>.Failure("患者不存在");                // 这里可以添加业务规则检查，比如患者状态是否允许更新等
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证患者更新权限失败: {PatientId}", id);                return ServiceResult<bool>.Failure("验证更新权限失败");            }
        }

        /// <summary>
        /// 验证状态变更是否有效
        /// </summary>
        public ServiceResult<bool> ValidateStatusChange(CommonStatus currentStatus, CommonStatus newStatus)
        {
            // 简化的状态变更验证
            if (currentStatus == newStatus)
                return ServiceResult<bool>.Failure("新状态与当前状态相同，无需更改");            // 这里可以添加更复杂的状态转换规则
            return ServiceResult<bool>.Success(true);
        }

        #endregion

        #region 身份证信息处理

        /// <summary>
        /// 处理身份证信息（提取年龄、性别等）
        /// </summary>
        public ServiceResult<Patient> ProcessIdNumberInfo(Patient model)
        {
            try
            {
                _validationService.ProcessIdNumberInfo(model);
                return ServiceResult<Patient>.Success(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理身份证信息失败: {PatientId}", model.Id);                return ServiceResult<Patient>.Failure("处理身份证信息失败");            }
        }

        #endregion

        #region 数据格式验证
        
        // 基类已提供ValidateStringLength、ValidateEmail等方法，此处不再重复定义
        
        #endregion

        #region 综合验证

        /// <summary>
        /// 综合验证患者基本信息
        /// </summary>
        public async Task<ServiceResult<bool>> ValidatePatientBasicInfoAsync(PatientCreateDto dto)
        {
            try
            {
                // 验证姓名
                var nameValidation = ValidatePatientName(dto.Name);
                if (!nameValidation.IsSuccess)
                    return nameValidation;

                // 验证手机号
                var phoneValidation = ValidatePhoneNumber(dto.PhoneNumber);
                if (!phoneValidation.IsSuccess)
                    return phoneValidation;

                // 验证身份证号
                var idValidation = ValidateIdNumber(dto.IdNumber);
                if (!idValidation.IsSuccess)
                    return idValidation;

                // 验证性别
                var genderValidation = ValidateGender(dto.Gender);
                if (!genderValidation.IsSuccess)
                    return genderValidation;

                // 验证年龄
                var ageValidation = ValidateAge(dto.Age);
                if (!ageValidation.IsSuccess)
                    return ageValidation;

                // 验证地址长度
                var addressValidation = ValidateStringLength(dto.Address, "地址", 200);
                if (!addressValidation.IsSuccess)
                    return addressValidation;

                // 验证既往病史长度
                var medicalHistoryValidation = ValidateStringLength(dto.MedicalHistory, "既往病史", 1000);
                if (!medicalHistoryValidation.IsSuccess)
                    return medicalHistoryValidation;

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "综合验证患者基本信息失败");                return ServiceResult<bool>.Failure("验证患者基本信息失败");
            }
        }

        #endregion
    }
}


