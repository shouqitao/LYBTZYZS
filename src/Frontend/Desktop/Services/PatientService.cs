using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Common;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Services.Adapters;
using System.Linq;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 患者服务实现
    /// </summary>
    public class PatientService : IPatientService
    {
        private readonly IApiService _apiService;
        private readonly IPatientApiService _patientApiService;

        public PatientService(IApiService apiService, IPatientApiService patientApiService)
        {
            _apiService = apiService;
            _patientApiService = patientApiService;
        }

        /// <summary>
        /// 新增患者
        /// </summary>
        public async Task<ServiceResult> AddAsync(PatientDetailDto dto)
        {
            var createDto = ApiResponseAdapter.ToPatientCreateDto(dto);
            var result = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _patientApiService.CreatePatientAsync(createDto)
            );
            return result.IsSuccess ? ServiceResult.Success() : ServiceResult.Failure(result.ErrorMessage ?? "操作失败");
        }

        /// <summary>
        /// 编辑患者
        /// </summary>
        public async Task<ServiceResult> UpdateAsync(PatientDetailDto dto)
        {
            var updateDto = ApiResponseAdapter.ToPatientUpdateDto(dto);
            var result = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _patientApiService.UpdatePatientAsync(dto.Id, updateDto)
            );
            return result.IsSuccess ? ServiceResult.Success() : ServiceResult.Failure(result.ErrorMessage ?? "操作失败");
        }

        /// <summary>
        /// 启用患者档案
        /// </summary>
        public async Task<ServiceResult> EnableAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () =>
                await _patientApiService.ToggleStatusAsync(id)
            );
        }

        /// <summary>
        /// 禁用患者档案
        /// </summary>
        public async Task<ServiceResult> DisableAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () =>
                await _patientApiService.ToggleStatusAsync(id)
            );
        }

        /// <summary>
        /// 获取患者详情
        /// </summary>
        public async Task<ServiceResult<PatientDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var apiResponse = await _patientApiService.GetPatientByIdAsync(id);
                var serviceResult = ApiResponseAdapter.ToServiceResult(apiResponse);
                
                if (serviceResult.IsSuccess && serviceResult.Data.Data != null)
                {
                    var patientDetail = ApiResponseAdapter.ToPatientDetailDto(serviceResult.Data.Data);
                    return ServiceResult<PatientDetailDto>.Success(patientDetail);
                }
                
                return ServiceResult<PatientDetailDto>.Failure(serviceResult.ErrorMessage ?? "获取患者详情失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<PatientDetailDto>.Failure($"获取患者详情失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取所有患者
        /// </summary>
        public async Task<ServiceResult<List<PatientDetailDto>>> GetAllAsync()
        {
            try
            {
                var apiResponse = await _patientApiService.GetPatientsAsync(pageIndex: 1, pageSize: 1000);
                var serviceResult = ApiResponseAdapter.ToServiceResult(apiResponse);
                
                if (serviceResult.IsSuccess && serviceResult.Data.Data != null)
                {
                    var patientDetails = ApiResponseAdapter.ToPatientDetailDtos(serviceResult.Data.Data.Items);
                    return ServiceResult<List<PatientDetailDto>>.Success(patientDetails);
                }
                
                return ServiceResult<List<PatientDetailDto>>.Failure(serviceResult.ErrorMessage ?? "获取患者列表失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<PatientDetailDto>>.Failure($"获取所有患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 分页查询患者
        /// </summary>
        public async Task<LYBT.WPF.Client.Core.Models.Common.PagedResult<PatientInfo>> GetPagedAsync(PatientPagedQueryDto query)
        {
            try
            {
                // 使用更新后的RESTful GET接口
                var apiResponse = await _patientApiService.GetPatientsAsync(
                    pageIndex: query.PageIndex,
                    pageSize: query.PageSize,
                    searchTerm: query.Keyword
                );
                var serviceResult = ApiResponseAdapter.ToServiceResult(apiResponse);
                
                if (serviceResult.IsSuccess && serviceResult.Data.Data != null)
                {
                    var patientInfos = serviceResult.Data.Data.Items.Select(dto => ConvertToPatientInfo(ApiResponseAdapter.ToPatientDetailDto(dto))).ToList();
                    return new LYBT.WPF.Client.Core.Models.Common.PagedResult<PatientInfo>
                    {
                        Items = patientInfos,
                        TotalCount = (int)serviceResult.Data.Data.TotalCount,
                        CurrentPage = serviceResult.Data.Data.CurrentPage,
                        PageSize = serviceResult.Data.Data.PageSize
                    };
                }

                return new LYBT.WPF.Client.Core.Models.Common.PagedResult<PatientInfo>
                {
                    Items = new List<PatientInfo>(),
                    TotalCount = 0,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize,
                    ErrorMessage = serviceResult.ErrorMessage ?? "获取患者列表失败"
                };
            }
            catch (Exception ex)
            {
                return new LYBT.WPF.Client.Core.Models.Common.PagedResult<PatientInfo>
                {
                    Items = new List<PatientInfo>(),
                    TotalCount = 0,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize,
                    ErrorMessage = $"分页查询患者失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 批量禁用患者 - 功能已移除
        /// </summary>
        public async Task<ServiceResult> BatchDisableAsync(List<Guid> ids)
        {
            // 批量操作接口已移除，返回失败
            await Task.CompletedTask;
            return ServiceResult.Failure("批量操作功能已禁用");
        }

        /// <summary>
        /// 批量启用患者 - 功能已移除
        /// </summary>
        public async Task<ServiceResult> BatchEnableAsync(List<Guid> ids)
        {
            // 批量操作接口已移除，返回失败
            await Task.CompletedTask;
            return ServiceResult.Failure("批量操作功能已禁用");
        }

        /// <summary>
        /// 搜索患者
        /// </summary>
        public async Task<ServiceResult<List<PatientDetailDto>>> SearchAsync(string keyword)
        {
            try
            {
                var apiResponse = await _patientApiService.GetPatientsAsync(pageIndex: 1, pageSize: 100, searchTerm: keyword);
                var serviceResult = ApiResponseAdapter.ToServiceResult(apiResponse);
                
                if (serviceResult.IsSuccess && serviceResult.Data?.Data != null)
                {
                    var patientDetails = ApiResponseAdapter.ToPatientDetailDtos(serviceResult.Data.Data.Items);
                    return ServiceResult<List<PatientDetailDto>>.Success(patientDetails);
                }
                
                return ServiceResult<List<PatientDetailDto>>.Failure(serviceResult.ErrorMessage ?? "搜索患者失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<PatientDetailDto>>.Failure($"搜索患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 导入患者数据 - 功能已移除
        /// </summary>
        public async Task<ServiceResult> ImportAsync(List<PatientDetailDto> patients)
        {
            // 导入功能已移除，返回失败
            await Task.CompletedTask;
            return ServiceResult.Failure("导入功能已禁用");
        }

        /// <summary>
        /// 导出患者数据
        /// </summary>
        public async Task<ServiceResult<List<PatientDetailDto>>> ExportAsync()
        {
            try
            {
                var apiResponse = await _patientApiService.GetPatientsAsync(pageIndex: 1, pageSize: 10000);
                var serviceResult = ApiResponseAdapter.ToServiceResult(apiResponse);
                
                if (serviceResult.IsSuccess && serviceResult.Data?.Data != null)
                {
                    var patientDetails = ApiResponseAdapter.ToPatientDetailDtos(serviceResult.Data.Data.Items);
                    return ServiceResult<List<PatientDetailDto>>.Success(patientDetails);
                }
                
                return ServiceResult<List<PatientDetailDto>>.Failure(serviceResult.ErrorMessage ?? "导出患者数据失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<PatientDetailDto>>.Failure($"导出患者数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取患者历史病历 - 功能已移除
        /// </summary>
        public async Task<ServiceResult<List<object>>> GetHistoryRecordsAsync(Guid patientId)
        {
            // Records模块已移除，返回空列表
            await Task.CompletedTask;
            return ServiceResult<List<object>>.Success(new List<object>());
        }

        /// <summary>
        /// 获取活跃患者列表
        /// </summary>
        public async Task<ServiceResult<List<PatientDetailDto>>> GetActivePatientsAsync()
        {
            try
            {
                var apiResponse = await _patientApiService.GetActivePatientsAsync();
                var serviceResult = ApiResponseAdapter.ToServiceResult(apiResponse);
                
                if (serviceResult.IsSuccess && serviceResult.Data.Data != null)
                {
                    var patientDetails = ApiResponseAdapter.ToPatientDetailDtos(serviceResult.Data.Data);
                    return ServiceResult<List<PatientDetailDto>>.Success(patientDetails);
                }
                
                return ServiceResult<List<PatientDetailDto>>.Failure(serviceResult.ErrorMessage ?? "获取活跃患者列表失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<PatientDetailDto>>.Failure($"获取活跃患者列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 查询或创建患者
        /// </summary>
        public async Task<ServiceResult<PatientDetailDto>> FindOrCreateAsync(PatientDetailDto dto)
        {
            // 先尝试查找患者，如果不存在则创建  
            var searchResult = await SearchAsync(dto.Name);
            if (searchResult.IsSuccess && searchResult.Data != null && searchResult.Data.Any())
            {
                return ServiceResult<PatientDetailDto>.Success(searchResult.Data.First());
            }
            
            // 如果找不到，创建新患者
            var createDto = ApiResponseAdapter.ToPatientCreateDto(dto);
            
            try
            {
                var apiResponse = await _patientApiService.CreatePatientAsync(createDto);
                var serviceResult = ApiResponseAdapter.ToServiceResult(apiResponse);
                
                if (serviceResult.IsSuccess && serviceResult.Data.Data != null)
                {
                    var patientDetail = ApiResponseAdapter.ToPatientDetailDto(serviceResult.Data.Data);
                    return ServiceResult<PatientDetailDto>.Success(patientDetail);
                }
                
                return ServiceResult<PatientDetailDto>.Failure(serviceResult.ErrorMessage ?? "创建患者失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<PatientDetailDto>.Failure($"创建患者失败: {ex.Message}");
            }
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
                BirthDate = dto.DateOfBirth,
                CreateTime = dto.CreateTime,
                UpdateTime = dto.UpdateTime,
                Status = dto.Status  // 直接使用CommonStatus
            };
        }

        /// <summary>
        /// 获取患者列表
        /// </summary>
        public async Task<List<PatientInfo>> GetListAsync()
        {
            try
            {
                // 使用现有的GetActivePatientsAsync方法获取启用的患者列表
                var apiResponse = await _patientApiService.GetActivePatientsAsync();
                var serviceResult = ApiResponseAdapter.ToServiceResult(apiResponse);
                
                if (serviceResult.IsSuccess && serviceResult.Data.Data != null)
                {
                    var patientDetails = ApiResponseAdapter.ToPatientDetailDtos(serviceResult.Data.Data);
                    return patientDetails.Select(ConvertToPatientInfo).ToList();
                }
                return new List<PatientInfo>();
            }
            catch (Exception ex)
            {
                throw new Exception($"获取患者列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 创建患者
        /// </summary>
        public async Task<ServiceResult<PatientDetailDto>> CreateAsync(PatientDetailDto dto)
        {
            var createDto = ApiResponseAdapter.ToPatientCreateDto(dto);
            var apiResponse = await _patientApiService.CreatePatientAsync(createDto);
            var serviceResult = ApiResponseAdapter.ToServiceResult(apiResponse);
            
            if (serviceResult.IsSuccess && serviceResult.Data.Data != null)
            {
                var patientDetail = ApiResponseAdapter.ToPatientDetailDto(serviceResult.Data.Data);
                return ServiceResult<PatientDetailDto>.Success(patientDetail);
            }
            
            return ServiceResult<PatientDetailDto>.Failure(serviceResult.ErrorMessage ?? "创建患者失败");
        }

        /// <summary>
        /// 按姓名或拼音搜索患者
        /// </summary>
        public async Task<ServiceResult<List<PatientDetailDto>>> SearchByNameOrPinYinAsync(string keyword)
        {
            try
            {
                // 使用分页查询接口，传入searchTerm参数
                var apiResponse = await _patientApiService.GetPatientsAsync(
                    pageIndex: 1,
                    pageSize: 50,
                    searchTerm: keyword
                );
                var serviceResult = ApiResponseAdapter.ToServiceResult(apiResponse);
                
                if (serviceResult.IsSuccess && serviceResult.Data?.Data != null)
                {
                    var patientDetails = ApiResponseAdapter.ToPatientDetailDtos(serviceResult.Data.Data.Items);
                    return ServiceResult<List<PatientDetailDto>>.Success(patientDetails);
                }
                
                return ServiceResult<List<PatientDetailDto>>.Failure("搜索失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<PatientDetailDto>>.Failure($"搜索患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 按电话号码搜索患者（支持后几位）
        /// </summary>
        public async Task<ServiceResult<List<PatientDetailDto>>> SearchByPhoneAsync(string phone)
        {
            try
            {
                // 使用搜索接口进行电话号码查询
                var apiResponse = await _patientApiService.GetPatientsAsync(
                    pageIndex: 1,
                    pageSize: 50,
                    searchTerm: phone
                );
                var serviceResult = ApiResponseAdapter.ToServiceResult(apiResponse);
                
                if (serviceResult.IsSuccess && serviceResult.Data?.Data != null)
                {
                    // 转换为PatientDetailDto并进行后几位过滤
                    var patientDetails = ApiResponseAdapter.ToPatientDetailDtos(serviceResult.Data.Data.Items);
                    if (phone.Length < 11)
                    {
                        patientDetails = patientDetails.Where(p => 
                            !string.IsNullOrEmpty(p.PhoneNumber) && 
                            p.PhoneNumber.EndsWith(phone)
                        ).ToList();
                    }
                    
                    return ServiceResult<List<PatientDetailDto>>.Success(patientDetails);
                }
                
                return ServiceResult<List<PatientDetailDto>>.Failure("搜索失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<PatientDetailDto>>.Failure($"按电话搜索患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 按身份证号搜索患者（支持后几位）
        /// </summary>
        public async Task<ServiceResult<List<PatientDetailDto>>> SearchByIdCardAsync(string idCard)
        {
            try
            {
                // 使用搜索接口进行身份证查询
                var apiResponse = await _patientApiService.GetPatientsAsync(
                    pageIndex: 1,
                    pageSize: 50,
                    searchTerm: idCard
                );
                var serviceResult = ApiResponseAdapter.ToServiceResult(apiResponse);
                
                if (serviceResult.IsSuccess && serviceResult.Data?.Data != null)
                {
                    // 转换为PatientDetailDto并进行后几位过滤
                    var patientDetails = ApiResponseAdapter.ToPatientDetailDtos(serviceResult.Data.Data.Items);
                    if (idCard.Length < 18)
                    {
                        patientDetails = patientDetails.Where(p => 
                            !string.IsNullOrEmpty(p.IDNumber) && 
                            p.IDNumber.ToUpper().EndsWith(idCard.ToUpper())
                        ).ToList();
                    }
                    
                    return ServiceResult<List<PatientDetailDto>>.Success(patientDetails);
                }
                
                return ServiceResult<List<PatientDetailDto>>.Failure("搜索失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<PatientDetailDto>>.Failure($"按身份证搜索患者失败: {ex.Message}");
            }
        }
    }
}