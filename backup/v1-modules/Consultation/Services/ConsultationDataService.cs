using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Core.Models.Consultation;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Desktop.Consultation.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;
using AutoMapper;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Desktop.Core.Models.Formulas;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// 看诊数据服务 - 负责数据加载和缓存管理
    /// </summary>
    public class ConsultationDataService : IConsultationDataService
    {
        #region 缓存配置常量
        
        private const string HERBS_CACHE_KEY = "consultation:herbs";
        private const string FORMULAS_CACHE_KEY = "consultation:formulas";
        private const string PATIENTS_CACHE_KEY = "consultation:patients";
        
        private static readonly TimeSpan HERBS_CACHE_DURATION = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan FORMULAS_CACHE_DURATION = TimeSpan.FromMinutes(60);
        private static readonly TimeSpan PATIENTS_CACHE_DURATION = TimeSpan.FromMinutes(10);
        
        #endregion

        #region 依赖服务
        
        private readonly IPatientApi _patientsApiService;
        private readonly IConsultationApiService _consultationApiService;
        private readonly IFormulaApiService _formulaApiService;
        private readonly IHerbService _herbService;
        private readonly ICacheService _cacheService;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationDataService> _logger;
        
        #endregion

        public ConsultationDataService(
            IPatientApi patientsApiService,
            IConsultationApiService consultationApiService,
            IFormulaApiService formulaApiService,
            IHerbService herbService,
            ICacheService cacheService,
            IMapper mapper,
            ILogger<ConsultationDataService> logger)
        {
            _patientsApiService = patientsApiService ?? throw new ArgumentNullException(nameof(patientsApiService));
            _consultationApiService = consultationApiService ?? throw new ArgumentNullException(nameof(consultationApiService));
            _formulaApiService = formulaApiService ?? throw new ArgumentNullException(nameof(formulaApiService));
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 患者数据加载

        /// <summary>
        /// 加载患者列表（带缓存）
        /// </summary>
        public async Task<List<PatientInfo>> LoadPatientsAsync(bool forceRefresh = false)
        {
            try
            {
                // 如果强制刷新，先清除缓存
                if (forceRefresh)
                {
                    _cacheService.Remove(PATIENTS_CACHE_KEY);
                }

                // 使用GetOrCreateAsync方法，自动处理缓存逻辑
                var patients = await _cacheService.GetOrCreateAsync(PATIENTS_CACHE_KEY, async () =>
                {
                    _logger.LogInformation("从API加载患者列表");
                    var response = await _patientsApiService.GetActivePatientsAsync();
                    
                    if (response.IsSuccessStatusCode && response.Content != null)
                    {
                        var patientList = _mapper.Map<List<PatientInfo>>(response.Content);
                        _logger.LogInformation($"成功加载 {patientList.Count} 个患者");
                        return patientList;
                    }
                    
                    _logger.LogWarning("加载患者列表失败，返回空列表");
                    return new List<PatientInfo>();
                }, PATIENTS_CACHE_DURATION);

                return patients;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载患者列表时发生异常");
                return new List<PatientInfo>();
            }
        }

        #endregion

        #region 中药材数据加载

        /// <summary>
        /// 加载中药材列表（带缓存）
        /// </summary>
        public async Task<List<HerbDto>> LoadHerbsAsync(bool forceRefresh = false)
        {
            try
            {
                // 如果强制刷新，先清除缓存
                if (forceRefresh)
                {
                    _cacheService.Remove(HERBS_CACHE_KEY);
                }

                // 使用GetOrCreateAsync方法，自动处理缓存逻辑
                var herbs = await _cacheService.GetOrCreateAsync<List<HerbDto>>(HERBS_CACHE_KEY, async () =>
                {
                    _logger.LogInformation("从API加载中药材列表");
                    var herbResult = await _herbService.GetHerbsAsync();
                    if (herbResult.IsSuccess && herbResult.Data != null)
                    {
                        // UltraThink转换：HerbInfo → HerbDto
                        var herbDtos = herbResult.Data.Select(ConvertToHerbDto).ToList();
                        _logger.LogInformation($"成功加载 {herbDtos.Count} 种中药材");
                        return herbDtos;
                    }
                    else
                    {
                        _logger.LogWarning($"加载中药材失败: {herbResult.ErrorMessage}");
                        return new List<HerbDto>();
                    }
                }, HERBS_CACHE_DURATION);

                return herbs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载中药材列表时发生异常");
                return new List<HerbDto>();
            }
        }

        #endregion

        #region 验方数据加载

        /// <summary>
        /// 加载验方模板列表（带缓存）
        /// </summary>
        public async Task<List<FormulaInfo>> LoadFormulasAsync(bool forceRefresh = false)
        {
            try
            {
                // 如果强制刷新，先清除缓存
                if (forceRefresh)
                {
                    _cacheService.Remove(FORMULAS_CACHE_KEY);
                }

                // 使用GetOrCreateAsync方法，自动处理缓存逻辑
                var formulas = await _cacheService.GetOrCreateAsync(FORMULAS_CACHE_KEY, async () =>
                {
                    _logger.LogInformation("从API加载验方模板列表");
                    var response = await _formulaApiService.GetFormulasAsync();
                    
                    if (response.IsSuccessStatusCode && response.Content != null)
                    {
                        var formulaList = _mapper.Map<List<FormulaInfo>>(response.Content.Items);
                        _logger.LogInformation($"成功加载 {formulaList.Count} 个验方模板");
                        return formulaList;
                    }
                    
                    _logger.LogWarning("加载验方模板列表失败，返回空列表");
                    return new List<FormulaInfo>();
                }, FORMULAS_CACHE_DURATION);

                return formulas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载验方模板列表时发生异常");
                return new List<FormulaInfo>();
            }
        }

        #endregion

        #region 看诊记录操作

        /// <summary>
        /// 创建新的看诊记录
        /// </summary>
        public async Task<ConsultationInfo?> CreateConsultationAsync(Guid patientId)
        {
            try
            {
                var startDto = new ConsultationStartDto
                {
                    PatientId = patientId,
                    MedicalCaseId = Guid.NewGuid(), // 应该先创建或获取医疗案例
                    DoctorId = Guid.NewGuid(), // 应该从当前登录用户获取
                    Remark = string.Empty
                };

                var response = await _consultationApiService.StartConsultationAsync(startDto);
                
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var consultation = _mapper.Map<ConsultationInfo>(response.Content);
                    _logger.LogInformation($"成功创建看诊记录，ID: {consultation.Id}");
                    return consultation;
                }
                
                _logger.LogWarning("创建看诊记录失败");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建看诊记录时发生异常");
                return null;
            }
        }

        /// <summary>
        /// 更新看诊记录
        /// </summary>
        public async Task<bool> UpdateConsultationAsync(ConsultationInfo consultation)
        {
            try
            {
                var updateDto = _mapper.Map<ConsultationUpdateDto>(consultation);
                var response = await _consultationApiService.UpdateConsultationAsync(consultation.Id, updateDto);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"成功更新看诊记录，ID: {consultation.Id}");
                    return true;
                }
                
                _logger.LogWarning($"更新看诊记录失败，ID: {consultation.Id}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新看诊记录时发生异常，ID: {consultation.Id}");
                return false;
            }
        }

        #endregion

        #region 缓存管理

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public void ClearAllCache()
        {
            try
            {
                // 使用缓存服务的批量移除功能
                var keysToRemove = new[] { HERBS_CACHE_KEY, FORMULAS_CACHE_KEY, PATIENTS_CACHE_KEY };
                var removedCount = _cacheService.RemoveMany(keysToRemove);
                
                _logger.LogInformation("已清除所有缓存数据，共移除 {Count} 项", removedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除缓存时发生异常");
            }
        }

        /// <summary>
        /// 清除特定类型的缓存
        /// </summary>
        /// <param name="cacheType">缓存类型（herbs/formulas/patients）</param>
        public void ClearSpecificCache(string cacheType)
        {
            try
            {
                var key = cacheType.ToLower() switch
                {
                    "herbs" => HERBS_CACHE_KEY,
                    "formulas" => FORMULAS_CACHE_KEY,
                    "patients" => PATIENTS_CACHE_KEY,
                    _ => null
                };

                if (key != null)
                {
                    var removed = _cacheService.Remove(key);
                    _logger.LogInformation("已清除 {CacheType} 缓存，结果: {Result}", cacheType, removed ? "成功" : "未找到");
                }
                else
                {
                    _logger.LogWarning("未知的缓存类型: {CacheType}", cacheType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除特定缓存时发生异常，类型: {CacheType}", cacheType);
            }
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>缓存统计</returns>
        public object GetCacheStatistics()
        {
            try
            {
                var stats = _cacheService.GetStatistics();
                return new
                {
                    TotalItems = stats.ItemCount,
                    HitRate = $"{stats.HitRate:P2}",
                    TotalRequests = stats.TotalRequests,
                    MemoryUsage = $"{stats.EstimatedMemoryUsage / 1024 / 1024:F2} MB",
                    HerbsCached = _cacheService.Exists(HERBS_CACHE_KEY),
                    FormulasCached = _cacheService.Exists(FORMULAS_CACHE_KEY),
                    PatientsCached = _cacheService.Exists(PATIENTS_CACHE_KEY)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取缓存统计信息时发生异常");
                return new { Error = "获取统计信息失败" };
            }
        }

        #endregion

        #region 私有转换方法

        /// <summary>
        /// UltraThink转换：HerbInfo → HerbDto（UI层到传输层）
        /// </summary>
        private static HerbDto ConvertToHerbDto(LYBT.Desktop.Core.Models.Herbs.HerbInfo herbInfo)
        {
            return new HerbDto
            {
                Id = herbInfo.Id,
                Name = herbInfo.Name,
                PinYinCode = herbInfo.PinYinCode,
                WuBiCode = null, // HerbInfo没有WuBiCode属性
                Origin = herbInfo.Origin,
                Spec = herbInfo.Spec,
                Unit = herbInfo.Unit,
                Price = herbInfo.Price,
                Effect = herbInfo.Effect,
                Usage = herbInfo.Usage,
                Remark = herbInfo.Remark,
                Status = herbInfo.Status,
                Stock = (int)herbInfo.Stock // UltraThink：decimal到int类型转换
            };
        }

        #endregion
    }
}