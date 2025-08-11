using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.WPF.Client.BusinessModules.Shared;

namespace LYBT.WPF.Client.BusinessModules.Patients.Services
{
    /// <summary>
    /// 共享患者服务实现
    /// 提供跨工作台的患者管理功能
    /// </summary>
    public class SharedPatientService : ISharedPatientService
    {
        private readonly IPatientService _patientService;

        public SharedPatientService(IPatientService patientService)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
        }

        /// <summary>
        /// 创建新患者档案
        /// </summary>
        public async Task<ServiceResult<PatientDetailDto>> CreatePatientAsync(PatientDetailDto dto)
        {
            return await _patientService.CreateAsync(dto);
        }

        /// <summary>
        /// 根据ID获取患者信息
        /// </summary>
        public async Task<ServiceResult<PatientDetailDto>> GetPatientAsync(Guid patientId)
        {
            return await _patientService.GetByIdAsync(patientId);
        }

        /// <summary>
        /// 快速搜索患者
        /// </summary>
        public async Task<ServiceResult<List<PatientDetailDto>>> QuickSearchAsync(string keyword)
        {
            return await _patientService.QuickSearchAsync(keyword);
        }

        /// <summary>
        /// 获取活跃患者列表
        /// </summary>
        public async Task<ServiceResult<List<PatientDetailDto>>> GetActivePatientsAsync(int limit = 20)
        {
            var result = await _patientService.GetActivePatientsAsync();
            if (result.IsSuccess && result.Data != null && result.Data.Count > limit)
            {
                result.Data = result.Data.Take(limit).ToList();
            }
            return result;
        }

        /// <summary>
        /// 更新患者基本信息
        /// </summary>
        public async Task<ServiceResult> UpdatePatientBasicInfoAsync(PatientDetailDto dto)
        {
            return await _patientService.UpdateAsync(dto);
        }

        /// <summary>
        /// 查找或创建患者
        /// </summary>
        public async Task<ServiceResult<PatientDetailDto>> FindOrCreatePatientAsync(PatientDetailDto dto)
        {
            return await _patientService.FindOrCreateAsync(dto);
        }

        /// <summary>
        /// 验证患者档案是否完整
        /// </summary>
        public async Task<ServiceResult<bool>> ValidatePatientProfileAsync(Guid patientId)
        {
            var patientResult = await _patientService.GetByIdAsync(patientId);
            if (!patientResult.IsSuccess || patientResult.Data == null)
            {
                return new ServiceResult<bool> 
                { 
                    IsSuccess = false, 
                    Data = false,
                    ErrorMessage = "患者不存在" 
                };
            }

            var patient = patientResult.Data;
            
            // 验证必填字段
            bool isComplete = !string.IsNullOrEmpty(patient.Name) &&
                             !string.IsNullOrEmpty(patient.PhoneNumber) &&
                             patient.Gender != null &&
                             patient.Age > 0;

            return new ServiceResult<bool> 
            { 
                IsSuccess = true, 
                Data = isComplete,
                ErrorMessage = isComplete ? null : "患者档案信息不完整"
            };
        }

        /// <summary>
        /// 获取患者最后就诊信息
        /// </summary>
        public async Task<ServiceResult<object>> GetLastVisitInfoAsync(Guid patientId)
        {
            // TODO: 需要与Consultation模块集成后实现
            // 当前返回模拟数据
            await Task.CompletedTask;
            
            return new ServiceResult<object>
            {
                IsSuccess = true,
                Data = new
                {
                    PatientId = patientId,
                    LastVisitDate = DateTime.Now.AddDays(-7),
                    ChiefComplaint = "感冒发热",
                    Diagnosis = "风寒感冒",
                    Treatment = "麻黄汤加减"
                }
            };
        }
    }
}