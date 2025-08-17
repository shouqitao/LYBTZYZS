using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Patients.Services.Interfaces;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Services
{
    /// <summary>
    /// Patient模块核心业务服务实现
    /// UltraThink模块化架构：封装模块业务逻辑，使用AutoMapper进行DTO↔Info转换
    /// </summary>
    public class PatientModuleService : IPatientModuleService
    {
        private readonly IPatientApiService _apiService;
        private readonly IMapper _mapper;
        
        public PatientModuleService(IPatientApiService apiService, IMapper mapper)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        
        #region 基础CRUD操作
        
        public async Task<ServiceResult<PagedResult<PatientInfo>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                // 转换为患者专用查询DTO
                var patientQuery = new PatientPagedQueryDto
                {
                    PageIndex = query.PageIndex,
                    PageSize = query.PageSize,
                    Keyword = query.Keyword,
                    SortField = query.SortField,
                    SortDirection = query.SortDirection
                };

                // UltraThink四层架构：API调用获取DTOs
                var apiResult = await _apiService.GetPagedAsync(patientQuery);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<PagedResult<PatientInfo>>.Failure(
                        apiResult.ErrorMessage ?? "获取患者列表失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTOs → Infos
                var patientInfos = _mapper.Map<List<PatientInfo>>(apiResult.Data.Items);
                var result = new PagedResult<PatientInfo>(
                    patientInfos,
                    apiResult.Data.TotalCount,
                    apiResult.Data.CurrentPage,
                    apiResult.Data.PageSize);
                
                return ServiceResult<PagedResult<PatientInfo>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<PatientInfo>>.Failure($"获取患者列表异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<PatientInfo>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<PatientInfo>.Failure("患者ID不能为空");
                }
                
                // UltraThink四层架构：API调用获取DTO
                var apiResult = await _apiService.GetByIdAsync(id);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<PatientInfo>.Failure(
                        apiResult.ErrorMessage ?? "获取患者详情失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTO → Info
                var patientInfo = _mapper.Map<PatientInfo>(apiResult.Data);
                return ServiceResult<PatientInfo>.Success(patientInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<PatientInfo>.Failure($"获取患者详情异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<PatientInfo>> CreateAsync(PatientCreateInfo createInfo)
        {
            try
            {
                // 业务验证
                var validationResult = await ValidateAsync(_mapper.Map<PatientInfo>(createInfo));
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<PatientInfo>.Failure(validationResult.ErrorMessage);
                }
                
                // 检查电话号码是否已存在
                if (!string.IsNullOrEmpty(createInfo.PhoneNumber))
                {
                    var phoneExistsResult = await IsPhoneExistsAsync(createInfo.PhoneNumber);
                    if (phoneExistsResult.IsSuccess && phoneExistsResult.Data)
                    {
                        return ServiceResult<PatientInfo>.Failure("该电话号码已被使用");
                    }
                }
                
                // 检查身份证号是否已存在
                if (!string.IsNullOrEmpty(createInfo.IdCard))
                {
                    var idCardExistsResult = await IsIdCardExistsAsync(createInfo.IdCard);
                    if (idCardExistsResult.IsSuccess && idCardExistsResult.Data)
                    {
                        return ServiceResult<PatientInfo>.Failure("该身份证号已被使用");
                    }
                }
                
                // UltraThink四层架构：使用AutoMapper转换 Info → DTO
                var createDto = _mapper.Map<PatientCreateDto>(createInfo);
                
                // API调用
                var apiResult = await _apiService.CreateAsync(createDto);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<PatientInfo>.Failure(
                        apiResult.ErrorMessage ?? "创建患者失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTO → Info
                var patientInfo = _mapper.Map<PatientInfo>(apiResult.Data);
                return ServiceResult<PatientInfo>.Success(patientInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<PatientInfo>.Failure($"创建患者异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<PatientInfo>> UpdateAsync(PatientUpdateInfo updateInfo)
        {
            try
            {
                // 业务验证
                var validationResult = await ValidateAsync(_mapper.Map<PatientInfo>(updateInfo));
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<PatientInfo>.Failure(validationResult.ErrorMessage);
                }
                
                // 检查电话号码是否已被其他患者使用
                if (!string.IsNullOrEmpty(updateInfo.PhoneNumber))
                {
                    var phoneExistsResult = await IsPhoneExistsAsync(updateInfo.PhoneNumber, updateInfo.Id);
                    if (phoneExistsResult.IsSuccess && phoneExistsResult.Data)
                    {
                        return ServiceResult<PatientInfo>.Failure("该电话号码已被其他患者使用");
                    }
                }
                
                // 检查身份证号是否已被其他患者使用
                if (!string.IsNullOrEmpty(updateInfo.IdCard))
                {
                    var idCardExistsResult = await IsIdCardExistsAsync(updateInfo.IdCard, updateInfo.Id);
                    if (idCardExistsResult.IsSuccess && idCardExistsResult.Data)
                    {
                        return ServiceResult<PatientInfo>.Failure("该身份证号已被其他患者使用");
                    }
                }
                
                // UltraThink四层架构：使用AutoMapper转换 Info → DTO
                var updateDto = _mapper.Map<PatientUpdateDto>(updateInfo);
                
                // API调用
                var apiResult = await _apiService.UpdateAsync(updateDto);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<PatientInfo>.Failure(
                        apiResult.ErrorMessage ?? "更新患者失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTO → Info
                var patientInfo = _mapper.Map<PatientInfo>(apiResult.Data);
                return ServiceResult<PatientInfo>.Success(patientInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<PatientInfo>.Failure($"更新患者异常: {ex.Message}");
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
                
                var apiResult = await _apiService.DeleteAsync(id);
                if (!apiResult.IsSuccess)
                {
                    return ServiceResult.Failure(apiResult.ErrorMessage ?? "删除患者失败");
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
        
        public async Task<ServiceResult<PagedResult<PatientInfo>>> SearchPatientsAsync(PagedQueryBaseDto request)
        {
            try
            {
                // 使用GetPagedAsync实现搜索功能
                return await GetPagedAsync(request);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<PatientInfo>>.Failure($"搜索患者异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<PatientInfo>>> SearchByKeywordAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return ServiceResult<IEnumerable<PatientInfo>>.Success(new List<PatientInfo>());
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
                    return ServiceResult<IEnumerable<PatientInfo>>.Failure(result.ErrorMessage);
                }
                
                return ServiceResult<IEnumerable<PatientInfo>>.Success(result.Data.Items);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<PatientInfo>>.Failure($"关键字搜索患者异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> ValidateAsync(PatientInfo patientInfo)
        {
            try
            {
                if (patientInfo == null)
                {
                    return ServiceResult.Failure("患者信息不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(patientInfo.Name))
                {
                    return ServiceResult.Failure("患者姓名不能为空");
                }
                
                if (patientInfo.Name.Length > 50)
                {
                    return ServiceResult.Failure("患者姓名长度不能超过50个字符");
                }
                
                if (patientInfo.Age < 0 || patientInfo.Age > 150)
                {
                    return ServiceResult.Failure("年龄必须在0到150之间");
                }
                
                if (!string.IsNullOrEmpty(patientInfo.PhoneNumber) && patientInfo.PhoneNumber.Length > 20)
                {
                    return ServiceResult.Failure("电话号码长度不能超过20个字符");
                }
                
                if (!string.IsNullOrEmpty(patientInfo.IdCard) && patientInfo.IdCard.Length > 18)
                {
                    return ServiceResult.Failure("身份证号长度不能超过18个字符");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"验证患者信息异常: {ex.Message}");
            }
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
                    p.IdCard == idCard && 
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
                var apiResult = await _apiService.EnableAsync(id);
                if (!apiResult.IsSuccess)
                {
                    return ServiceResult.Failure(apiResult.ErrorMessage ?? "启用患者失败");
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
                var apiResult = await _apiService.DisableAsync(id);
                if (!apiResult.IsSuccess)
                {
                    return ServiceResult.Failure(apiResult.ErrorMessage ?? "禁用患者失败");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"禁用患者异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 统计查询
        
        public async Task<ServiceResult<PatientStatisticsInfo>> GetStatisticsAsync()
        {
            try
            {
                // 获取患者总数据进行统计
                var allPatientsResult = await GetPagedAsync(new PagedQueryBaseDto 
                { 
                    PageIndex = 1, 
                    PageSize = 10000 // 获取足够多的数据进行统计
                });
                
                if (!allPatientsResult.IsSuccess)
                {
                    return ServiceResult<PatientStatisticsInfo>.Failure(allPatientsResult.ErrorMessage);
                }
                
                var patients = allPatientsResult.Data.Items;
                var now = DateTime.Now;
                var thisMonthStart = new DateTime(now.Year, now.Month, 1);
                
                var statistics = new PatientStatisticsInfo
                {
                    TotalCount = patients.Count,
                    ActiveCount = patients.Count(p => p.IsActive),
                    InactiveCount = patients.Count(p => !p.IsActive),
                    NewThisMonthCount = patients.Count(p => p.CreateTime >= thisMonthStart),
                    MaleCount = patients.Count(p => p.Gender == LYBT.Shared.Models.Enums.Gender.Male),
                    FemaleCount = patients.Count(p => p.Gender == LYBT.Shared.Models.Enums.Gender.Female),
                    AgeGroupCounts = new Dictionary<string, int>
                    {
                        ["0-18岁"] = patients.Count(p => p.Age <= 18),
                        ["19-35岁"] = patients.Count(p => p.Age >= 19 && p.Age <= 35),
                        ["36-55岁"] = patients.Count(p => p.Age >= 36 && p.Age <= 55),
                        ["56-70岁"] = patients.Count(p => p.Age >= 56 && p.Age <= 70),
                        ["70岁以上"] = patients.Count(p => p.Age > 70)
                    }
                };
                
                return ServiceResult<PatientStatisticsInfo>.Success(statistics);
            }
            catch (Exception ex)
            {
                return ServiceResult<PatientStatisticsInfo>.Failure($"获取患者统计信息异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<PatientInfo>>> GetRecentActiveAsync(int count = 10)
        {
            try
            {
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = count * 2, // 获取更多数据以便筛选活跃患者
                    SortField = "UpdateTime",
                    SortDirection = "DESC"
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<IEnumerable<PatientInfo>>.Failure(result.ErrorMessage);
                }
                
                var recentActive = result.Data.Items
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.UpdateTime ?? p.CreateTime)
                    .Take(count);
                
                return ServiceResult<IEnumerable<PatientInfo>>.Success(recentActive);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<PatientInfo>>.Failure($"获取最近活跃患者异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 导入导出功能
        
        public async Task<ServiceResult<IEnumerable<PatientInfo>>> ImportAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return ServiceResult<IEnumerable<PatientInfo>>.Failure("文件路径不能为空");
                }
                
                // TODO: 实现实际的导入逻辑
                // 这里是预留功能，返回空列表表示功能开发中
                return ServiceResult<IEnumerable<PatientInfo>>.Success(new List<PatientInfo>());
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<PatientInfo>>.Failure($"导入患者数据异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> ExportAsync(IEnumerable<Guid> patientIds, string filePath)
        {
            try
            {
                if (patientIds == null || !patientIds.Any())
                {
                    return ServiceResult.Failure("导出的患者ID列表不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return ServiceResult.Failure("导出文件路径不能为空");
                }
                
                // TODO: 实现实际的导出逻辑
                // 这里是预留功能，返回成功表示功能开发中
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"导出患者数据异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> GenerateImportTemplateAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return ServiceResult.Failure("模板文件路径不能为空");
                }
                
                // TODO: 实现实际的模板生成逻辑
                // 这里是预留功能，返回成功表示功能开发中
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"生成导入模板异常: {ex.Message}");
            }
        }
        
        #endregion
    }
}