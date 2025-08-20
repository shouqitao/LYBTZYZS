using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Modules.Patients.Api;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Services
{
    /// <summary>
    /// Patient模块核心业务服务实现
    /// UltraThink v2.0架构：直接使用DTO，移除Info层转换逻辑
    /// </summary>
    public class PatientModuleService
    {
        private readonly IPatientApi _apiService;
        private readonly IMapper _mapper;
        
        public PatientModuleService(IPatientApi apiService, IMapper mapper)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        
        #region 基础CRUD操作
        
        public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                // UltraThink v2.0: 直接使用API调用获取DTOs
                var apiResponse = await _apiService.GetPatientsAsync(
                    query.PageIndex,
                    query.PageSize,
                    query.Keyword);
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    return ServiceResult<PagedResult<PatientDto>>.Failure("获取患者列表失败");
                }
                
                // UltraThink v2.0: 直接使用DTO，无需映射
                var pagedData = apiResponse.Content;
                var result = new PagedResult<PatientDto>(
                    pagedData.Items.ToList(),
                    pagedData.TotalCount,
                    pagedData.CurrentPage,
                    pagedData.PageSize);
                
                return ServiceResult<PagedResult<PatientDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<PatientDto>>.Failure($"获取患者列表异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<PatientDto>.Failure("患者ID不能为空");
                }
                
                // UltraThink v2.0: API调用直接获取DTO
                var apiResponse = await _apiService.GetPatientByIdAsync(id);
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    return ServiceResult<PatientDto>.Failure("获取患者详情失败");
                }
                
                // UltraThink v2.0: 直接使用DTO，无需映射
                return ServiceResult<PatientDto>.Success(apiResponse.Content);
            }
            catch (Exception ex)
            {
                return ServiceResult<PatientDto>.Failure($"获取患者详情异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto createDto)
        {
            try
            {
                // UltraThink v2.0: 直接使用CreateDto进行业务验证
                var validationResult = await ValidateCreateDtoAsync(createDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<PatientDto>.Failure(validationResult.ErrorMessage);
                }
                
                // 检查电话号码是否已存在
                if (!string.IsNullOrEmpty(createDto.PhoneNumber))
                {
                    var phoneExistsResult = await IsPhoneExistsAsync(createDto.PhoneNumber);
                    if (phoneExistsResult.IsSuccess && phoneExistsResult.Data)
                    {
                        return ServiceResult<PatientDto>.Failure("该电话号码已被使用");
                    }
                }
                
                // 检查身份证号是否已存在
                if (!string.IsNullOrEmpty(createDto.IDNumber))
                {
                    var idCardExistsResult = await IsIdCardExistsAsync(createDto.IDNumber);
                    if (idCardExistsResult.IsSuccess && idCardExistsResult.Data)
                    {
                        return ServiceResult<PatientDto>.Failure("该身份证号已被使用");
                    }
                }
                
                // API调用
                var apiResponse = await _apiService.CreatePatientAsync(createDto);
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    return ServiceResult<PatientDto>.Failure("创建患者失败");
                }
                
                // UltraThink v2.0: 直接使用DTO，无需映射
                return ServiceResult<PatientDto>.Success(apiResponse.Content);
            }
            catch (Exception ex)
            {
                return ServiceResult<PatientDto>.Failure($"创建患者异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<PatientDto>> UpdateAsync(PatientUpdateDto updateDto)
        {
            try
            {
                // UltraThink v2.0: 直接使用UpdateDto进行业务验证
                var validationResult = await ValidateUpdateDtoAsync(updateDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<PatientDto>.Failure(validationResult.ErrorMessage);
                }
                
                // 检查电话号码是否已被其他患者使用
                if (!string.IsNullOrEmpty(updateDto.PhoneNumber))
                {
                    var phoneExistsResult = await IsPhoneExistsAsync(updateDto.PhoneNumber, updateDto.Id);
                    if (phoneExistsResult.IsSuccess && phoneExistsResult.Data)
                    {
                        return ServiceResult<PatientDto>.Failure("该电话号码已被其他患者使用");
                    }
                }
                
                // 检查身份证号是否已被其他患者使用
                if (!string.IsNullOrEmpty(updateDto.IDNumber))
                {
                    var idCardExistsResult = await IsIdCardExistsAsync(updateDto.IDNumber, updateDto.Id);
                    if (idCardExistsResult.IsSuccess && idCardExistsResult.Data)
                    {
                        return ServiceResult<PatientDto>.Failure("该身份证号已被其他患者使用");
                    }
                }
                
                // API调用
                var apiResponse = await _apiService.UpdatePatientAsync(updateDto.Id, updateDto);
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    return ServiceResult<PatientDto>.Failure("更新患者失败");
                }
                
                // UltraThink v2.0: 直接使用DTO，无需映射
                return ServiceResult<PatientDto>.Success(apiResponse.Content);
            }
            catch (Exception ex)
            {
                return ServiceResult<PatientDto>.Failure($"更新患者异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("患者ID不能为空");
                }
                
                var apiResponse = await _apiService.DeletePatientAsync(id);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    return ServiceResult.Failure("删除患者失败");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"删除患者异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 业务特定操作
        
        public async Task<ServiceResult<PagedResult<PatientDto>>> SearchPatientsAsync(PagedQueryBaseDto request)
        {
            try
            {
                // 使用GetPagedAsync实现搜索功能
                return await GetPagedAsync(request);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<PatientDto>>.Failure($"搜索患者异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<PatientDto>>> SearchByKeywordAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return ServiceResult<IEnumerable<PatientDto>>.Success(new List<PatientDto>());
                }
                
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 50, // 限制搜索结果数量
                    Keyword = keyword
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<IEnumerable<PatientDto>>.Failure(result.ErrorMessage);
                }
                
                return ServiceResult<IEnumerable<PatientDto>>.Success(result.Data.Items);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<PatientDto>>.Failure($"关键字搜索患者异常: {ex.Message}");
            }
        }
        
        // UltraThink v2.0: 简化验证方法 - 移除冗余的通用验证，合并Create/Update验证逻辑
        private async Task<ServiceResult> ValidateCreateDtoAsync(PatientCreateDto createDto)
        {
            if (createDto == null) return ServiceResult.Failure("创建患者信息不能为空");
            if (string.IsNullOrWhiteSpace(createDto.Name)) return ServiceResult.Failure("患者姓名不能为空");
            if (createDto.Name.Length > 50) return ServiceResult.Failure("患者姓名长度不能超过50个字符");
            
            // UltraThink v2.0: Age是计算属性，不验证存储值
            // 验证出生日期的合理性
            if (createDto.DateOfBirth.HasValue && createDto.DateOfBirth.Value > DateTime.Today)
            {
                return ServiceResult.Failure("出生日期不能晚于今天");
            }
            
            return ServiceResult.Success();
        }
        
        private async Task<ServiceResult> ValidateUpdateDtoAsync(PatientUpdateDto updateDto)
        {
            if (updateDto == null) return ServiceResult.Failure("更新患者信息不能为空");
            if (string.IsNullOrWhiteSpace(updateDto.Name)) return ServiceResult.Failure("患者姓名不能为空");
            if (updateDto.Name.Length > 50) return ServiceResult.Failure("患者姓名长度不能超过50个字符");
            
            // UltraThink v2.0: Age是计算属性，不验证存储值
            // 验证出生日期的合理性
            if (updateDto.DateOfBirth.HasValue && updateDto.DateOfBirth.Value > DateTime.Today)
            {
                return ServiceResult.Failure("出生日期不能晚于今天");
            }
            
            return ServiceResult.Success();
        }
        
        public async Task<ServiceResult<bool>> IsPhoneExistsAsync(string phone, Guid? excludeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phone))
                {
                    return ServiceResult<bool>.Success(false);
                }
                
                // 这里应该调用API检查电话号码是否存在
                // 目前模拟实现，实际应该有专门的API
                var searchResult = await SearchByKeywordAsync(phone);
                if (!searchResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure(searchResult.ErrorMessage);
                }
                
                var exists = searchResult.Data.Any(p => 
                    p.PhoneNumber == phone && 
                    (excludeId == null || p.Id != excludeId.Value));
                
                return ServiceResult<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"检查电话号码异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<bool>> IsIdCardExistsAsync(string idCard, Guid? excludeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idCard))
                {
                    return ServiceResult<bool>.Success(false);
                }
                
                // 这里应该调用API检查身份证号是否存在
                // 目前模拟实现，实际应该有专门的API
                var searchResult = await SearchByKeywordAsync(idCard);
                if (!searchResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure(searchResult.ErrorMessage);
                }
                
                var exists = searchResult.Data.Any(p => 
                    p.IdNumber == idCard && 
                    (excludeId == null || p.Id != excludeId.Value));
                
                return ServiceResult<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"检查身份证号异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 状态管理
        
        public async Task<ServiceResult> EnableAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("患者ID不能为空");
                }
                
                // 调用API的启用接口
                var apiResponse = await _apiService.ToggleStatusAsync(id);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    return ServiceResult.Failure("启用患者失败");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"启用患者异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> DisableAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("患者ID不能为空");
                }
                
                // 调用API的禁用接口
                var apiResponse = await _apiService.ToggleStatusAsync(id);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    return ServiceResult.Failure("禁用患者失败");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"禁用患者异常: {ex.Message}");
            }
        }
        
        #endregion
        
        // UltraThink v2.0: 移除统计查询功能 - 删除过度设计的统计功能
        
        // UltraThink v2.0: 移除导入导出功能 - 删除过度设计的导入导出功能
    }
}