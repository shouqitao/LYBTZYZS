using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Events;
using Microsoft.Extensions.Logging;
using AutoMapper;
using LYBT.Desktop.Consultation.Services;
using LYBT.Desktop.Consultation.Components;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Events;

namespace LYBT.Desktop.Consultation.ViewModels
{
    /// <summary>
    /// 中医四诊ViewModel - UltraThink Phase 2: 合并简化版
    /// 合并了 SimpleTCMFourDiagnosisViewModel + TCMFourDiagnosisViewModel
    /// 直接使用 TCMDiagnosisService 统一四诊服务
    /// </summary>
    public class TCMDiagnosisViewModel : BindableBase
    {
        #region 核心服务

        private readonly TCMDiagnosisService _tcmDiagnosisService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ICustomDialogService _dialogService;
        private readonly ILogger<TCMDiagnosisViewModel> _logger;
        private readonly IMapper _mapper;

        #endregion

        #region 数据属性

        private Guid _consultationId;
        public Guid ConsultationId
        {
            get => _consultationId;
            set => SetProperty(ref _consultationId, value);
        }

        private Guid _medicalCaseId;
        public Guid MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }

        /// <summary>
        /// 四诊数据服务（直接暴露给View绑定）
        /// </summary>
        public TCMDiagnosisService DiagnosisService => _tcmDiagnosisService;

        #endregion

        #region 状态属性

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        private bool _isAnalyzing;
        public bool IsAnalyzing
        {
            get => _isAnalyzing;
            set => SetProperty(ref _isAnalyzing, value);
        }

        #endregion

        #region 分析结果属性

        private TCMDiagnosisAnalysis? _analysisResult;
        public TCMDiagnosisAnalysis? AnalysisResult
        {
            get => _analysisResult;
            set => SetProperty(ref _analysisResult, value);
        }

        private string _overallAssessment = "";
        public string OverallAssessment
        {
            get => _overallAssessment;
            set => SetProperty(ref _overallAssessment, value);
        }

        #endregion

        #region 快速输入集合

        public ObservableCollection<string> RecentSymptoms { get; } = new();
        public ObservableCollection<string> CommonPatterns { get; } = new();

        #endregion

        #region 命令

        public ICommand SaveCommand { get; }
        public ICommand LoadCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand AnalyzeCommand { get; }
        public ICommand QuickInputCommand { get; }
        public ICommand ImportHistoryCommand { get; }
        public ICommand ExportDataCommand { get; }
        public ICommand ResetToDefaultCommand { get; }

        #endregion

        #region 构造函数

        public TCMDiagnosisViewModel(
            TCMDiagnosisService tcmDiagnosisService,
            IEventAggregator eventAggregator,
            ICustomDialogService dialogService,
            ILogger<TCMDiagnosisViewModel> logger,
            IMapper mapper)
        {
            _tcmDiagnosisService = tcmDiagnosisService ?? throw new ArgumentNullException(nameof(tcmDiagnosisService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave);
            LoadCommand = new DelegateCommand(async () => await LoadAsync());
            ClearCommand = new DelegateCommand(Clear, () => !IsLoading);
            AnalyzeCommand = new DelegateCommand(async () => await AnalyzeAsync(), CanAnalyze);
            QuickInputCommand = new DelegateCommand<string>(QuickInput);
            ImportHistoryCommand = new DelegateCommand(async () => await ImportHistoryAsync());
            ExportDataCommand = new DelegateCommand(async () => await ExportDataAsync(), CanExport);
            ResetToDefaultCommand = new DelegateCommand(ResetToDefault, () => !IsLoading);

            // 监听数据变更
            _tcmDiagnosisService.PropertyChanged += OnDiagnosisServicePropertyChanged;

            // 订阅事件
            _eventAggregator.GetEvent<MedicalCaseSelectedEvent>().Subscribe(OnMedicalCaseSelected);

            // 初始化快速输入数据
            InitializeQuickInputData();

            _logger.LogInformation("TCMDiagnosisViewModel 初始化完成");
        }

        #endregion

        #region 事件处理

        private void OnDiagnosisServicePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            HasUnsavedChanges = true;
            
            // 当关键数据变化时，更新命令状态
            (SaveCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (AnalyzeCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            (ExportDataCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }

        private async void OnMedicalCaseSelected(MedicalCaseSelectedEventArgs args)
        {
            try
            {
                MedicalCaseId = args.MedicalCaseId;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理医疗案例选择事件时发生错误: {MedicalCaseId}", args.MedicalCaseId);
                await _dialogService.ShowErrorAsync("加载患者数据失败", ex.Message);
            }
        }

        #endregion

        #region 命令实现

        private async Task SaveAsync()
        {
            try
            {
                IsLoading = true;

                if (ConsultationId == Guid.Empty)
                {
                    await _dialogService.ShowWarningAsync("保存失败", "请先选择诊疗记录");
                    return;
                }

                var tcmData = _tcmDiagnosisService.ToData();
                tcmData.ConsultationId = ConsultationId;

                // 这里应该调用API保存数据
                // await _consultationApi.SaveTCMDiagnosisAsync(tcmData);

                HasUnsavedChanges = false;
                
                _logger.LogInformation("四诊数据保存成功: {ConsultationId}", ConsultationId);
                await _dialogService.ShowInformationAsync("保存成功", "四诊数据已保存");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存四诊数据时发生错误: {ConsultationId}", ConsultationId);
                await _dialogService.ShowErrorAsync("保存失败", ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanSave()
        {
            return !IsLoading && HasUnsavedChanges && _tcmDiagnosisService.IsDataValid();
        }

        private async Task LoadAsync()
        {
            try
            {
                IsLoading = true;

                if (ConsultationId == Guid.Empty)
                {
                    _logger.LogWarning("尝试加载数据但ConsultationId为空");
                    return;
                }

                // 这里应该调用API加载数据
                // var tcmData = await _consultationApi.GetTCMDiagnosisAsync(ConsultationId);
                // if (tcmData != null)
                // {
                //     _tcmDiagnosisService.LoadFromData(tcmData);
                // }

                HasUnsavedChanges = false;
                _logger.LogInformation("四诊数据加载完成: {ConsultationId}", ConsultationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载四诊数据时发生错误: {ConsultationId}", ConsultationId);
                await _dialogService.ShowErrorAsync("加载失败", ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Clear()
        {
            try
            {
                _tcmDiagnosisService.Reset();
                AnalysisResult = null;
                OverallAssessment = "";
                HasUnsavedChanges = false;

                _logger.LogInformation("四诊数据已清空");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空四诊数据时发生错误");
            }
        }

        private async Task AnalyzeAsync()
        {
            try
            {
                IsAnalyzing = true;

                if (!_tcmDiagnosisService.IsDataValid())
                {
                    await _dialogService.ShowWarningAsync("分析失败", "请至少输入一项四诊数据");
                    return;
                }

                // 执行四诊分析
                AnalysisResult = _tcmDiagnosisService.GetComprehensiveAnalysis();
                OverallAssessment = AnalysisResult?.OverallAssessment ?? "";

                _logger.LogInformation("四诊分析完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "四诊分析时发生错误");
                await _dialogService.ShowErrorAsync("分析失败", ex.Message);
            }
            finally
            {
                IsAnalyzing = false;
            }
        }

        private bool CanAnalyze()
        {
            return !IsLoading && !IsAnalyzing && _tcmDiagnosisService.IsDataValid();
        }

        private void QuickInput(string? data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return;

            try
            {
                // 解析快速输入数据格式: "category:value"
                var parts = data.Split(':', 2);
                if (parts.Length != 2)
                    return;

                var category = parts[0].Trim();
                var value = parts[1].Trim();

                switch (category.ToLowerInvariant())
                {
                    case "complexion":
                    case "面色":
                        _tcmDiagnosisService.Complexion = value;
                        break;
                    case "spirit":
                    case "神态":
                        _tcmDiagnosisService.Spirit = value;
                        break;
                    case "tongue":
                    case "舌象":
                        _tcmDiagnosisService.TongueBody = value;
                        break;
                    case "pulse":
                    case "脉象":
                        _tcmDiagnosisService.Pulse = value;
                        break;
                    case "voice":
                    case "声音":
                        _tcmDiagnosisService.Voice = value;
                        break;
                    case "breathing":
                    case "呼吸":
                        _tcmDiagnosisService.Breathing = value;
                        break;
                    case "chief":
                    case "主诉":
                        _tcmDiagnosisService.ChiefComplaint = value;
                        break;
                }

                _logger.LogInformation("快速输入完成: {Category} = {Value}", category, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "快速输入时发生错误: {Data}", data);
            }
        }

        private async Task ImportHistoryAsync()
        {
            try
            {
                // 这里可以实现从历史记录导入数据的功能
                await _dialogService.ShowInformationAsync("功能提示", "历史记录导入功能开发中...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入历史记录时发生错误");
                await _dialogService.ShowErrorAsync("导入失败", ex.Message);
            }
        }

        private async Task ExportDataAsync()
        {
            try
            {
                var tcmData = _tcmDiagnosisService.ToData();
                
                // 这里可以实现导出功能，比如导出到文件或剪贴板
                await _dialogService.ShowInformationAsync("导出成功", "四诊数据已导出");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出数据时发生错误");
                await _dialogService.ShowErrorAsync("导出失败", ex.Message);
            }
        }

        private bool CanExport()
        {
            return !IsLoading && _tcmDiagnosisService.IsDataValid();
        }

        private void ResetToDefault()
        {
            try
            {
                Clear();
                // 可以在这里设置一些默认值
                _logger.LogInformation("四诊数据已重置为默认值");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置默认值时发生错误");
            }
        }

        #endregion

        #region 辅助方法

        private void InitializeQuickInputData()
        {
            try
            {
                // 常用症状快速输入
                RecentSymptoms.Add("complexion:面色红润");
                RecentSymptoms.Add("complexion:面色苍白");
                RecentSymptoms.Add("spirit:精神萎靡");
                RecentSymptoms.Add("tongue:舌淡红");
                RecentSymptoms.Add("pulse:脉缓");
                
                // 常用证型模式
                CommonPatterns.Add("气虚证");
                CommonPatterns.Add("血虚证");
                CommonPatterns.Add("阳虚证");
                CommonPatterns.Add("阴虚证");
                CommonPatterns.Add("痰湿证");
                CommonPatterns.Add("血瘀证");
                CommonPatterns.Add("肝郁证");
                CommonPatterns.Add("脾胃虚弱");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化快速输入数据时发生错误");
            }
        }

        /// <summary>
        /// 应用证型模式
        /// </summary>
        public void ApplyPattern(string pattern)
        {
            try
            {
                switch (pattern)
                {
                    case "气虚证":
                        _tcmDiagnosisService.Complexion = "面色苍白";
                        _tcmDiagnosisService.Spirit = "精神萎靡";
                        _tcmDiagnosisService.Voice = "声音低沉";
                        _tcmDiagnosisService.TongueBody = "舌淡";
                        _tcmDiagnosisService.Pulse = "脉弱";
                        break;
                    case "血虚证":
                        _tcmDiagnosisService.Complexion = "面色萎黄";
                        _tcmDiagnosisService.TongueBody = "舌淡白";
                        _tcmDiagnosisService.Pulse = "脉细";
                        break;
                    // 可以添加更多证型模式
                }
                
                _logger.LogInformation("应用证型模式: {Pattern}", pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用证型模式时发生错误: {Pattern}", pattern);
            }
        }

        /// <summary>
        /// 获取输入完整性百分比
        /// </summary>
        public int GetCompletionPercentage()
        {
            try
            {
                var totalFields = 20; // 总字段数
                var filledFields = 0;

                // 望诊字段
                if (!string.IsNullOrWhiteSpace(_tcmDiagnosisService.Complexion)) filledFields++;
                if (!string.IsNullOrWhiteSpace(_tcmDiagnosisService.Spirit)) filledFields++;
                if (!string.IsNullOrWhiteSpace(_tcmDiagnosisService.TongueBody)) filledFields++;
                if (!string.IsNullOrWhiteSpace(_tcmDiagnosisService.TongueCoating)) filledFields++;

                // 闻诊字段
                if (!string.IsNullOrWhiteSpace(_tcmDiagnosisService.Voice)) filledFields++;
                if (!string.IsNullOrWhiteSpace(_tcmDiagnosisService.Breathing)) filledFields++;
                if (!string.IsNullOrWhiteSpace(_tcmDiagnosisService.Cough)) filledFields++;

                // 问诊字段
                if (!string.IsNullOrWhiteSpace(_tcmDiagnosisService.ChiefComplaint)) filledFields++;
                if (!string.IsNullOrWhiteSpace(_tcmDiagnosisService.PresentIllness)) filledFields++;
                if (!string.IsNullOrWhiteSpace(_tcmDiagnosisService.ColdHeat)) filledFields++;
                if (!string.IsNullOrWhiteSpace(_tcmDiagnosisService.Sweating)) filledFields++;
                if (!string.IsNullOrWhiteSpace(_tcmDiagnosisService.Stools)) filledFields++;

                // 切诊字段
                if (!string.IsNullOrWhiteSpace(_tcmDiagnosisService.Pulse)) filledFields++;
                if (!string.IsNullOrWhiteSpace(_tcmDiagnosisService.PulseRate)) filledFields++;
                if (!string.IsNullOrWhiteSpace(_tcmDiagnosisService.Abdomen)) filledFields++;

                // 综合诊断
                if (!string.IsNullOrWhiteSpace(_tcmDiagnosisService.Syndrome)) filledFields++;
                if (!string.IsNullOrWhiteSpace(_tcmDiagnosisService.TreatmentPrinciple)) filledFields++;

                return (int)((double)filledFields / totalFields * 100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算完整性百分比时发生错误");
                return 0;
            }
        }

        #endregion

        #region 清理方法

        /// <summary>
        /// 清理资源和事件订阅
        /// </summary>
        public void Cleanup()
        {
            try
            {
                _tcmDiagnosisService.PropertyChanged -= OnDiagnosisServicePropertyChanged;
                _eventAggregator.GetEvent<MedicalCaseSelectedEvent>().Unsubscribe(OnMedicalCaseSelected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理TCMDiagnosisViewModel资源时发生错误");
            }
        }

        #endregion
    }
}