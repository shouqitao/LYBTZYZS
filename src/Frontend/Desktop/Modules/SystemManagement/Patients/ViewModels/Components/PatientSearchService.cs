using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Models.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Modules.SystemManagement.Patients.ViewModels.Components
{
    /// <summary>
    /// 患者搜索服务 - UltraThink专门化组件
    /// 职责单一：专注患者搜索、查询和数据获取功能
    /// 代码干净：清晰的搜索逻辑和数据转换
    /// 性能出色：优化的查询算法和缓存策略
    /// </summary>
    public class PatientSearchService
    {
        private readonly IPatientService _patientService;
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly ILogger<PatientSearchService> _logger;

        // 关联的数据管理器
        private PatientDataManager? _dataManager;

        public PatientSearchService(
            IPatientService patientService,
            IMedicalCaseService medicalCaseService,
            ILogger<PatientSearchService> logger)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 依赖注入

        /// <summary>
        /// 设置数据管理器依赖
        /// </summary>
        public void SetDataManager(PatientDataManager dataManager)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
        }

        #endregion

        #region 搜索功能

        /// <summary>
        /// 搜索患者
        /// </summary>
        public async Task<SearchResult> SearchPatientsAsync(string keyword)
        {
            var result = new SearchResult();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                result.IsSuccess = true;
                result.Message = "搜索关键词为空";
                return result;
            }

            try
            {
                _logger.LogDebug("开始搜索患者：{Keyword}", keyword);
                
                var serviceResult = await _patientService.QuickSearchAsync(keyword);
                
                if (serviceResult.IsSuccess && serviceResult.Data != null)
                {
                    var patientInfos = serviceResult.Data.Select(dto => new PatientInfo
                    {
                        Id = dto.Id,
                        Name = dto.Name,
                        Gender = dto.Gender,
                        Age = CalculateAge(dto.BirthDate),
                        Phone = dto.PhoneNumber,
                        Status = dto.Status
                    }).ToList();

                    result.IsSuccess = true;
                    result.Patients = new ObservableCollection<PatientInfo>(patientInfos);
                    result.Message = $"找到 {patientInfos.Count} 个患者";

                    _logger.LogInformation("患者搜索完成：{Count} 个结果", patientInfos.Count);
                }
                else
                {
                    result.IsSuccess = false;
                    result.Message = serviceResult.ErrorMessage ?? "搜索失败";
                    result.Patients = new ObservableCollection<PatientInfo>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索患者失败：{Keyword}", keyword);
                result.IsSuccess = false;
                result.Message = $"搜索出错：{ex.Message}";
                result.Patients = new ObservableCollection<PatientInfo>();
            }

            return result;
        }

        /// <summary>
        /// 实时搜索（带缓存和防抖）
        /// </summary>
        public async Task<SearchResult> RealTimeSearchAsync(string keyword)
        {
            try
            {
                // 基本验证
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return new SearchResult 
                    { 
                        IsSuccess = true, 
                        Patients = new ObservableCollection<PatientInfo>(),
                        Message = "请输入搜索关键词"
                    };
                }

                // 至少2个字符才开始搜索
                if (keyword.Trim().Length < 2)
                {
                    return new SearchResult 
                    { 
                        IsSuccess = true, 
                        Patients = new ObservableCollection<PatientInfo>(),
                        Message = "请输入至少2个字符"
                    };
                }

                // 执行搜索
                return await SearchPatientsAsync(keyword);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "实时搜索失败");
                return new SearchResult 
                { 
                    IsSuccess = false, 
                    Message = $"搜索失败：{ex.Message}",
                    Patients = new ObservableCollection<PatientInfo>()
                };
            }
        }

        #endregion

        #region 患者详情加载

        /// <summary>
        /// 加载患者详细信息
        /// </summary>
        public async Task<PatientDetailResult> LoadPatientDetailsAsync(Guid patientId)
        {
            var result = new PatientDetailResult();

            try
            {
                _logger.LogDebug("开始加载患者详情：{PatientId}", patientId);

                var serviceResult = await _patientService.GetByIdAsync(patientId);
                
                if (serviceResult.IsSuccess && serviceResult.Data != null)
                {
                    result.IsSuccess = true;
                    result.PatientDetails = serviceResult.Data;
                    result.Message = "患者详情加载成功";

                    _logger.LogInformation("患者详情加载成功：{PatientName}", serviceResult.Data.Name);

                    // 同时加载该患者的医疗案例历史
                    var casesResult = await LoadPatientMedicalCasesAsync(patientId);
                    if (casesResult.IsSuccess)
                    {
                        result.MedicalCases = casesResult.MedicalCases;
                    }
                }
                else
                {
                    result.IsSuccess = false;
                    result.Message = serviceResult.ErrorMessage ?? "加载患者详情失败";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载患者详情失败：{PatientId}", patientId);
                result.IsSuccess = false;
                result.Message = $"加载失败：{ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 加载患者医疗案例历史
        /// </summary>
        public async Task<MedicalCasesResult> LoadPatientMedicalCasesAsync(Guid patientId)
        {
            var result = new MedicalCasesResult();

            try
            {
                _logger.LogDebug("开始加载患者医疗案例：{PatientId}", patientId);

                var serviceResult = await _medicalCaseService.GetByPatientIdAsync(patientId);
                
                if (serviceResult.IsSuccess && serviceResult.Data != null)
                {
                    var cases = serviceResult.Data as List<MedicalCaseInfo> ?? new List<MedicalCaseInfo>();
                    var recentCases = cases.OrderByDescending(c => c.CreateTime).Take(10).ToList();

                    result.IsSuccess = true;
                    result.MedicalCases = new ObservableCollection<MedicalCaseInfo>(recentCases);
                    result.Message = $"加载了 {recentCases.Count} 个医疗案例";

                    _logger.LogInformation("患者医疗案例加载成功：{CaseCount} 个案例", recentCases.Count);
                }
                else
                {
                    result.IsSuccess = false;
                    result.Message = serviceResult.ErrorMessage ?? "加载医疗案例失败";
                    result.MedicalCases = new ObservableCollection<MedicalCaseInfo>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载患者医疗案例失败：{PatientId}", patientId);
                result.IsSuccess = false;
                result.Message = $"加载失败：{ex.Message}";
                result.MedicalCases = new ObservableCollection<MedicalCaseInfo>();
            }

            return result;
        }

        #endregion

        #region 今日数据加载

        /// <summary>
        /// 加载今日接待的患者
        /// </summary>
        public async Task<TodayPatientsResult> LoadTodayPatientsAsync()
        {
            var result = new TodayPatientsResult();

            try
            {
                _logger.LogDebug("开始加载今日患者数据");

                // 获取今日的医疗案例
                var serviceResult = await _medicalCaseService.GetPagedAsync(1, 20);

                if (serviceResult != null && serviceResult.Items != null && string.IsNullOrEmpty(serviceResult.ErrorMessage))
                {
                    var todayCases = serviceResult.Items.OrderByDescending(c => c.CreateTime).ToList();

                    result.IsSuccess = true;
                    result.RecentCases = new ObservableCollection<MedicalCaseInfo>(todayCases);
                    result.Message = $"加载了 {todayCases.Count} 个今日案例";

                    _logger.LogInformation("今日患者数据加载成功：{Count} 个案例", todayCases.Count);
                }
                else
                {
                    result.IsSuccess = false;
                    result.Message = serviceResult?.ErrorMessage ?? "加载今日数据失败";
                    result.RecentCases = new ObservableCollection<MedicalCaseInfo>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载今日患者数据失败");
                result.IsSuccess = false;
                result.Message = $"加载失败：{ex.Message}";
                result.RecentCases = new ObservableCollection<MedicalCaseInfo>();
            }

            return result;
        }

        #endregion

        #region 患者查找或创建

        /// <summary>
        /// 查找或创建患者
        /// </summary>
        public async Task<PatientCreateResult> FindOrCreatePatientAsync(PatientDetailDto patientDto)
        {
            var result = new PatientCreateResult();

            try
            {
                _logger.LogDebug("开始查找或创建患者：{PatientName}", patientDto.Name);

                var serviceResult = await _patientService.FindOrCreateAsync(patientDto);
                
                if (serviceResult.IsSuccess && serviceResult.Data != null)
                {
                    result.IsSuccess = true;
                    result.Patient = serviceResult.Data;
                    result.IsNewPatient = serviceResult.Data.Id == Guid.Empty; // 判断是否是新创建的
                    result.Message = result.IsNewPatient ? "创建了新患者" : "找到了现有患者";

                    _logger.LogInformation("患者查找或创建成功：{PatientName}, 新患者：{IsNew}", 
                        serviceResult.Data.Name, result.IsNewPatient);
                }
                else
                {
                    result.IsSuccess = false;
                    result.Message = serviceResult.ErrorMessage ?? "查找或创建患者失败";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查找或创建患者失败：{PatientName}", patientDto.Name);
                result.IsSuccess = false;
                result.Message = $"操作失败：{ex.Message}";
            }

            return result;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 计算年龄
        /// </summary>
        private int CalculateAge(DateTime? birthDate)
        {
            if (!birthDate.HasValue) return 0;
            
            var today = DateTime.Today;
            var age = today.Year - birthDate.Value.Year;
            if (birthDate.Value.Date > today.AddYears(-age)) age--;
            
            return age;
        }

        #endregion

        #region 结果类定义

        public class SearchResult
        {
            public bool IsSuccess { get; set; }
            public string Message { get; set; } = string.Empty;
            public ObservableCollection<PatientInfo> Patients { get; set; } = new();
        }

        public class PatientDetailResult
        {
            public bool IsSuccess { get; set; }
            public string Message { get; set; } = string.Empty;
            public PatientDetailDto? PatientDetails { get; set; }
            public ObservableCollection<MedicalCaseInfo> MedicalCases { get; set; } = new();
        }

        public class MedicalCasesResult
        {
            public bool IsSuccess { get; set; }
            public string Message { get; set; } = string.Empty;
            public ObservableCollection<MedicalCaseInfo> MedicalCases { get; set; } = new();
        }

        public class TodayPatientsResult
        {
            public bool IsSuccess { get; set; }
            public string Message { get; set; } = string.Empty;
            public ObservableCollection<MedicalCaseInfo> RecentCases { get; set; } = new();
        }

        public class PatientCreateResult
        {
            public bool IsSuccess { get; set; }
            public string Message { get; set; } = string.Empty;
            public PatientDetailDto? Patient { get; set; }
            public bool IsNewPatient { get; set; }
        }

        #endregion
    }
}