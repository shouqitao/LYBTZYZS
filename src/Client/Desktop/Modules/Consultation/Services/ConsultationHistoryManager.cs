using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models.Consultation;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Prism.Mvvm;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// 诊疗历史管理器 - 专门负责患者历史记录管理
    /// UltraThink重构：从ConsultationWorkflowViewModel中提取历史记录管理职责
    /// </summary>
    public class ConsultationHistoryManager : BindableBase
    {
        #region 依赖服务

        private readonly IConsultationService _consultationService;
        private readonly IPrescriptionService _prescriptionService;
        private readonly ILogger<ConsultationHistoryManager> _logger;

        #endregion

        #region 历史记录属性

        private ObservableCollection<HistoryRecord> _patientHistory = new();
        public ObservableCollection<HistoryRecord> PatientHistory
        {
            get => _patientHistory;
            set => SetProperty(ref _patientHistory, value);
        }

        private HistoryRecord? _selectedHistoryRecord;
        public HistoryRecord? SelectedHistoryRecord
        {
            get => _selectedHistoryRecord;
            set => SetProperty(ref _selectedHistoryRecord, value);
        }

        private bool _isHistoryPanelVisible;
        public bool IsHistoryPanelVisible
        {
            get => _isHistoryPanelVisible;
            set => SetProperty(ref _isHistoryPanelVisible, value);
        }

        private bool _isHistoryLoading;
        public bool IsHistoryLoading
        {
            get => _isHistoryLoading;
            set => SetProperty(ref _isHistoryLoading, value);
        }

        private bool _hasHistoryData;
        public bool HasHistoryData
        {
            get => _hasHistoryData;
            set => SetProperty(ref _hasHistoryData, value);
        }

        #endregion

        #region 构造函数

        public ConsultationHistoryManager(
            IConsultationService consultationService,
            IPrescriptionService prescriptionService,
            ILogger<ConsultationHistoryManager> logger)
        {
            _consultationService = consultationService ?? throw new ArgumentNullException(nameof(consultationService));
            _prescriptionService = prescriptionService ?? throw new ArgumentNullException(nameof(prescriptionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 加载患者历史记录
        /// </summary>
        public async Task<bool> LoadPatientHistoryAsync(Guid patientId)
        {
            try
            {
                IsHistoryLoading = true;
                PatientHistory.Clear();

                _logger.LogInformation("开始加载患者历史记录: {PatientId}", patientId);

                // 加载诊疗历史
                var consultationHistory = await LoadConsultationHistoryAsync(patientId);
                
                // 加载处方历史  
                var prescriptionHistory = await LoadPrescriptionHistoryAsync(patientId);

                // 合并并排序历史记录
                var allHistory = consultationHistory.Concat(prescriptionHistory)
                    .OrderByDescending(h => h.Date)
                    .ToList();

                foreach (var record in allHistory)
                {
                    PatientHistory.Add(record);
                }

                HasHistoryData = PatientHistory.Any();
                
                _logger.LogInformation("患者历史记录加载完成，共 {Count} 条记录", PatientHistory.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载患者历史记录失败: {PatientId}", patientId);
                return false;
            }
            finally
            {
                IsHistoryLoading = false;
            }
        }

        /// <summary>
        /// 获取四诊历史数据用于导入
        /// </summary>
        public Task<ConsultationData?> GetFourDiagnosisDataAsync(HistoryRecord historyRecord)
        {
            try
            {
                if (historyRecord.Type != HistoryRecordType.Consultation)
                {
                    return Task.FromResult<ConsultationData?>(null);
                }

                // 简化获取四诊数据，暂时返回空
                _logger.LogInformation("获取四诊历史数据（暂时简化实现）: {RecordId}", historyRecord.RecordId);
                _logger.LogWarning("获取四诊历史数据失败: {RecordId}", historyRecord.RecordId);
                return Task.FromResult<ConsultationData?>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取四诊历史数据时发生错误: {RecordId}", historyRecord.RecordId);
                return Task.FromResult<ConsultationData?>(null);
            }
        }

        /// <summary>
        /// 获取处方历史数据用于导入
        /// </summary>
        public Task<PrescriptionData?> GetPrescriptionDataAsync(HistoryRecord historyRecord)
        {
            try
            {
                if (historyRecord.Type != HistoryRecordType.Prescription)
                {
                    return Task.FromResult<PrescriptionData?>(null);
                }

                // 简化获取处方数据，暂时返回空
                _logger.LogInformation("获取处方历史数据（暂时简化实现）: {RecordId}", historyRecord.RecordId);
                _logger.LogWarning("获取处方历史数据失败: {RecordId}", historyRecord.RecordId);
                return Task.FromResult<PrescriptionData?>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方历史数据时发生错误: {RecordId}", historyRecord.RecordId);
                return Task.FromResult<PrescriptionData?>(null);
            }
        }

        /// <summary>
        /// 切换历史面板显示状态
        /// </summary>
        public void ToggleHistoryPanel()
        {
            IsHistoryPanelVisible = !IsHistoryPanelVisible;
            _logger.LogDebug("历史面板显示状态切换为: {IsVisible}", IsHistoryPanelVisible);
        }

        /// <summary>
        /// 显示历史面板
        /// </summary>
        public void ShowHistoryPanel()
        {
            IsHistoryPanelVisible = true;
        }

        /// <summary>
        /// 隐藏历史面板
        /// </summary>
        public void HideHistoryPanel()
        {
            IsHistoryPanelVisible = false;
        }

        /// <summary>
        /// 清除历史数据
        /// </summary>
        public void ClearHistory()
        {
            PatientHistory.Clear();
            SelectedHistoryRecord = null;
            HasHistoryData = false;
            IsHistoryPanelVisible = false;
        }

        #endregion

        #region 私有方法

        private async Task<List<HistoryRecord>> LoadConsultationHistoryAsync(Guid patientId)
        {
            var history = new List<HistoryRecord>();

            try
            {
                var result = await _consultationService.GetPatientHistoryAsync(patientId);
                if (result.IsSuccess && result.Data != null)
                {
                    foreach (var consultation in result.Data)
                    {
                        history.Add(new HistoryRecord
                        {
                            RecordId = consultation.Id,
                            Type = HistoryRecordType.Consultation,
                            Date = DateTime.Now,
                            Title = $"诊疗记录 - {DateTime.Now:yyyy-MM-dd}",
                            Summary = $"诊疗记录",  // 简化摘要
                            DoctorName = consultation.DoctorName ?? "未知医生"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载诊疗历史失败");
            }

            return history;
        }

        private async Task<List<HistoryRecord>> LoadPrescriptionHistoryAsync(Guid patientId)
        {
            var history = new List<HistoryRecord>();

            try
            {
                var result = await _prescriptionService.GetByPatientIdAsync(patientId);
                if (result.IsSuccess && result.Data != null)
                {
                    foreach (var prescription in result.Data)
                    {
                        history.Add(new HistoryRecord
                        {
                            RecordId = prescription.Id,
                            Type = HistoryRecordType.Prescription,
                            Date = DateTime.Now,
                            Title = $"处方记录 - {DateTime.Now:yyyy-MM-dd}",
                            Summary = $"处方记录", // 简化摘要
                            DoctorName = prescription.DoctorName ?? "未知医生"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载处方历史失败");
            }

            return history;
        }

        #endregion
    }

    /// <summary>
    /// 历史记录类型
    /// </summary>
    public enum HistoryRecordType
    {
        Consultation,
        Prescription
    }

    /// <summary>
    /// 历史记录项
    /// </summary>
    public class HistoryRecord
    {
        public Guid RecordId { get; set; }
        public HistoryRecordType Type { get; set; }
        public DateTime Date { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
    }
}