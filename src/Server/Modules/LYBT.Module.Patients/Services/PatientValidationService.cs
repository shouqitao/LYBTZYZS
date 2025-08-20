using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Services
{
    /// <summary>
    /// 患者验证服务
    /// 负责数据验证、重复检查、身份证解析等功能
    /// </summary>
    public class PatientValidationService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IMapper _mapper;

        public PatientValidationService(IPatientRepository patientRepository, IMapper mapper)
        {
            _patientRepository = patientRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// 从身份证号码中提取出生日期
        /// </summary>
        public DateTime? ExtractBirthDateFromIdNumber(string idNumber)
        {
            if (string.IsNullOrEmpty(idNumber) || idNumber.Length != PatientConstants.IdNumberLength)
            {
                return null;
            }

            try
            {
                var year = int.Parse(idNumber.Substring(6, 4));
                var month = int.Parse(idNumber.Substring(10, 2));
                var day = int.Parse(idNumber.Substring(12, 2));
                return new DateTime(year, month, day);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 计算年龄
        /// </summary>
        public int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age))
            {
                age--;
            }
            return age;
        }

        /// <summary>
        /// 验证新建患者数据
        /// </summary>
        public async Task ValidateForCreateAsync(PatientDetailDto dto)
        {
            // 基础验证
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("患者姓名不能为空");
            }

            // 检查身份证号重复
            if (!string.IsNullOrEmpty(dto.IDNumber))
            {
                if (await _patientRepository.IsIdNumberExistsAsync(dto.IDNumber))
                {
                    throw new ArgumentException("身份证号已存在");
                }
            }

            // 检查手机号重复
            if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                if (await _patientRepository.IsPhoneNumberExistsAsync(dto.PhoneNumber))
                {
                    throw new ArgumentException("手机号已存在");
                }
            }
        }

        /// <summary>
        /// 验证更新患者数据
        /// </summary>
        public async Task ValidateForUpdateAsync(Guid id, PatientDetailDto dto)
        {
            // 基础验证
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("患者姓名不能为空");
            }

            // 检查身份证号重复（排除当前患者）
            if (!string.IsNullOrEmpty(dto.IDNumber))
            {
                if (await _patientRepository.IsIdNumberExistsAsync(dto.IDNumber, id))
                {
                    throw new ArgumentException("身份证号已存在");
                }
            }

            // 检查手机号重复（排除当前患者）
            if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                if (await _patientRepository.IsPhoneNumberExistsAsync(dto.PhoneNumber, id))
                {
                    throw new ArgumentException("手机号已存在");
                }
            }
        }

        /// <summary>
        /// 检查患者是否重复
        /// </summary>
        public async Task<List<PatientDetailDto>> CheckDuplicatePatientsAsync(string idNumber, string phoneNumber)
        {
            var duplicates = new List<Patient>();

            if (!string.IsNullOrEmpty(idNumber))
            {
                var byIdNumber = await _patientRepository.GetByIdNumberAsync(idNumber);
                if (byIdNumber != null)
                {
                    duplicates.Add(byIdNumber);
                }
            }

            if (!string.IsNullOrEmpty(phoneNumber))
            {
                var byPhone = await _patientRepository.GetByPhoneNumberAsync(phoneNumber);
                if (byPhone != null && !duplicates.Any(p => p.Id == byPhone.Id))
                {
                    duplicates.Add(byPhone);
                }
            }

            return duplicates.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

        /// <summary>
        /// 处理身份证信息（提取生日和年龄）
        /// </summary>
        public void ProcessIdNumberInfo(Patient model)
        {
            if (!string.IsNullOrEmpty(model.IdNumber) && 
                LYBT.Shared.Utilities.Helpers.CommonHelper.CheckIdNumber(model.IdNumber))
            {
                model.BirthDate = ExtractBirthDateFromIdNumber(model.IdNumber);
                // Age字段已删除（UltraThink v2.0简化）- Age现在是计算属性
            }
        }
    }
}