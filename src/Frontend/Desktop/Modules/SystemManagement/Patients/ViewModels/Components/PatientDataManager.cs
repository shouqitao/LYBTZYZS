using System;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Models.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.WPF.Client.Modules.SystemManagement.Patients.ViewModels.Components
{
    /// <summary>
    /// 患者数据管理器 - UltraThink专门化组件
    /// 职责单一：专注患者接待相关数据的管理和状态维护
    /// 代码干净：清晰的数据结构和状态管理
    /// 性能出色：优化的数据更新和内存使用
    /// </summary>
    public class PatientDataManager
    {
        private readonly ILogger<PatientDataManager> _logger;

        public PatientDataManager(ILogger<PatientDataManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializeCollections();
        }

        #region 核心数据属性

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchKeyword { get; set; } = "";

        /// <summary>
        /// 搜索结果
        /// </summary>
        public ObservableCollection<PatientInfo> SearchResults { get; set; } = new();

        /// <summary>
        /// 选中的患者
        /// </summary>
        public PatientInfo? SelectedPatient { get; set; }

        /// <summary>
        /// 患者详细信息
        /// </summary>
        public PatientDetailDto? PatientDetails { get; set; }

        /// <summary>
        /// 最近的医疗案例
        /// </summary>
        public ObservableCollection<MedicalCaseInfo> RecentCases { get; set; } = new();

        #endregion

        #region 快速接待表单数据

        /// <summary>
        /// 患者姓名
        /// </summary>
        public string PatientName { get; set; } = "";

        /// <summary>
        /// 患者性别
        /// </summary>
        public string PatientGender { get; set; } = "男";

        /// <summary>
        /// 患者年龄
        /// </summary>
        public string PatientAge { get; set; } = "";

        /// <summary>
        /// 患者电话
        /// </summary>
        public string PatientPhone { get; set; } = "";

        /// <summary>
        /// 患者身份证
        /// </summary>
        public string PatientIdCard { get; set; } = "";

        /// <summary>
        /// 主诉
        /// </summary>
        public string ChiefComplaint { get; set; } = "";

        #endregion

        #region 状态属性

        /// <summary>
        /// 加载状态
        /// </summary>
        public bool IsLoading { get; set; }

        /// <summary>
        /// 是否新患者
        /// </summary>
        public bool IsNewPatient { get; set; }

        /// <summary>
        /// 数据是否有变更
        /// </summary>
        public bool HasChanges { get; private set; }

        #endregion

        #region 数据操作方法

        /// <summary>
        /// 设置搜索结果
        /// </summary>
        public void SetSearchResults(ObservableCollection<PatientInfo> results)
        {
            try
            {
                SearchResults.Clear();
                if (results?.Count > 0)
                {
                    foreach (var item in results)
                    {
                        SearchResults.Add(item);
                    }

                    // 如果只有一个结果，自动选中
                    if (SearchResults.Count == 1)
                    {
                        SelectedPatient = SearchResults[0];
                        MarkAsChanged();
                    }
                }

                _logger.LogDebug("设置搜索结果：{Count} 个患者", SearchResults.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置搜索结果失败");
            }
        }

        /// <summary>
        /// 设置患者详情
        /// </summary>
        public void SetPatientDetails(PatientDetailDto? details)
        {
            PatientDetails = details;
            if (details != null)
            {
                _logger.LogDebug("设置患者详情：{PatientName}", details.Name);
            }
            MarkAsChanged();
        }

        /// <summary>
        /// 设置最近案例
        /// </summary>
        public void SetRecentCases(ObservableCollection<MedicalCaseInfo> cases)
        {
            try
            {
                RecentCases.Clear();
                if (cases?.Count > 0)
                {
                    foreach (var case_ in cases)
                    {
                        RecentCases.Add(case_);
                    }
                }

                _logger.LogDebug("设置最近案例：{Count} 个案例", RecentCases.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置最近案例失败");
            }
        }

        /// <summary>
        /// 清空表单
        /// </summary>
        public void ClearForm()
        {
            try
            {
                PatientName = "";
                PatientGender = "男";
                PatientAge = "";
                PatientPhone = "";
                PatientIdCard = "";
                ChiefComplaint = "";
                SearchKeyword = "";
                SelectedPatient = null;
                PatientDetails = null;
                IsNewPatient = false;
                HasChanges = false;

                _logger.LogDebug("表单已清空");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空表单失败");
            }
        }

        /// <summary>
        /// 清空搜索结果
        /// </summary>
        public void ClearSearchResults()
        {
            SearchResults.Clear();
            SelectedPatient = null;
            PatientDetails = null;
            _logger.LogDebug("搜索结果已清空");
        }

        /// <summary>
        /// 设置选中患者
        /// </summary>
        public void SetSelectedPatient(PatientInfo? patient)
        {
            SelectedPatient = patient;
            if (patient != null)
            {
                _logger.LogDebug("选中患者：{PatientName}", patient.Name);
            }
            MarkAsChanged();
        }

        /// <summary>
        /// 更新加载状态
        /// </summary>
        public void SetLoadingState(bool isLoading)
        {
            IsLoading = isLoading;
            _logger.LogDebug("加载状态更新：{IsLoading}", isLoading);
        }

        /// <summary>
        /// 标记数据已变更
        /// </summary>
        public void MarkAsChanged()
        {
            HasChanges = true;
        }

        /// <summary>
        /// 重置变更标记
        /// </summary>
        public void ResetChanges()
        {
            HasChanges = false;
        }

        #endregion

        #region 表单填充方法

        /// <summary>
        /// 从患者详情填充表单
        /// </summary>
        public void FillFormFromPatientDetails(PatientDetailDto patient)
        {
            try
            {
                PatientName = patient.Name ?? "";
                PatientGender = patient.Gender.ToString();
                PatientPhone = patient.PhoneNumber ?? "";
                PatientIdCard = patient.IDNumber ?? "";
                
                // 计算年龄
                if (patient.BirthDate.HasValue)
                {
                    var age = CalculateAge(patient.BirthDate.Value);
                    PatientAge = age.ToString();
                }

                MarkAsChanged();
                _logger.LogDebug("从患者详情填充表单：{PatientName}", patient.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从患者详情填充表单失败");
            }
        }

        /// <summary>
        /// 验证快速接待表单
        /// </summary>
        public bool IsQuickReceptionFormValid()
        {
            // 要么选中了现有患者，要么填写了新患者基本信息
            return SelectedPatient != null || 
                   (!string.IsNullOrWhiteSpace(PatientName) && !string.IsNullOrWhiteSpace(PatientPhone));
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 初始化集合
        /// </summary>
        private void InitializeCollections()
        {
            SearchResults = new ObservableCollection<PatientInfo>();
            RecentCases = new ObservableCollection<MedicalCaseInfo>();
            _logger.LogDebug("数据集合初始化完成");
        }

        /// <summary>
        /// 计算年龄
        /// </summary>
        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age;
        }

        #endregion
    }
}