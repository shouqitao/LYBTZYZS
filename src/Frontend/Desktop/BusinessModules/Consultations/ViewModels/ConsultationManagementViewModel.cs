using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Desktop.Shared;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Consultation.Shared.ViewModels
{
    /// <summary>
    /// 看诊管理视图模型
    /// </summary>
    public class ConsultationManagementViewModel : BaseManagementViewModel<ConsultationDto>
    {
        private readonly ISharedConsultationService _consultationService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<ConsultationManagementViewModel> _logger;

        private string _searchKeyword = string.Empty;
        private ConsultationDto _selectedConsultation;
        private bool _showTodayOnly = false;

        public ConsultationManagementViewModel(
            ISharedConsultationService consultationService,
            IDialogService dialogService,
            ILogger<ConsultationManagementViewModel> logger)
            : base(logger)
        {
            _consultationService = consultationService;
            _dialogService = dialogService;
            _logger = logger;

            Title = "看诊管理";
            InitializeCommands();
            
            // 自动加载数据
            _ = LoadDataAsync();
        }

        #region Properties

        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        public ConsultationDto SelectedConsultation
        {
            get => _selectedConsultation;
            set
            {
                SetProperty(ref _selectedConsultation, value);
                UpdateCommandStates();
            }
        }

        public bool ShowTodayOnly
        {
            get => _showTodayOnly;
            set
            {
                SetProperty(ref _showTodayOnly, value);
                _ = LoadDataAsync();
            }
        }

        #endregion

        #region Commands

        public DelegateCommand SearchCommand { get; private set; }
        public DelegateCommand StartConsultationCommand { get; private set; }
        public DelegateCommand ViewConsultationCommand { get; private set; }
        public DelegateCommand EditConsultationCommand { get; private set; }
        public DelegateCommand FourExaminationsCommand { get; private set; }
        public DelegateCommand CompleteConsultationCommand { get; private set; }
        public DelegateCommand ExportConsultationCommand { get; private set; }
        public DelegateCommand ShowStatisticsCommand { get; private set; }

        #endregion

        #region Methods

        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            SearchCommand = new DelegateCommand(async () => await SearchConsultationsAsync());
            StartConsultationCommand = new DelegateCommand(async () => await StartConsultationAsync());
            ViewConsultationCommand = new DelegateCommand(async () => await ViewConsultationAsync(), () => SelectedConsultation != null);
            EditConsultationCommand = new DelegateCommand(async () => await EditConsultationAsync(), () => SelectedConsultation != null);
            FourExaminationsCommand = new DelegateCommand(async () => await OpenFourExaminationsAsync(), () => SelectedConsultation != null);
            CompleteConsultationCommand = new DelegateCommand(async () => await CompleteConsultationAsync(), () => SelectedConsultation != null);
            ExportConsultationCommand = new DelegateCommand(async () => await ExportConsultationAsync(), () => SelectedConsultation != null);
            ShowStatisticsCommand = new DelegateCommand(async () => await ShowStatisticsAsync());
        }

        protected override async Task LoadDataAsync()
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                var result = await _consultationService.GetConsultationsAsync(CurrentPage, PageSize, SearchKeyword);
                if (result.IsSuccess)
                {
                    var pagedData = result.Data;
                    var consultations = pagedData.Items;

                    // 应用今日过滤
                    if (_showTodayOnly)
                    {
                        consultations = consultations.Where(c => c.ConsultationTime.Date == DateTime.Now.Date).ToList();
                    }

                    Items = new ObservableCollection<ConsultationDto>(consultations);
                    TotalCount = _showTodayOnly ? consultations.Count : pagedData.TotalCount;
                    TotalPages = pagedData.TotalPages;

                    _logger.LogInformation("看诊数据加载完成，共 {Count} 条记录", TotalCount);
                }
                else
                {
                    ErrorMessage = result.ErrorMessage;
                    _logger.LogWarning("看诊数据加载失败: {Error}", result.ErrorMessage);
                }
            });
        }

        private async Task SearchConsultationsAsync()
        {
            CurrentPage = 1;
            await LoadDataAsync();
        }

        private async Task StartConsultationAsync()
        {
            try
            {
                var dialogParameters = new DialogParameters
                {
                    { "Title", "开始看诊" },
                    { "Mode", "Start" }
                };

                _dialogService.ShowDialog("PatientSelectionDialog", dialogParameters, async (result) =>
                {
                    if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("PatientId"))
                    {
                        var patientId = result.Parameters.GetValue<Guid>("PatientId");
                        var doctorId = Guid.NewGuid(); // TODO: 获取当前医生ID

                        await ExecuteWithLoadingAsync(async () =>
                        {
                            var serviceResult = await _consultationService.StartConsultationAsync(patientId, doctorId);
                            if (serviceResult.IsSuccess)
                            {
                                await LoadDataAsync();
                                ShowSuccessMessage("看诊开始成功");
                                _logger.LogInformation("看诊开始成功，患者ID: {PatientId}", patientId);
                            }
                            else
                            {
                                ErrorMessage = serviceResult.ErrorMessage;
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始看诊时发生错误");
                ErrorMessage = $"开始看诊时发生错误: {ex.Message}";
            }
        }

        private async Task ViewConsultationAsync()
        {
            if (SelectedConsultation == null) return;

            try
            {
                var dialogParameters = new DialogParameters
                {
                    { "Title", "看诊详情" },
                    { "Mode", "View" },
                    { "ConsultationId", SelectedConsultation.Id }
                };

                _dialogService.ShowDialog("ConsultationDialog", dialogParameters, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查看看诊详情时发生错误");
                ErrorMessage = $"查看看诊详情时发生错误: {ex.Message}";
            }
        }

        private async Task EditConsultationAsync()
        {
            if (SelectedConsultation == null) return;

            try
            {
                var dialogParameters = new DialogParameters
                {
                    { "Title", "编辑看诊" },
                    { "Mode", "Edit" },
                    { "ConsultationId", SelectedConsultation.Id }
                };

                _dialogService.ShowDialog("ConsultationDialog", dialogParameters, async (result) =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        await LoadDataAsync();
                        ShowSuccessMessage("看诊记录更新成功");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "编辑看诊时发生错误");
                ErrorMessage = $"编辑看诊时发生错误: {ex.Message}";
            }
        }

        private async Task OpenFourExaminationsAsync()
        {
            if (SelectedConsultation == null) return;

            try
            {
                var dialogParameters = new DialogParameters
                {
                    { "Title", "中医四诊" },
                    { "ConsultationId", SelectedConsultation.Id }
                };

                _dialogService.ShowDialog("FourExaminationsDialog", dialogParameters, async (result) =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        await LoadDataAsync();
                        ShowSuccessMessage("四诊信息保存成功");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开四诊对话框时发生错误");
                ErrorMessage = $"打开四诊对话框时发生错误: {ex.Message}";
            }
        }

        private async Task CompleteConsultationAsync()
        {
            if (SelectedConsultation == null) return;

            try
            {
                var confirmResult = MessageBox.Show(
                    $"确定要完成患者 '{SelectedConsultation.PatientName}' 的看诊吗？",
                    "确认完成",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    await ExecuteWithLoadingAsync(async () =>
                    {
                        var result = await _consultationService.CompleteConsultationAsync(
                            SelectedConsultation.Id, 
                            SelectedConsultation.Diagnosis, 
                            "标准治疗方案");

                        if (result.IsSuccess)
                        {
                            await LoadDataAsync();
                            ShowSuccessMessage("看诊完成");
                            _logger.LogInformation("看诊完成: {ConsultationId}", SelectedConsultation.Id);
                        }
                        else
                        {
                            ErrorMessage = result.ErrorMessage;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成看诊时发生错误");
                ErrorMessage = $"完成看诊时发生错误: {ex.Message}";
            }
        }

        private async Task ExportConsultationAsync()
        {
            if (SelectedConsultation == null) return;

            try
            {
                await ExecuteWithLoadingAsync(async () =>
                {
                    var result = await _consultationService.ExportConsultationAsync(SelectedConsultation.Id, "pdf");
                    if (result.IsSuccess)
                    {
                        ShowSuccessMessage("看诊记录导出成功");
                        _logger.LogInformation("看诊记录导出成功: {ConsultationId}", SelectedConsultation.Id);
                    }
                    else
                    {
                        ErrorMessage = result.ErrorMessage;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出看诊记录时发生错误");
                ErrorMessage = $"导出看诊记录时发生错误: {ex.Message}";
            }
        }

        private async Task ShowStatisticsAsync()
        {
            try
            {
                var dialogParameters = new DialogParameters
                {
                    { "Title", "看诊统计" },
                    { "DoctorId", Guid.NewGuid() } // TODO: 获取当前医生ID
                };

                _dialogService.ShowDialog("ConsultationStatisticsDialog", dialogParameters, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "显示看诊统计时发生错误");
                ErrorMessage = $"显示看诊统计时发生错误: {ex.Message}";
            }
        }

        private void UpdateCommandStates()
        {
            ViewConsultationCommand?.RaiseCanExecuteChanged();
            EditConsultationCommand?.RaiseCanExecuteChanged();
            FourExaminationsCommand?.RaiseCanExecuteChanged();
            CompleteConsultationCommand?.RaiseCanExecuteChanged();
            ExportConsultationCommand?.RaiseCanExecuteChanged();
        }

        private void ShowSuccessMessage(string message)
        {
            MessageBox.Show(message, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}