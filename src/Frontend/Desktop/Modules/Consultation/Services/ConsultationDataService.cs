using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Models.Consultation;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.WPF.Client.Core.Models.Formulas;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;
using AutoMapper;

namespace LYBT.WPF.Client.Modules.Consultation.Services
{
    /// <summary>
    /// 看诊数据服务 - 负责数据加载和缓存管理
    /// </summary>
    public class ConsultationDataService : IConsultationDataService
    {
        #region 缓存配置常量
        
        private const int HERBS_CACHE_DURATION_MINUTES = 30;
        private const int FORMULAS_CACHE_DURATION_MINUTES = 60;
        private const int PATIENTS_CACHE_DURATION_MINUTES = 10;
        
        #endregion

        #region 依赖服务
        
        private readonly IPatientsApiService _patientsApiService;
        private readonly IConsultationApiService _consultationApiService;
        private readonly IFormulaApiService _formulaApiService;
        private readonly IHerbService _herbService;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationDataService> _logger;
        
        #endregion

        #region 缓存字段
        
        private List<HerbInfo>? _cachedHerbs;
        private DateTime _herbsCacheTime = DateTime.MinValue;
        
        private List<FormulaInfo>? _cachedFormulas;
        private DateTime _formulasCacheTime = DateTime.MinValue;
        
        private List<PatientInfo>? _cachedPatients;
        private DateTime _patientsCacheTime = DateTime.MinValue;
        
        #endregion

        public ConsultationDataService(
            IPatientsApiService patientsApiService,
            IConsultationApiService consultationApiService,
            IFormulaApiService formulaApiService,
            IHerbService herbService,
            IMapper mapper,
            ILogger<ConsultationDataService> logger)
        {
            _patientsApiService = patientsApiService;
            _consultationApiService = consultationApiService;
            _formulaApiService = formulaApiService;
            _herbService = herbService;
            _mapper = mapper;
            _logger = logger;
        }

        #region 患者数据加载

        /// <summary>
        /// 加载患者列表（带缓存）
        /// </summary>
        public async Task<List<PatientInfo>> LoadPatientsAsync(bool forceRefresh = false)
        {
            try
            {
                // 检查缓存
                if (!forceRefresh && IsCacheValid(_patientsCacheTime, PATIENTS_CACHE_DURATION_MINUTES))
                {
                    _logger.LogDebug("使用缓存的患者数据");
                    return _cachedPatients ?? new List<PatientInfo>();
                }

                _logger.LogInformation("从API加载患者列表");
                var response = await _patientsApiService.GetActivePatientsAsync();
                
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var patients = _mapper.Map<List<PatientInfo>>(response.Content);
                    
                    // 更新缓存
                    _cachedPatients = patients;
                    _patientsCacheTime = DateTime.Now;
                    
                    _logger.LogInformation($"成功加载 {patients.Count} 个患者");
                    return patients;
                }
                
                _logger.LogWarning("加载患者列表失败");
                return new List<PatientInfo>();
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
        public async Task<List<HerbInfo>> LoadHerbsAsync(bool forceRefresh = false)
        {
            try
            {
                // 检查缓存
                if (!forceRefresh && IsCacheValid(_herbsCacheTime, HERBS_CACHE_DURATION_MINUTES))
                {
                    _logger.LogDebug("使用缓存的中药材数据");
                    return _cachedHerbs ?? new List<HerbInfo>();
                }

                _logger.LogInformation("从API加载中药材列表");
                var herbs = await _herbService.GetHerbsAsync();
                
                // 更新缓存
                _cachedHerbs = herbs;
                _herbsCacheTime = DateTime.Now;
                
                _logger.LogInformation($"成功加载 {herbs.Count} 种中药材");
                return herbs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载中药材列表时发生异常");
                return new List<HerbInfo>();
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
                // 检查缓存
                if (!forceRefresh && IsCacheValid(_formulasCacheTime, FORMULAS_CACHE_DURATION_MINUTES))
                {
                    _logger.LogDebug("使用缓存的验方数据");
                    return _cachedFormulas ?? new List<FormulaInfo>();
                }

                _logger.LogInformation("从API加载验方模板列表");
                var response = await _formulaApiService.GetFormulasAsync();
                
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var formulas = _mapper.Map<List<FormulaInfo>>(response.Content.Items);
                    
                    // 更新缓存
                    _cachedFormulas = formulas;
                    _formulasCacheTime = DateTime.Now;
                    
                    _logger.LogInformation($"成功加载 {formulas.Count} 个验方模板");
                    return formulas;
                }
                
                _logger.LogWarning("加载验方模板列表失败");
                return new List<FormulaInfo>();
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
                var createDto = new CreateConsultationDto
                {
                    PatientId = patientId,
                    ChiefComplaint = string.Empty,
                    Symptoms = string.Empty
                };

                var response = await _consultationApiService.CreateAsync(createDto);
                
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
                var updateDto = _mapper.Map<UpdateConsultationDto>(consultation);
                var response = await _consultationApiService.UpdateAsync(consultation.Id, updateDto);
                
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

        #region 辅助方法

        /// <summary>
        /// 检查缓存是否有效
        /// </summary>
        private bool IsCacheValid(DateTime cacheTime, int durationMinutes)
        {
            return cacheTime != DateTime.MinValue && 
                   (DateTime.Now - cacheTime).TotalMinutes < durationMinutes;
        }

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public void ClearAllCache()
        {
            _cachedHerbs = null;
            _herbsCacheTime = DateTime.MinValue;
            
            _cachedFormulas = null;
            _formulasCacheTime = DateTime.MinValue;
            
            _cachedPatients = null;
            _patientsCacheTime = DateTime.MinValue;
            
            _logger.LogInformation("已清除所有缓存数据");
        }

        #endregion
    }
}