using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Prescriptions.Services.Interfaces;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Prescriptions.Services
{
    /// <summary>
    /// Prescriptions模块核心业务服务实现
    /// UltraThink模块化架构：封装处方管理模块业务逻辑，使用AutoMapper进行DTO↔Info转换
    /// </summary>
    public class PrescriptionsModuleService : IPrescriptionsModuleService
    {
        private readonly IPrescriptionApiService _apiService;
        private readonly IMapper _mapper;
        
        public PrescriptionsModuleService(IPrescriptionApiService apiService, IMapper mapper)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        
        #region 基础CRUD操作
        
        public async Task<ServiceResult<PagedResult<PrescriptionInfo>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                // 转换为处方专用查询DTO
                var prescriptionQuery = new PrescriptionPagedQueryDto
                {
                    PageIndex = query.PageIndex,
                    PageSize = query.PageSize,
                    Keyword = query.Keyword,
                    SortField = query.SortField,
                    SortDirection = query.SortDirection
                };

                // UltraThink四层架构：API调用获取DTOs
                var apiResult = await _apiService.GetPagedAsync(prescriptionQuery);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<PagedResult<PrescriptionInfo>>.Failure(
                        apiResult.ErrorMessage ?? "获取处方列表失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTOs → Infos
                var prescriptionInfos = _mapper.Map<List<PrescriptionInfo>>(apiResult.Data.Items);
                var result = new PagedResult<PrescriptionInfo>(
                    prescriptionInfos,
                    apiResult.Data.TotalCount,
                    apiResult.Data.CurrentPage,
                    apiResult.Data.PageSize);
                
                return ServiceResult<PagedResult<PrescriptionInfo>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<PrescriptionInfo>>.Failure($"获取处方列表异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<PrescriptionInfo>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<PrescriptionInfo>.Failure("处方ID不能为空");
                }
                
                // UltraThink四层架构：API调用获取DTO
                var apiResult = await _apiService.GetByIdAsync(id);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<PrescriptionInfo>.Failure(
                        apiResult.ErrorMessage ?? "获取处方详情失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTO → Info
                var prescriptionInfo = _mapper.Map<PrescriptionInfo>(apiResult.Data);
                return ServiceResult<PrescriptionInfo>.Success(prescriptionInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<PrescriptionInfo>.Failure($"获取处方详情异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<PrescriptionInfo>> CreateAsync(PrescriptionCreateInfo createInfo)
        {
            try
            {
                // 业务验证
                var validationResult = await ValidateAsync(_mapper.Map<PrescriptionInfo>(createInfo));
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<PrescriptionInfo>.Failure(validationResult.ErrorMessage);
                }
                
                // UltraThink四层架构：使用AutoMapper转换 Info → DTO
                var createDto = _mapper.Map<PrescriptionCreateDto>(createInfo);
                
                // API调用
                var apiResult = await _apiService.CreateAsync(createDto);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<PrescriptionInfo>.Failure(
                        apiResult.ErrorMessage ?? "创建处方失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTO → Info
                var prescriptionInfo = _mapper.Map<PrescriptionInfo>(apiResult.Data);
                return ServiceResult<PrescriptionInfo>.Success(prescriptionInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<PrescriptionInfo>.Failure($"创建处方异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<PrescriptionInfo>> UpdateAsync(PrescriptionUpdateInfo updateInfo)
        {
            try
            {
                // 业务验证
                var validationResult = await ValidateAsync(_mapper.Map<PrescriptionInfo>(updateInfo));
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<PrescriptionInfo>.Failure(validationResult.ErrorMessage);
                }
                
                // 检查是否可以修改
                var canModifyResult = await CanModifyAsync(updateInfo.Id);
                if (!canModifyResult.IsSuccess || !canModifyResult.Data)
                {
                    return ServiceResult<PrescriptionInfo>.Failure(
                        canModifyResult.ErrorMessage ?? "当前处方状态不允许修改");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 Info → DTO
                var updateDto = _mapper.Map<PrescriptionUpdateDto>(updateInfo);
                
                // API调用
                var apiResult = await _apiService.UpdateAsync(updateDto);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<PrescriptionInfo>.Failure(
                        apiResult.ErrorMessage ?? "更新处方失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTO → Info
                var prescriptionInfo = _mapper.Map<PrescriptionInfo>(apiResult.Data);
                return ServiceResult<PrescriptionInfo>.Success(prescriptionInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<PrescriptionInfo>.Failure($"更新处方异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("处方ID不能为空");
                }
                
                // 检查是否可以删除
                var canDeleteResult = await CanDeleteAsync(id);
                if (!canDeleteResult.IsSuccess || !canDeleteResult.Data)
                {
                    return ServiceResult.Failure(
                        canDeleteResult.ErrorMessage ?? "当前处方状态不允许删除");
                }
                
                var apiResult = await _apiService.DeleteAsync(id);
                if (!apiResult.IsSuccess)
                {
                    return ServiceResult.Failure(apiResult.ErrorMessage ?? "删除处方失败");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"删除处方异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 状态管理
        
        public async Task<ServiceResult> UpdateStatusAsync(Guid id, PrescriptionStatus status, string? reason = null)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("处方ID不能为空");
                }
                
                var apiResult = await _apiService.UpdateStatusAsync(id, status, reason);
                if (!apiResult.IsSuccess)
                {
                    return ServiceResult.Failure(apiResult.ErrorMessage ?? "更新状态失败");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"更新状态异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> CompletePrescriptionAsync(Guid id)
        {
            try
            {
                // 验证处方完整性
                var prescriptionResult = await GetByIdAsync(id);
                if (!prescriptionResult.IsSuccess)
                {
                    return ServiceResult.Failure("获取处方信息失败");
                }
                
                var prescription = prescriptionResult.Data;
                if (!prescription.Items.Any())
                {
                    return ServiceResult.Failure("处方必须包含药材才能完成");
                }
                
                return await UpdateStatusAsync(id, PrescriptionStatus.Completed, "完成处方");
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"完成处方异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> VoidPrescriptionAsync(Guid id, string reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return ServiceResult.Failure("作废原因不能为空");
                }
                
                // 注意：此处假设有Void状态，如果没有可以使用其他状态
                return await UpdateStatusAsync(id, PrescriptionStatus.Draft, $"作废: {reason}");
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"作废处方异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<int>> BatchUpdateStatusAsync(IEnumerable<Guid> ids, PrescriptionStatus status, string? reason = null)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return ServiceResult<int>.Failure("处方ID列表不能为空");
                }
                
                var apiResult = await _apiService.BatchUpdateStatusAsync(ids, status, reason);
                if (!apiResult.IsSuccess)
                {
                    return ServiceResult<int>.Failure(apiResult.ErrorMessage ?? "批量更新状态失败");
                }
                
                return ServiceResult<int>.Success(apiResult.Data);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Failure($"批量更新状态异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 查询操作
        
        public async Task<ServiceResult<PagedResult<PrescriptionInfo>>> SearchAsync(PagedQueryBaseDto request)
        {
            try
            {
                // 使用GetPagedAsync实现搜索功能
                return await GetPagedAsync(request);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<PrescriptionInfo>>.Failure($"搜索处方异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<PrescriptionInfo>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                {
                    return ServiceResult<IEnumerable<PrescriptionInfo>>.Failure("患者ID不能为空");
                }
                
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 1000, // 获取患者的所有处方
                    Keyword = patientId.ToString()
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<IEnumerable<PrescriptionInfo>>.Failure(result.ErrorMessage);
                }
                
                var patientPrescriptions = result.Data.Items.Where(p => p.PatientId == patientId);
                return ServiceResult<IEnumerable<PrescriptionInfo>>.Success(patientPrescriptions);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<PrescriptionInfo>>.Failure($"根据患者ID获取处方异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<PrescriptionInfo>>> GetByDoctorIdAsync(Guid doctorId)
        {
            try
            {
                if (doctorId == Guid.Empty)
                {
                    return ServiceResult<IEnumerable<PrescriptionInfo>>.Failure("医生ID不能为空");
                }
                
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 1000, // 获取医生的所有处方
                    Keyword = doctorId.ToString()
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<IEnumerable<PrescriptionInfo>>.Failure(result.ErrorMessage);
                }
                
                var doctorPrescriptions = result.Data.Items.Where(p => p.UserId == doctorId);
                return ServiceResult<IEnumerable<PrescriptionInfo>>.Success(doctorPrescriptions);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<PrescriptionInfo>>.Failure($"根据医生ID获取处方异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<PrescriptionInfo>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                if (medicalCaseId == Guid.Empty)
                {
                    return ServiceResult<IEnumerable<PrescriptionInfo>>.Failure("医疗案例ID不能为空");
                }
                
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 1000,
                    Keyword = medicalCaseId.ToString()
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<IEnumerable<PrescriptionInfo>>.Failure(result.ErrorMessage);
                }
                
                var casePrescriptions = result.Data.Items.Where(p => p.MedicalCaseId == medicalCaseId);
                return ServiceResult<IEnumerable<PrescriptionInfo>>.Success(casePrescriptions);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<PrescriptionInfo>>.Failure($"根据医疗案例ID获取处方异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<PagedResult<PrescriptionInfo>>> GetByStatusAsync(PrescriptionStatus status, PagedQueryBaseDto query)
        {
            try
            {
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<PagedResult<PrescriptionInfo>>.Failure(result.ErrorMessage);
                }
                
                // 过滤指定状态的处方
                var statusPrescriptions = result.Data.Items.Where(p => p.Status == status).ToList();
                var filteredResult = new PagedResult<PrescriptionInfo>(
                    statusPrescriptions,
                    statusPrescriptions.Count,
                    query.PageIndex,
                    query.PageSize);
                
                return ServiceResult<PagedResult<PrescriptionInfo>>.Success(filteredResult);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<PrescriptionInfo>>.Failure($"根据状态获取处方异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<PrescriptionInfo>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                if (startDate > endDate)
                {
                    return ServiceResult<IEnumerable<PrescriptionInfo>>.Failure("开始日期不能大于结束日期");
                }
                
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 10000 // 获取足够多的数据进行筛选
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<IEnumerable<PrescriptionInfo>>.Failure(result.ErrorMessage);
                }
                
                var dateRangePrescriptions = result.Data.Items.Where(p => 
                    p.CreateTime.Date >= startDate.Date && 
                    p.CreateTime.Date <= endDate.Date);
                
                return ServiceResult<IEnumerable<PrescriptionInfo>>.Success(dateRangePrescriptions);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<PrescriptionInfo>>.Failure($"根据日期范围获取处方异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 处方项目管理
        
        public async Task<ServiceResult<PrescriptionItemInfo>> AddPrescriptionItemAsync(Guid prescriptionId, PrescriptionItemCreateInfo itemInfo)
        {
            try
            {
                var itemValidation = await ValidatePrescriptionItemAsync(_mapper.Map<PrescriptionItemInfo>(itemInfo));
                if (!itemValidation.IsSuccess)
                {
                    return ServiceResult<PrescriptionItemInfo>.Failure(itemValidation.ErrorMessage);
                }
                
                // 转换为DTO并调用API
                var itemDto = _mapper.Map<PrescriptionItemCreateDto>(itemInfo);
                var apiResult = await _apiService.AddPrescriptionItemAsync(prescriptionId, itemDto);
                
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<PrescriptionItemInfo>.Failure(
                        apiResult.ErrorMessage ?? "添加处方项目失败");
                }
                
                var prescriptionItemInfo = _mapper.Map<PrescriptionItemInfo>(apiResult.Data);
                return ServiceResult<PrescriptionItemInfo>.Success(prescriptionItemInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<PrescriptionItemInfo>.Failure($"添加处方项目异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<PrescriptionItemInfo>> UpdatePrescriptionItemAsync(PrescriptionItemUpdateInfo itemInfo)
        {
            try
            {
                var itemValidation = await ValidatePrescriptionItemAsync(_mapper.Map<PrescriptionItemInfo>(itemInfo));
                if (!itemValidation.IsSuccess)
                {
                    return ServiceResult<PrescriptionItemInfo>.Failure(itemValidation.ErrorMessage);
                }
                
                // 转换为DTO并调用API
                var itemDto = _mapper.Map<PrescriptionItemUpdateDto>(itemInfo);
                var apiResult = await _apiService.UpdatePrescriptionItemAsync(itemDto);
                
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<PrescriptionItemInfo>.Failure(
                        apiResult.ErrorMessage ?? "更新处方项目失败");
                }
                
                var prescriptionItemInfo = _mapper.Map<PrescriptionItemInfo>(apiResult.Data);
                return ServiceResult<PrescriptionItemInfo>.Success(prescriptionItemInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<PrescriptionItemInfo>.Failure($"更新处方项目异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> DeletePrescriptionItemAsync(Guid itemId)
        {
            try
            {
                if (itemId == Guid.Empty)
                {
                    return ServiceResult.Failure("处方项目ID不能为空");
                }
                
                var apiResult = await _apiService.DeletePrescriptionItemAsync(itemId);
                if (!apiResult.IsSuccess)
                {
                    return ServiceResult.Failure(apiResult.ErrorMessage ?? "删除处方项目失败");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"删除处方项目异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<List<PrescriptionItemInfo>>> BatchAddPrescriptionItemsAsync(Guid prescriptionId, IEnumerable<PrescriptionItemCreateInfo> items)
        {
            try
            {
                var itemsList = items.ToList();
                if (!itemsList.Any())
                {
                    return ServiceResult<List<PrescriptionItemInfo>>.Failure("处方项目列表不能为空");
                }
                
                var addedItems = new List<PrescriptionItemInfo>();
                foreach (var item in itemsList)
                {
                    var result = await AddPrescriptionItemAsync(prescriptionId, item);
                    if (result.IsSuccess)
                    {
                        addedItems.Add(result.Data);
                    }
                    else
                    {
                        // 如果有失败的，回滚已添加的项目
                        foreach (var addedItem in addedItems)
                        {
                            await DeletePrescriptionItemAsync(addedItem.Id);
                        }
                        return ServiceResult<List<PrescriptionItemInfo>>.Failure(
                            $"添加处方项目失败: {result.ErrorMessage}");
                    }
                }
                
                return ServiceResult<List<PrescriptionItemInfo>>.Success(addedItems);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<PrescriptionItemInfo>>.Failure($"批量添加处方项目异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 验证操作
        
        public async Task<ServiceResult> ValidateAsync(PrescriptionInfo prescriptionInfo)
        {
            try
            {
                if (prescriptionInfo == null)
                {
                    return ServiceResult.Failure("处方信息不能为空");
                }
                
                if (prescriptionInfo.PatientId == Guid.Empty)
                {
                    return ServiceResult.Failure("患者信息不能为空");
                }
                
                if (prescriptionInfo.UserId == Guid.Empty)
                {
                    return ServiceResult.Failure("医生信息不能为空");
                }
                
                if (!prescriptionInfo.Items.Any())
                {
                    return ServiceResult.Failure("处方必须包含药材");
                }
                
                if (prescriptionInfo.DosageCount <= 0)
                {
                    return ServiceResult.Failure("服药剂数必须大于0");
                }
                
                // 验证每个药材项目
                foreach (var item in prescriptionInfo.Items)
                {
                    var itemValidation = await ValidatePrescriptionItemAsync(item);
                    if (!itemValidation.IsSuccess)
                    {
                        return ServiceResult.Failure($"药材 '{item.HerbName}': {itemValidation.ErrorMessage}");
                    }
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"验证处方信息异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> ValidatePrescriptionItemAsync(PrescriptionItemInfo itemInfo)
        {
            try
            {
                if (itemInfo == null)
                {
                    return ServiceResult.Failure("处方项目信息不能为空");
                }
                
                var validationResult = itemInfo.Validate();
                if (!validationResult.IsValid)
                {
                    return ServiceResult.Failure(validationResult.ErrorMessage);
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"验证处方项目异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<bool>> CanModifyAsync(Guid id)
        {
            try
            {
                var prescriptionResult = await GetByIdAsync(id);
                if (!prescriptionResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure("获取处方信息失败");
                }
                
                // 已完成的处方不能修改
                var canModify = prescriptionResult.Data.Status != PrescriptionStatus.Completed;
                
                return ServiceResult<bool>.Success(canModify);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"检查修改权限异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<bool>> CanDeleteAsync(Guid id)
        {
            try
            {
                var prescriptionResult = await GetByIdAsync(id);
                if (!prescriptionResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure("获取处方信息失败");
                }
                
                // 只有草稿状态的处方可以删除
                var canDelete = prescriptionResult.Data.Status == PrescriptionStatus.Draft;
                
                return ServiceResult<bool>.Success(canDelete);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"检查删除权限异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 统计功能
        
        public async Task<ServiceResult<PrescriptionStatisticsInfo>> GetStatisticsAsync()
        {
            try
            {
                // 获取所有处方进行统计
                var allPrescriptionsResult = await GetPagedAsync(new PagedQueryBaseDto 
                { 
                    PageIndex = 1, 
                    PageSize = 10000 // 获取足够多的数据进行统计
                });
                
                if (!allPrescriptionsResult.IsSuccess)
                {
                    return ServiceResult<PrescriptionStatisticsInfo>.Failure(allPrescriptionsResult.ErrorMessage);
                }
                
                var prescriptions = allPrescriptionsResult.Data.Items;
                
                var statistics = new PrescriptionStatisticsInfo
                {
                    TotalCount = prescriptions.Count,
                    DraftCount = prescriptions.Count(p => p.Status == PrescriptionStatus.Draft),
                    CompletedCount = prescriptions.Count(p => p.Status == PrescriptionStatus.Completed),
                    TotalAmount = prescriptions.Sum(p => p.TotalAmount),
                    AverageAmount = prescriptions.Any() ? prescriptions.Average(p => p.TotalAmount) : 0,
                    StatisticsDate = DateTime.Now,
                    HerbUsageCounts = GetHerbUsageCounts(prescriptions),
                    DoctorPrescriptionCounts = prescriptions.GroupBy(p => p.DoctorName)
                                                          .ToDictionary(g => g.Key, g => g.Count())
                };
                
                return ServiceResult<PrescriptionStatisticsInfo>.Success(statistics);
            }
            catch (Exception ex)
            {
                return ServiceResult<PrescriptionStatisticsInfo>.Failure($"获取统计信息异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<PrescriptionStatisticsInfo>> GetTodayStatisticsAsync()
        {
            try
            {
                var today = DateTime.Today;
                var todayPrescriptions = await GetByDateRangeAsync(today, today.AddDays(1).AddSeconds(-1));
                
                if (!todayPrescriptions.IsSuccess)
                {
                    return ServiceResult<PrescriptionStatisticsInfo>.Failure(todayPrescriptions.ErrorMessage);
                }
                
                var prescriptions = todayPrescriptions.Data.ToList();
                
                var statistics = new PrescriptionStatisticsInfo
                {
                    TotalCount = prescriptions.Count,
                    DraftCount = prescriptions.Count(p => p.Status == PrescriptionStatus.Draft),
                    CompletedCount = prescriptions.Count(p => p.Status == PrescriptionStatus.Completed),
                    TotalAmount = prescriptions.Sum(p => p.TotalAmount),
                    AverageAmount = prescriptions.Any() ? prescriptions.Average(p => p.TotalAmount) : 0,
                    StatisticsDate = today,
                    HerbUsageCounts = GetHerbUsageCounts(prescriptions),
                    DoctorPrescriptionCounts = prescriptions.GroupBy(p => p.DoctorName)
                                                          .ToDictionary(g => g.Key, g => g.Count())
                };
                
                return ServiceResult<PrescriptionStatisticsInfo>.Success(statistics);
            }
            catch (Exception ex)
            {
                return ServiceResult<PrescriptionStatisticsInfo>.Failure($"获取今日统计信息异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<DoctorPrescriptionStatisticsInfo>> GetDoctorStatisticsAsync(Guid doctorId)
        {
            try
            {
                var doctorPrescriptionsResult = await GetByDoctorIdAsync(doctorId);
                if (!doctorPrescriptionsResult.IsSuccess)
                {
                    return ServiceResult<DoctorPrescriptionStatisticsInfo>.Failure(doctorPrescriptionsResult.ErrorMessage);
                }
                
                var prescriptions = doctorPrescriptionsResult.Data.ToList();
                if (!prescriptions.Any())
                {
                    return ServiceResult<DoctorPrescriptionStatisticsInfo>.Failure("该医生没有处方记录");
                }
                
                var statistics = new DoctorPrescriptionStatisticsInfo
                {
                    DoctorId = doctorId,
                    DoctorName = prescriptions.First().DoctorName,
                    TotalPrescriptions = prescriptions.Count,
                    CompletedPrescriptions = prescriptions.Count(p => p.Status == PrescriptionStatus.Completed),
                    TotalAmount = prescriptions.Sum(p => p.TotalAmount),
                    AverageAmount = prescriptions.Average(p => p.TotalAmount),
                    LastPrescriptionTime = prescriptions.Max(p => p.CreateTime),
                    FrequentHerbs = GetFrequentHerbs(prescriptions, 5)
                };
                
                return ServiceResult<DoctorPrescriptionStatisticsInfo>.Success(statistics);
            }
            catch (Exception ex)
            {
                return ServiceResult<DoctorPrescriptionStatisticsInfo>.Failure($"获取医生统计信息异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<HerbUsageStatisticsInfo>>> GetPopularHerbsAsync(int count = 10)
        {
            try
            {
                var allPrescriptionsResult = await GetPagedAsync(new PagedQueryBaseDto 
                { 
                    PageIndex = 1, 
                    PageSize = 10000 
                });
                
                if (!allPrescriptionsResult.IsSuccess)
                {
                    return ServiceResult<IEnumerable<HerbUsageStatisticsInfo>>.Failure(allPrescriptionsResult.ErrorMessage);
                }
                
                var allItems = allPrescriptionsResult.Data.Items.SelectMany(p => p.Items).ToList();
                var totalUsage = allItems.Count;
                
                var herbStats = allItems.GroupBy(item => item.HerbName)
                                       .Select(g => new HerbUsageStatisticsInfo
                                       {
                                           HerbName = g.Key,
                                           UsageCount = g.Count(),
                                           TotalQuantity = g.Sum(item => item.Quantity),
                                           Percentage = totalUsage > 0 ? (decimal)g.Count() / totalUsage * 100 : 0,
                                           LastUsed = g.Max(item => item.CreateTime),
                                           AverageQuantity = g.Average(item => item.Quantity)
                                       })
                                       .OrderByDescending(h => h.UsageCount)
                                       .Take(count);
                
                return ServiceResult<IEnumerable<HerbUsageStatisticsInfo>>.Success(herbStats);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<HerbUsageStatisticsInfo>>.Failure($"获取热门药材异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<PrescriptionCostStatisticsInfo>> GetCostStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var prescriptions = startDate.HasValue && endDate.HasValue
                    ? await GetByDateRangeAsync(startDate.Value, endDate.Value)
                    : await GetPagedAsync(new PagedQueryBaseDto { PageIndex = 1, PageSize = 10000 });
                
                if (!prescriptions.IsSuccess)
                {
                    return ServiceResult<PrescriptionCostStatisticsInfo>.Failure(prescriptions.ErrorMessage);
                }
                
                var prescriptionsList = prescriptions.Data is IEnumerable<PrescriptionInfo> enumerable 
                    ? enumerable.ToList() 
                    : ((PagedResult<PrescriptionInfo>)prescriptions.Data).Items.ToList();
                
                if (!prescriptionsList.Any())
                {
                    return ServiceResult<PrescriptionCostStatisticsInfo>.Success(new PrescriptionCostStatisticsInfo
                    {
                        StartDate = startDate ?? DateTime.MinValue,
                        EndDate = endDate ?? DateTime.Now
                    });
                }
                
                var statistics = new PrescriptionCostStatisticsInfo
                {
                    TotalCost = prescriptionsList.Sum(p => p.TotalAmount),
                    AverageCost = prescriptionsList.Average(p => p.TotalAmount),
                    MinCost = prescriptionsList.Min(p => p.TotalAmount),
                    MaxCost = prescriptionsList.Max(p => p.TotalAmount),
                    PrescriptionCount = prescriptionsList.Count,
                    StartDate = startDate ?? prescriptionsList.Min(p => p.CreateTime),
                    EndDate = endDate ?? prescriptionsList.Max(p => p.CreateTime)
                };
                
                return ServiceResult<PrescriptionCostStatisticsInfo>.Success(statistics);
            }
            catch (Exception ex)
            {
                return ServiceResult<PrescriptionCostStatisticsInfo>.Failure($"获取费用统计异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 模板和复制功能
        
        public async Task<ServiceResult<PrescriptionInfo>> CopyPrescriptionAsync(Guid prescriptionId, Guid? newPatientId = null)
        {
            try
            {
                var originalResult = await GetByIdAsync(prescriptionId);
                if (!originalResult.IsSuccess)
                {
                    return ServiceResult<PrescriptionInfo>.Failure("获取原处方失败");
                }
                
                var original = originalResult.Data;
                var createInfo = new PrescriptionCreateInfo();
                createInfo.CopyFrom(original);
                
                if (newPatientId.HasValue)
                {
                    createInfo.PatientId = newPatientId.Value;
                    // 这里可能需要获取新患者的姓名
                }
                
                // 重置为草稿状态
                createInfo.Status = PrescriptionStatus.Draft;
                
                return await CreateAsync(createInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<PrescriptionInfo>.Failure($"复制处方异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<PrescriptionInfo>> CreateFromTemplateAsync(Guid templateId, Guid patientId, Guid doctorId)
        {
            try
            {
                // 这里需要获取验方模板信息，假设有相应的服务
                // var templateResult = await _formulaTemplateService.GetByIdAsync(templateId);
                
                // 暂时返回未实现的错误
                return ServiceResult<PrescriptionInfo>.Failure("从验方模板创建处方功能开发中");
            }
            catch (Exception ex)
            {
                return ServiceResult<PrescriptionInfo>.Failure($"从验方模板创建处方异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> SaveAsTemplateAsync(Guid prescriptionId, string templateName, string? description = null)
        {
            try
            {
                // 这里需要调用验方模板服务保存
                // 暂时返回未实现的错误
                return ServiceResult.Failure("保存为验方模板功能开发中");
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"保存为验方模板异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 业务规则验证
        
        public async Task<ServiceResult<List<HerbStockWarningInfo>>> CheckHerbStockAsync(Guid prescriptionId)
        {
            try
            {
                var prescriptionResult = await GetByIdAsync(prescriptionId);
                if (!prescriptionResult.IsSuccess)
                {
                    return ServiceResult<List<HerbStockWarningInfo>>.Failure("获取处方信息失败");
                }
                
                var warnings = new List<HerbStockWarningInfo>();
                
                // 这里需要检查药材库存，暂时返回空列表
                // foreach (var item in prescriptionResult.Data.Items)
                // {
                //     var stockResult = await _herbService.GetStockAsync(item.HerbId);
                //     if (stockResult.IsSuccess && stockResult.Data < item.Quantity)
                //     {
                //         warnings.Add(new HerbStockWarningInfo
                //         {
                //             HerbName = item.HerbName,
                //             RequiredQuantity = item.Quantity,
                //             AvailableStock = stockResult.Data,
                //             ShortageQuantity = item.Quantity - stockResult.Data,
                //             Unit = item.Unit
                //         });
                //     }
                // }
                
                return ServiceResult<List<HerbStockWarningInfo>>.Success(warnings);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbStockWarningInfo>>.Failure($"检查药材库存异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<List<HerbCompatibilityWarningInfo>>> CheckHerbCompatibilityAsync(Guid prescriptionId)
        {
            try
            {
                var prescriptionResult = await GetByIdAsync(prescriptionId);
                if (!prescriptionResult.IsSuccess)
                {
                    return ServiceResult<List<HerbCompatibilityWarningInfo>>.Failure("获取处方信息失败");
                }
                
                var warnings = new List<HerbCompatibilityWarningInfo>();
                
                // 这里需要实现药材配伍禁忌检查逻辑，暂时返回空列表
                
                return ServiceResult<List<HerbCompatibilityWarningInfo>>.Success(warnings);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbCompatibilityWarningInfo>>.Failure($"检查药材配伍异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<decimal>> CalculateTotalPriceAsync(Guid prescriptionId)
        {
            try
            {
                var prescriptionResult = await GetByIdAsync(prescriptionId);
                if (!prescriptionResult.IsSuccess)
                {
                    return ServiceResult<decimal>.Failure("获取处方信息失败");
                }
                
                var totalPrice = prescriptionResult.Data.Items.Sum(item => item.Subtotal);
                return ServiceResult<decimal>.Success(totalPrice);
            }
            catch (Exception ex)
            {
                return ServiceResult<decimal>.Failure($"计算处方总价异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<PrescriptionPrintInfo>> GetPrintInfoAsync(Guid id)
        {
            try
            {
                var prescriptionResult = await GetByIdAsync(id);
                if (!prescriptionResult.IsSuccess)
                {
                    return ServiceResult<PrescriptionPrintInfo>.Failure("获取处方信息失败");
                }
                
                var printInfo = new PrescriptionPrintInfo
                {
                    Prescription = prescriptionResult.Data,
                    PatientInfo = prescriptionResult.Data.PatientInfo,
                    DoctorInfo = prescriptionResult.Data.DoctorName,
                    ClinicInfo = "凌隐宝堂中医诊所", // 可以从配置获取
                    PrintTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    QrCodeData = $"PRESCRIPTION:{id}"
                };
                
                return ServiceResult<PrescriptionPrintInfo>.Success(printInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<PrescriptionPrintInfo>.Failure($"获取打印信息异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 关联数据
        
        public async Task<ServiceResult<IEnumerable<AvailableHerbInfo>>> GetAvailableHerbsAsync(string? keyword = null)
        {
            try
            {
                // 这里需要调用中药材服务获取可用药材
                // 暂时返回空列表
                var availableHerbs = new List<AvailableHerbInfo>();
                
                return ServiceResult<IEnumerable<AvailableHerbInfo>>.Success(availableHerbs);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<AvailableHerbInfo>>.Failure($"获取可用中药材异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<FormulaTemplateInfo>>> GetFormulaTemplatesAsync(string? keyword = null)
        {
            try
            {
                // 这里需要调用验方模板服务
                // 暂时返回空列表
                var templates = new List<FormulaTemplateInfo>();
                
                return ServiceResult<IEnumerable<FormulaTemplateInfo>>.Success(templates);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<FormulaTemplateInfo>>.Failure($"获取验方模板异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<PrescriptionInfo>>> GetHistoryPrescriptionsAsync(Guid patientId, int count = 10)
        {
            try
            {
                var patientPrescriptionsResult = await GetByPatientIdAsync(patientId);
                if (!patientPrescriptionsResult.IsSuccess)
                {
                    return ServiceResult<IEnumerable<PrescriptionInfo>>.Failure(patientPrescriptionsResult.ErrorMessage);
                }
                
                var historyPrescriptions = patientPrescriptionsResult.Data
                    .OrderByDescending(p => p.CreateTime)
                    .Take(count);
                
                return ServiceResult<IEnumerable<PrescriptionInfo>>.Success(historyPrescriptions);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<PrescriptionInfo>>.Failure($"获取历史处方异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 私有辅助方法
        
        private Dictionary<string, int> GetHerbUsageCounts(IEnumerable<PrescriptionInfo> prescriptions)
        {
            return prescriptions.SelectMany(p => p.Items)
                              .GroupBy(item => item.HerbName)
                              .ToDictionary(g => g.Key, g => g.Count());
        }
        
        private List<string> GetFrequentHerbs(IEnumerable<PrescriptionInfo> prescriptions, int count)
        {
            return prescriptions.SelectMany(p => p.Items)
                              .GroupBy(item => item.HerbName)
                              .OrderByDescending(g => g.Count())
                              .Take(count)
                              .Select(g => g.Key)
                              .ToList();
        }
        
        #endregion
    }
}