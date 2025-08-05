using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Records;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Common;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Services.Interfaces;
using System.Linq;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 患者服务实现
    /// </summary>
    public class PatientService : IPatientService
    {
        private readonly IApiService _apiService;
        private readonly IPatientsApiService _patientsApiService;

        public PatientService(IApiService apiService, IPatientsApiService patientsApiService)
        {
            _apiService = apiService;
            _patientsApiService = patientsApiService;
        }

        /// <summary>
        /// 新增患者
        /// </summary>
        public async Task<ServiceResult> AddAsync(PatientDetailDto dto)
        {
            var result = await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _patientsApiService.CreatePatientAsync(dto)
            );
            return result.IsSuccess ? ServiceResult.Success() : ServiceResult.Failure(result.ErrorMessage ?? "操作失败");
        }

        /// <summary>
        /// 编辑患者
        /// </summary>
        public async Task<ServiceResult> UpdateAsync(PatientDetailDto dto)
        {
            var result = await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _patientsApiService.UpdatePatientAsync(dto.Id, dto)
            );
            return result.IsSuccess ? ServiceResult.Success() : ServiceResult.Failure(result.ErrorMessage ?? "操作失败");
        }

        /// <summary>
        /// 启用患者档案
        /// </summary>
        public async Task<ServiceResult> EnableAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () => 
                await _patientsApiService.EnableAsync(id)
            );
        }

        /// <summary>
        /// 禁用患者档案
        /// </summary>
        public async Task<ServiceResult> DisableAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () => 
                await _patientsApiService.DisableAsync(id)
            );
        }

        /// <summary>
        /// 获取患者详情
        /// </summary>
        public async Task<ServiceResult<PatientDetailDto>> GetByIdAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _patientsApiService.GetPatientAsync(id)
            );
        }

        /// <summary>
        /// 获取所有患者
        /// </summary>
        public async Task<ServiceResult<List<PatientDetailDto>>> GetAllAsync()
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _patientsApiService.GetAllAsync()
            );
        }

        /// <summary>
        /// 分页查询患者
        /// </summary>
        public async Task<LYBT.WPF.Client.Core.Models.Common.PagedResult<PatientInfo>> GetPagedAsync(PatientPagedQueryDto query)
        {
            try
            {
                var response = await _patientsApiService.GetPagedAsync(query);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var patientInfos = response.Content.Items.Select(ConvertToPatientInfo).ToList();
                    return new LYBT.WPF.Client.Core.Models.Common.PagedResult<PatientInfo>
                    {
                        Items = patientInfos,
                        TotalCount = response.Content.TotalCount,
                        CurrentPage = response.Content.CurrentPage,
                        PageSize = response.Content.PageSize
                    };
                }

                return new LYBT.WPF.Client.Core.Models.Common.PagedResult<PatientInfo>
                {
                    Items = new List<PatientInfo>(),
                    TotalCount = 0,
                    CurrentPage = query.CurrentPage,
                    PageSize = query.PageSize,
                    ErrorMessage = "获取患者列表失败"
                };
            }
            catch (Exception ex)
            {
                return new LYBT.WPF.Client.Core.Models.Common.PagedResult<PatientInfo>
                {
                    Items = new List<PatientInfo>(),
                    TotalCount = 0,
                    CurrentPage = query.CurrentPage,
                    PageSize = query.PageSize,
                    ErrorMessage = $"分页查询患者失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 批量禁用患者
        /// </summary>
        public async Task<ServiceResult> BatchDisableAsync(List<Guid> ids)
        {
            var dto = new BatchOperationDto { Ids = ids };
            return await ApiErrorHandler.HandleApiCallAsync(async () => 
                await _patientsApiService.BatchDisableAsync(dto)
            );
        }

        /// <summary>
        /// 批量启用患者
        /// </summary>
        public async Task<ServiceResult> BatchEnableAsync(List<Guid> ids)
        {
            var dto = new BatchOperationDto { Ids = ids };
            return await ApiErrorHandler.HandleApiCallAsync(async () => 
                await _patientsApiService.BatchEnableAsync(dto)
            );
        }

        /// <summary>
        /// 搜索患者
        /// </summary>
        public async Task<ServiceResult<List<PatientDetailDto>>> SearchAsync(string keyword)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _patientsApiService.SearchAsync(keyword)
            );
        }

        /// <summary>
        /// 导入患者数据
        /// </summary>
        public async Task<ServiceResult> ImportAsync(List<PatientDetailDto> patients)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () => 
                await _patientsApiService.ImportAsync(patients)
            );
        }

        /// <summary>
        /// 导出患者数据
        /// </summary>
        public async Task<ServiceResult<List<PatientDetailDto>>> ExportAsync()
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _patientsApiService.ExportAsync()
            );
        }

        /// <summary>
        /// 获取患者历史病历
        /// </summary>
        public async Task<ServiceResult<List<RecordDto>>> GetHistoryRecordsAsync(Guid patientId)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _patientsApiService.GetHistoryAsync(patientId)
            );
        }

        /// <summary>
        /// 获取活跃患者列表
        /// </summary>
        public async Task<ServiceResult<List<PatientDetailDto>>> GetActivePatientsAsync()
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _patientsApiService.GetActivePatientsAsync()
            );
        }

        /// <summary>
        /// 查询或创建患者
        /// </summary>
        public async Task<ServiceResult<PatientDetailDto>> FindOrCreateAsync(PatientDetailDto dto)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _patientsApiService.FindOrCreateAsync(dto)
            );
        }

        /// <summary>
        /// 快速搜索患者（根据关键词）
        /// </summary>
        public async Task<ServiceResult<List<PatientDetailDto>>> QuickSearchAsync(string keyword)
        {
            return await SearchAsync(keyword);
        }

        /// <summary>
        /// 转换PatientDetailDto到PatientInfo
        /// </summary>
        private PatientInfo ConvertToPatientInfo(PatientDetailDto dto)
        {
            return new PatientInfo
            {
                Id = dto.Id,
                Name = dto.Name,
                Gender = dto.Gender,
                Age = dto.Age,
                PhoneNumber = dto.PhoneNumber,
                IdNumber = dto.IDNumber,  // 注意大小写
                Address = dto.Address,
                AllergyHistory = dto.AllergyHistory,
                BirthDate = dto.BirthDate,
                CreateTime = dto.CreateTime,
                UpdateTime = dto.UpdateTime,
                Status = dto.IsActive ? PatientStatus.Active : PatientStatus.Inactive  // 根据IsActive设置状态
            };
        }

        /// <summary>
        /// 获取患者列表
        /// </summary>
        public async Task<List<PatientInfo>> GetListAsync()
        {
            try
            {
                // 模拟获取患者列表
                await Task.Delay(300);
                var patientInfos = new List<PatientInfo>
                {
                    new PatientInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "张三",
                        Gender = Gender.Male,
                        Age = 35,
                        PhoneNumber = "13800138001",
                        IdNumber = "110101198801010001"
                    },
                    new PatientInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "李四",
                        Gender = Gender.Female,
                        Age = 28,
                        PhoneNumber = "13800138002",
                        IdNumber = "110101199502020002"
                    }
                };
                return patientInfos;
            }
            catch (Exception ex)
            {
                throw new Exception($"获取患者列表失败: {ex.Message}", ex);
            }
        }
    }
}