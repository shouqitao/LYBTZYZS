using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.Models.MedicalCase;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.MedicalCase.Services.Interfaces;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.MedicalCase.Services
{
    /// <summary>
    /// MedicalCase模块核心业务服务实现
    /// UltraThink模块化架构：封装医疗案例模块业务逻辑，使用AutoMapper进行DTO↔Info转换
    /// </summary>
    public class MedicalCaseModuleService : IMedicalCaseModuleService
    {
        private readonly IMedicalCaseApiService _apiService;
        private readonly IMapper _mapper;
        
        public MedicalCaseModuleService(IMedicalCaseApiService apiService, IMapper mapper)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        
        #region 基础CRUD操作
        
        public async Task<ServiceResult<PagedResult<MedicalCaseInfo>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                // 转换为医疗案例专用查询DTO
                var medicalCaseQuery = new MedicalCasePagedQueryDto
                {
                    PageIndex = query.PageIndex,
                    PageSize = query.PageSize,
                    Keyword = query.Keyword,
                    SortField = query.SortField,
                    SortDirection = query.SortDirection
                };

                // UltraThink四层架构：API调用获取DTOs
                var apiResult = await _apiService.GetPagedAsync(medicalCaseQuery);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<PagedResult<MedicalCaseInfo>>.Failure(
                        apiResult.ErrorMessage ?? "获取医疗案例列表失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTOs → Infos
                var medicalCaseInfos = _mapper.Map<List<MedicalCaseInfo>>(apiResult.Data.Items);
                var result = new PagedResult<MedicalCaseInfo>(
                    medicalCaseInfos,
                    apiResult.Data.TotalCount,
                    apiResult.Data.CurrentPage,
                    apiResult.Data.PageSize);
                
                return ServiceResult<PagedResult<MedicalCaseInfo>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<MedicalCaseInfo>>.Failure($"获取医疗案例列表异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<MedicalCaseInfo>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<MedicalCaseInfo>.Failure("医疗案例ID不能为空");
                }
                
                // UltraThink四层架构：API调用获取DTO
                var apiResult = await _apiService.GetByIdAsync(id);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<MedicalCaseInfo>.Failure(
                        apiResult.ErrorMessage ?? "获取医疗案例详情失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTO → Info
                var medicalCaseInfo = _mapper.Map<MedicalCaseInfo>(apiResult.Data);
                return ServiceResult<MedicalCaseInfo>.Success(medicalCaseInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<MedicalCaseInfo>.Failure($"获取医疗案例详情异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<MedicalCaseInfo>> CreateAsync(MedicalCaseCreateInfo createInfo)
        {
            try
            {
                // 业务验证
                var validationResult = await ValidateAsync(_mapper.Map<MedicalCaseInfo>(createInfo));
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<MedicalCaseInfo>.Failure(validationResult.ErrorMessage);
                }
                
                // 检查患者是否可以创建新案例
                var canCreateResult = await CanCreateCaseForPatientAsync(createInfo.PatientId);
                if (!canCreateResult.IsSuccess || !canCreateResult.Data)
                {
                    return ServiceResult<MedicalCaseInfo>.Failure(
                        canCreateResult.ErrorMessage ?? "该患者当前不能创建新的医疗案例");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 Info → DTO
                var createDto = _mapper.Map<MedicalCaseCreateDto>(createInfo);
                
                // API调用
                var apiResult = await _apiService.CreateAsync(createDto);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<MedicalCaseInfo>.Failure(
                        apiResult.ErrorMessage ?? "创建医疗案例失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTO → Info
                var medicalCaseInfo = _mapper.Map<MedicalCaseInfo>(apiResult.Data);
                return ServiceResult<MedicalCaseInfo>.Success(medicalCaseInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<MedicalCaseInfo>.Failure($"创建医疗案例异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<MedicalCaseInfo>> UpdateAsync(MedicalCaseUpdateInfo updateInfo)
        {
            try
            {
                // 业务验证
                var validationResult = await ValidateAsync(_mapper.Map<MedicalCaseInfo>(updateInfo));
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<MedicalCaseInfo>.Failure(validationResult.ErrorMessage);
                }
                
                // 检查是否可以修改
                var canModifyResult = await CanModifyAsync(updateInfo.Id);
                if (!canModifyResult.IsSuccess || !canModifyResult.Data)
                {
                    return ServiceResult<MedicalCaseInfo>.Failure(
                        canModifyResult.ErrorMessage ?? "当前医疗案例状态不允许修改");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 Info → DTO
                var updateDto = _mapper.Map<MedicalCaseUpdateDto>(updateInfo);
                
                // API调用
                var apiResult = await _apiService.UpdateAsync(updateDto);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<MedicalCaseInfo>.Failure(
                        apiResult.ErrorMessage ?? "更新医疗案例失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTO → Info
                var medicalCaseInfo = _mapper.Map<MedicalCaseInfo>(apiResult.Data);
                return ServiceResult<MedicalCaseInfo>.Success(medicalCaseInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<MedicalCaseInfo>.Failure($"更新医疗案例异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("医疗案例ID不能为空");
                }
                
                // 检查是否可以删除
                var canDeleteResult = await CanDeleteAsync(id);
                if (!canDeleteResult.IsSuccess || !canDeleteResult.Data)
                {
                    return ServiceResult.Failure(
                        canDeleteResult.ErrorMessage ?? "当前医疗案例状态不允许删除");
                }
                
                var apiResult = await _apiService.DeleteAsync(id);
                if (!apiResult.IsSuccess)
                {
                    return ServiceResult.Failure(apiResult.ErrorMessage ?? "删除医疗案例失败");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"删除医疗案例异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 状态管理
        
        public async Task<ServiceResult> UpdateStatusAsync(Guid id, MedicalCaseStatus status, string? reason = null)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("医疗案例ID不能为空");
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
        
        public async Task<ServiceResult> StartConsultationAsync(Guid id)
        {
            try
            {
                return await UpdateStatusAsync(id, MedicalCaseStatus.InConsultation, "开始看诊");
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"开始看诊异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> CompleteConsultationAsync(Guid id, string? diagnosis = null)
        {
            try
            {
                var result = await UpdateStatusAsync(id, MedicalCaseStatus.Completed, "完成看诊");
                
                // 如果提供了诊断结果，同时更新诊断信息
                if (result.IsSuccess && !string.IsNullOrWhiteSpace(diagnosis))
                {
                    var caseResult = await GetByIdAsync(id);
                    if (caseResult.IsSuccess)
                    {
                        var updateInfo = MedicalCaseUpdateInfo.FromMedicalCaseInfo(caseResult.Data);
                        updateInfo.Diagnosis = diagnosis;
                        updateInfo.SetCompleted(diagnosis);
                        
                        await UpdateAsync(updateInfo);
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"完成看诊异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> CancelAsync(Guid id, string reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return ServiceResult.Failure("取消原因不能为空");
                }
                
                return await UpdateStatusAsync(id, MedicalCaseStatus.Cancelled, reason);
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"取消医疗案例异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<int>> BatchUpdateStatusAsync(IEnumerable<Guid> ids, MedicalCaseStatus status, string? reason = null)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return ServiceResult<int>.Failure("医疗案例ID列表不能为空");
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
        
        public async Task<ServiceResult<PagedResult<MedicalCaseInfo>>> SearchAsync(PagedQueryBaseDto request)
        {
            try
            {
                // 使用GetPagedAsync实现搜索功能
                return await GetPagedAsync(request);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<MedicalCaseInfo>>.Failure($"搜索医疗案例异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<MedicalCaseInfo>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                {
                    return ServiceResult<IEnumerable<MedicalCaseInfo>>.Failure("患者ID不能为空");
                }
                
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 1000, // 获取患者的所有案例
                    Keyword = patientId.ToString()
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<IEnumerable<MedicalCaseInfo>>.Failure(result.ErrorMessage);
                }
                
                var patientCases = result.Data.Items.Where(c => c.PatientId == patientId);
                return ServiceResult<IEnumerable<MedicalCaseInfo>>.Success(patientCases);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<MedicalCaseInfo>>.Failure($"根据患者ID获取医疗案例异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<MedicalCaseInfo>>> GetByDoctorIdAsync(Guid doctorId)
        {
            try
            {
                if (doctorId == Guid.Empty)
                {
                    return ServiceResult<IEnumerable<MedicalCaseInfo>>.Failure("医生ID不能为空");
                }
                
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 1000, // 获取医生的所有案例
                    Keyword = doctorId.ToString()
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<IEnumerable<MedicalCaseInfo>>.Failure(result.ErrorMessage);
                }
                
                var doctorCases = result.Data.Items.Where(c => c.DoctorId == doctorId);
                return ServiceResult<IEnumerable<MedicalCaseInfo>>.Success(doctorCases);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<MedicalCaseInfo>>.Failure($"根据医生ID获取医疗案例异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<PagedResult<MedicalCaseInfo>>> GetByStatusAsync(MedicalCaseStatus status, PagedQueryBaseDto query)
        {
            try
            {
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<PagedResult<MedicalCaseInfo>>.Failure(result.ErrorMessage);
                }
                
                // 过滤指定状态的案例
                var statusCases = result.Data.Items.Where(c => c.Status == status).ToList();
                var filteredResult = new PagedResult<MedicalCaseInfo>(
                    statusCases,
                    statusCases.Count,
                    query.PageIndex,
                    query.PageSize);
                
                return ServiceResult<PagedResult<MedicalCaseInfo>>.Success(filteredResult);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<MedicalCaseInfo>>.Failure($"根据状态获取医疗案例异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<MedicalCaseInfo>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                if (startDate > endDate)
                {
                    return ServiceResult<IEnumerable<MedicalCaseInfo>>.Failure("开始日期不能大于结束日期");
                }
                
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 10000 // 获取足够多的数据进行筛选
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<IEnumerable<MedicalCaseInfo>>.Failure(result.ErrorMessage);
                }
                
                var dateRangeCases = result.Data.Items.Where(c => 
                    c.CreateTime.Date >= startDate.Date && 
                    c.CreateTime.Date <= endDate.Date);
                
                return ServiceResult<IEnumerable<MedicalCaseInfo>>.Success(dateRangeCases);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<MedicalCaseInfo>>.Failure($"根据日期范围获取医疗案例异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 验证操作
        
        public async Task<ServiceResult> ValidateAsync(MedicalCaseInfo medicalCaseInfo)
        {
            try
            {
                if (medicalCaseInfo == null)
                {
                    return ServiceResult.Failure("医疗案例信息不能为空");
                }
                
                var validationResult = medicalCaseInfo.Validate();
                if (!validationResult.IsValid)
                {
                    return ServiceResult.Failure(validationResult.ErrorMessage);
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"验证医疗案例信息异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<bool>> CanCreateCaseForPatientAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("患者ID不能为空");
                }
                
                // 检查患者是否有未完成的案例
                var hasIncompleteResult = await HasIncompleteCasesAsync(patientId);
                if (!hasIncompleteResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure(hasIncompleteResult.ErrorMessage);
                }
                
                // 如果有未完成案例，不允许创建新案例
                if (hasIncompleteResult.Data)
                {
                    return ServiceResult<bool>.Success(false);
                }
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"检查患者创建案例权限异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<bool>> CanDoctorHandleCaseAsync(Guid doctorId, Guid caseId)
        {
            try
            {
                if (doctorId == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("医生ID不能为空");
                }
                
                if (caseId == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("案例ID不能为空");
                }
                
                var caseResult = await GetByIdAsync(caseId);
                if (!caseResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure("获取案例信息失败");
                }
                
                // 检查医生是否是该案例的负责医生
                var canHandle = caseResult.Data.DoctorId == doctorId;
                
                return ServiceResult<bool>.Success(canHandle);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"检查医生处理案例权限异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 统计功能
        
        public async Task<ServiceResult<MedicalCaseStatisticsInfo>> GetStatisticsAsync()
        {
            try
            {
                // 获取所有案例进行统计
                var allCasesResult = await GetPagedAsync(new PagedQueryBaseDto 
                { 
                    PageIndex = 1, 
                    PageSize = 10000 // 获取足够多的数据进行统计
                });
                
                if (!allCasesResult.IsSuccess)
                {
                    return ServiceResult<MedicalCaseStatisticsInfo>.Failure(allCasesResult.ErrorMessage);
                }
                
                var cases = allCasesResult.Data.Items;
                
                var statistics = new MedicalCaseStatisticsInfo
                {
                    TotalCount = cases.Count,
                    RegisteredCount = cases.Count(c => c.Status == MedicalCaseStatus.Registered),
                    InConsultationCount = cases.Count(c => c.Status == MedicalCaseStatus.InConsultation),
                    CompletedCount = cases.Count(c => c.Status == MedicalCaseStatus.Completed),
                    CancelledCount = cases.Count(c => c.Status == MedicalCaseStatus.Cancelled),
                    AverageConsultationTime = CalculateAverageConsultationTime(cases),
                    StatisticsDate = DateTime.Now,
                    DiagnosisCounts = cases.Where(c => !string.IsNullOrEmpty(c.Diagnosis))
                                          .GroupBy(c => c.Diagnosis!)
                                          .ToDictionary(g => g.Key, g => g.Count()),
                    DoctorCaseCounts = cases.GroupBy(c => c.DoctorName)
                                           .ToDictionary(g => g.Key, g => g.Count())
                };
                
                return ServiceResult<MedicalCaseStatisticsInfo>.Success(statistics);
            }
            catch (Exception ex)
            {
                return ServiceResult<MedicalCaseStatisticsInfo>.Failure($"获取统计信息异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<MedicalCaseStatisticsInfo>> GetTodayStatisticsAsync()
        {
            try
            {
                var today = DateTime.Today;
                var todayCases = await GetByDateRangeAsync(today, today.AddDays(1).AddSeconds(-1));
                
                if (!todayCases.IsSuccess)
                {
                    return ServiceResult<MedicalCaseStatisticsInfo>.Failure(todayCases.ErrorMessage);
                }
                
                var cases = todayCases.Data.ToList();
                
                var statistics = new MedicalCaseStatisticsInfo
                {
                    TotalCount = cases.Count,
                    RegisteredCount = cases.Count(c => c.Status == MedicalCaseStatus.Registered),
                    InConsultationCount = cases.Count(c => c.Status == MedicalCaseStatus.InConsultation),
                    CompletedCount = cases.Count(c => c.Status == MedicalCaseStatus.Completed),
                    CancelledCount = cases.Count(c => c.Status == MedicalCaseStatus.Cancelled),
                    AverageConsultationTime = CalculateAverageConsultationTime(cases),
                    StatisticsDate = today,
                    DiagnosisCounts = cases.Where(c => !string.IsNullOrEmpty(c.Diagnosis))
                                          .GroupBy(c => c.Diagnosis!)
                                          .ToDictionary(g => g.Key, g => g.Count()),
                    DoctorCaseCounts = cases.GroupBy(c => c.DoctorName)
                                           .ToDictionary(g => g.Key, g => g.Count())
                };
                
                return ServiceResult<MedicalCaseStatisticsInfo>.Success(statistics);
            }
            catch (Exception ex)
            {
                return ServiceResult<MedicalCaseStatisticsInfo>.Failure($"获取今日统计信息异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<DoctorCaseStatisticsInfo>> GetDoctorStatisticsAsync(Guid doctorId)
        {
            try
            {
                var doctorCasesResult = await GetByDoctorIdAsync(doctorId);
                if (!doctorCasesResult.IsSuccess)
                {
                    return ServiceResult<DoctorCaseStatisticsInfo>.Failure(doctorCasesResult.ErrorMessage);
                }
                
                var cases = doctorCasesResult.Data.ToList();
                if (!cases.Any())
                {
                    return ServiceResult<DoctorCaseStatisticsInfo>.Failure("该医生没有案例记录");
                }
                
                var statistics = new DoctorCaseStatisticsInfo
                {
                    DoctorId = doctorId,
                    DoctorName = cases.First().DoctorName,
                    TotalCases = cases.Count,
                    CompletedCases = cases.Count(c => c.Status == MedicalCaseStatus.Completed),
                    InProgressCases = cases.Count(c => c.Status == MedicalCaseStatus.InConsultation),
                    AverageConsultationTime = CalculateAverageConsultationTime(cases),
                    LastCaseTime = cases.Max(c => c.CreateTime),
                    CommonDiagnoses = cases.Where(c => !string.IsNullOrEmpty(c.Diagnosis))
                                          .GroupBy(c => c.Diagnosis!)
                                          .OrderByDescending(g => g.Count())
                                          .Take(5)
                                          .Select(g => g.Key)
                                          .ToList()
                };
                
                statistics.CompletionRate = statistics.TotalCases > 0 
                    ? (decimal)statistics.CompletedCases / statistics.TotalCases * 100 
                    : 0;
                
                return ServiceResult<DoctorCaseStatisticsInfo>.Success(statistics);
            }
            catch (Exception ex)
            {
                return ServiceResult<DoctorCaseStatisticsInfo>.Failure($"获取医生统计信息异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<DiagnosisStatisticsInfo>>> GetPopularDiagnosisAsync(int count = 10)
        {
            try
            {
                var allCasesResult = await GetPagedAsync(new PagedQueryBaseDto 
                { 
                    PageIndex = 1, 
                    PageSize = 10000 
                });
                
                if (!allCasesResult.IsSuccess)
                {
                    return ServiceResult<IEnumerable<DiagnosisStatisticsInfo>>.Failure(allCasesResult.ErrorMessage);
                }
                
                var cases = allCasesResult.Data.Items.Where(c => !string.IsNullOrEmpty(c.Diagnosis)).ToList();
                var totalCases = cases.Count;
                
                var diagnosisStats = cases.GroupBy(c => c.Diagnosis!)
                                         .Select(g => new DiagnosisStatisticsInfo
                                         {
                                             Diagnosis = g.Key,
                                             Count = g.Count(),
                                             Percentage = totalCases > 0 ? (decimal)g.Count() / totalCases * 100 : 0,
                                             LastUsed = g.Max(c => c.CreateTime)
                                         })
                                         .OrderByDescending(d => d.Count)
                                         .Take(count);
                
                return ServiceResult<IEnumerable<DiagnosisStatisticsInfo>>.Success(diagnosisStats);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<DiagnosisStatisticsInfo>>.Failure($"获取热门诊断异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 业务规则验证
        
        public async Task<ServiceResult<bool>> CanModifyAsync(Guid id)
        {
            try
            {
                var caseResult = await GetByIdAsync(id);
                if (!caseResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure("获取案例信息失败");
                }
                
                // 已完成或已取消的案例不能修改
                var canModify = caseResult.Data.Status != MedicalCaseStatus.Completed && 
                               caseResult.Data.Status != MedicalCaseStatus.Cancelled;
                
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
                var caseResult = await GetByIdAsync(id);
                if (!caseResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure("获取案例信息失败");
                }
                
                // 只有已挂号状态的案例可以删除
                var canDelete = caseResult.Data.Status == MedicalCaseStatus.Registered;
                
                return ServiceResult<bool>.Success(canDelete);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"检查删除权限异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<CaseOperationHistoryInfo>>> GetOperationHistoryAsync(Guid id)
        {
            try
            {
                // 这里应该调用API获取操作历史
                // 目前返回空列表表示功能开发中
                var history = new List<CaseOperationHistoryInfo>();
                
                return ServiceResult<IEnumerable<CaseOperationHistoryInfo>>.Success(history);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<CaseOperationHistoryInfo>>.Failure($"获取操作历史异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 关联数据
        
        public async Task<ServiceResult<IEnumerable<ConsultationInfo>>> GetConsultationsAsync(Guid caseId)
        {
            try
            {
                // 这里应该调用相关API获取看诊记录
                // 目前返回空列表表示功能开发中
                var consultations = new List<ConsultationInfo>();
                
                return ServiceResult<IEnumerable<ConsultationInfo>>.Success(consultations);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<ConsultationInfo>>.Failure($"获取看诊记录异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<PrescriptionInfo>>> GetPrescriptionsAsync(Guid caseId)
        {
            try
            {
                // 这里应该调用相关API获取处方记录
                // 目前返回空列表表示功能开发中
                var prescriptions = new List<PrescriptionInfo>();
                
                return ServiceResult<IEnumerable<PrescriptionInfo>>.Success(prescriptions);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<PrescriptionInfo>>.Failure($"获取处方记录异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<bool>> HasIncompleteCasesAsync(Guid patientId)
        {
            try
            {
                var patientCasesResult = await GetByPatientIdAsync(patientId);
                if (!patientCasesResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure(patientCasesResult.ErrorMessage);
                }
                
                var hasIncomplete = patientCasesResult.Data.Any(c => 
                    c.Status == MedicalCaseStatus.Registered || 
                    c.Status == MedicalCaseStatus.InConsultation);
                
                return ServiceResult<bool>.Success(hasIncomplete);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"检查未完成案例异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 私有辅助方法
        
        private decimal CalculateAverageConsultationTime(IEnumerable<MedicalCaseInfo> cases)
        {
            var completedCases = cases.Where(c => c.Status == MedicalCaseStatus.Completed && c.CompleteTime.HasValue).ToList();
            
            if (!completedCases.Any())
                return 0;
            
            var totalMinutes = completedCases.Sum(c => (c.CompleteTime!.Value - c.CreateTime).TotalMinutes);
            return (decimal)(totalMinutes / completedCases.Count);
        }
        
        #endregion
    }
}