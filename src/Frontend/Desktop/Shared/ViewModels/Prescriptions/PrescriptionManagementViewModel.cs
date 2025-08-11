using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Desktop.Shared;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Navigation.Regions;
using Prism.Dialogs;

namespace LYBT.Desktop.Shared.ViewModels.Prescriptions
{
    /// <summary>
    /// 处方管理视图模型
    /// </summary>
    public class PrescriptionManagementViewModel : BaseServiceManagementViewModel<PrescriptionDto>
    {
        private readonly ISharedPrescriptionService _prescriptionService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<PrescriptionManagementViewModel> _logger;

        private string _searchKeyword = string.Empty;
        private PrescriptionDto _selectedPrescription;
        private PrescriptionStatus? _filterStatus = null;
        private bool _showTodayOnly = false;

        public PrescriptionManagementViewModel(
            ISharedPrescriptionService prescriptionService,
            IDialogService dialogService,
            ILogger<PrescriptionManagementViewModel> logger)
            : base(logger)
        {
            _prescriptionService = prescriptionService;
            _dialogService = dialogService;
            _logger = logger;

            Title = "处方管理";
            InitializeCommands();
            
            // 自动加载数据
            _ = LoadDataAsync();
        }

        #region Properties

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        /// <summary>
        /// 选中的处方
        /// </summary>
        public PrescriptionDto SelectedPrescription
        {
            get => _selectedPrescription;
            set
            {
                SetProperty(ref _selectedPrescription, value);
                UpdateCommandStates();
            }
        }

        /// <summary>
        /// 过滤状态
        /// </summary>
        public PrescriptionStatus? FilterStatus
        {
            get => _filterStatus;
            set
            {
                SetProperty(ref _filterStatus, value);
                _ = LoadDataAsync();
            }
        }

        /// <summary>
        /// 只显示今日处方
        /// </summary>
        public bool ShowTodayOnly
        {
            get => _showTodayOnly;
            set
            {
                SetProperty(ref _showTodayOnly, value);
                _ = LoadDataAsync();
            }
        }

        /// <summary>
        /// 处方状态选项
        /// </summary>
        public Array PrescriptionStatusOptions => Enum.GetValues(typeof(PrescriptionStatus));

        #endregion

        #region Commands

        public DelegateCommand SearchCommand { get; private set; }
        public DelegateCommand AddPrescriptionCommand { get; private set; }
        public DelegateCommand EditPrescriptionCommand { get; private set; }
        public DelegateCommand ViewPrescriptionCommand { get; private set; }
        public DelegateCommand CopyPrescriptionCommand { get; private set; }
        public DelegateCommand ValidatePrescriptionCommand { get; private set; }
        public DelegateCommand PrintPrescriptionCommand { get; private set; }
        public DelegateCommand VoidPrescriptionCommand { get; private set; }
        public DelegateCommand SubmitPrescriptionCommand { get; private set; }
        public DelegateCommand SaveDraftCommand { get; private set; }
        public DelegateCommand ShowTemplatesCommand { get; private set; }

        #endregion

        #region Methods

        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            SearchCommand = new DelegateCommand(async () => await SearchPrescriptionsAsync());
            AddPrescriptionCommand = new DelegateCommand(async () => await AddPrescriptionAsync());
            EditPrescriptionCommand = new DelegateCommand(async () => await EditPrescriptionAsync(), () => CanEditPrescription());
            ViewPrescriptionCommand = new DelegateCommand(async () => await ViewPrescriptionAsync(), () => SelectedPrescription != null);
            CopyPrescriptionCommand = new DelegateCommand(async () => await CopyPrescriptionAsync(), () => SelectedPrescription != null);
            ValidatePrescriptionCommand = new DelegateCommand(async () => await ValidatePrescriptionAsync(), () => SelectedPrescription != null);
            PrintPrescriptionCommand = new DelegateCommand(async () => await PrintPrescriptionAsync(), () => CanPrintPrescription());
            VoidPrescriptionCommand = new DelegateCommand(async () => await VoidPrescriptionAsync(), () => CanVoidPrescription());
            SubmitPrescriptionCommand = new DelegateCommand(async () => await SubmitPrescriptionAsync(), () => CanSubmitPrescription());
            SaveDraftCommand = new DelegateCommand(async () => await SaveDraftAsync(), () => SelectedPrescription != null);
            ShowTemplatesCommand = new DelegateCommand(async () => await ShowTemplatesAsync());
        }

        protected override async Task LoadDataAsync()
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                // TODO: 实现真实的分页查询逻辑
                // 目前使用模拟数据演示功能
                await Task.Delay(500);

                // 生成模拟处方数据
                var mockPrescriptions = GenerateMockPrescriptions();

                // 应用过滤器
                var filteredPrescriptions = mockPrescriptions.AsEnumerable();

                if (_filterStatus.HasValue)
                {
                    filteredPrescriptions = filteredPrescriptions.Where(p => p.Status == _filterStatus.Value);
                }

                if (_showTodayOnly)
                {
                    filteredPrescriptions = filteredPrescriptions.Where(p => p.CreateTime.Date == DateTime.Now.Date);
                }

                if (!string.IsNullOrEmpty(SearchKeyword))
                {
                    filteredPrescriptions = filteredPrescriptions.Where(p =>
                        p.PatientName?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) == true ||
                        p.DoctorName?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) == true ||
                        p.Diagnosis?.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) == true);
                }

                var prescriptionList = filteredPrescriptions.ToList();
                Items = new ObservableCollection<PrescriptionDto>(prescriptionList);
                TotalCount = prescriptionList.Count;
                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

                _logger.LogInformation("处方数据加载完成，共 {Count} 条记录", TotalCount);
            });
        }

        /// <summary>
        /// 搜索处方
        /// </summary>
        private async Task SearchPrescriptionsAsync()
        {
            await LoadDataAsync();
        }

        /// <summary>
        /// 添加处方
        /// </summary>
        private async Task AddPrescriptionAsync()
        {
            try
            {
                var dialogParameters = new DialogParameters
                {
                    { "Title", "添加处方" },
                    { "Mode", "Add" }
                };

                _dialogService.ShowDialog("PrescriptionAddEditDialog", dialogParameters, async (result) =>
                {
                    if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("PrescriptionData"))
                    {
                        var prescriptionDto = result.Parameters.GetValue<PrescriptionDto>("PrescriptionData");
                        await ExecuteWithLoadingAsync(async () =>
                        {
                            var serviceResult = await _prescriptionService.CreatePrescriptionAsync(prescriptionDto);
                            if (serviceResult.IsSuccess)
                            {
                                await LoadDataAsync(); // 刷新列表
                                ShowSuccessMessage("处方添加成功");
                                _logger.LogInformation("处方添加成功，患者: {PatientName}", prescriptionDto.PatientName);
                            }
                            else
                            {
                                ErrorMessage = serviceResult.ErrorMessage;
                                _logger.LogWarning("处方添加失败: {Error}", serviceResult.ErrorMessage);
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加处方时发生错误");
                ErrorMessage = $"添加处方时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 编辑处方
        /// </summary>
        private async Task EditPrescriptionAsync()
        {
            if (SelectedPrescription == null)
                return;

            try
            {
                var dialogParameters = new DialogParameters
                {
                    { "Title", "编辑处方" },
                    { "Mode", "Edit" },
                    { "PrescriptionId", SelectedPrescription.Id }
                };

                _dialogService.ShowDialog("PrescriptionAddEditDialog", dialogParameters, async (result) =>
                {
                    if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("PrescriptionData"))
                    {
                        await LoadDataAsync(); // 刷新列表
                        ShowSuccessMessage("处方更新成功");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "编辑处方时发生错误");
                ErrorMessage = $"编辑处方时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 查看处方详情
        /// </summary>
        private async Task ViewPrescriptionAsync()
        {
            if (SelectedPrescription == null)
                return;

            try
            {
                var dialogParameters = new DialogParameters
                {
                    { "Title", "处方详情" },
                    { "Mode", "View" },
                    { "PrescriptionId", SelectedPrescription.Id }
                };

                _dialogService.ShowDialog("PrescriptionDetailDialog", dialogParameters, (Action<IDialogResult>)null!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查看处方详情时发生错误");
                ErrorMessage = $"查看处方详情时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 复制处方
        /// </summary>
        private async Task CopyPrescriptionAsync()
        {
            if (SelectedPrescription == null)
                return;

            try
            {
                // TODO: 选择新患者的对话框
                var newPatientId = Guid.NewGuid(); // 临时使用随机ID

                await ExecuteWithLoadingAsync(async () =>
                {
                    var result = await _prescriptionService.CopyPrescriptionAsync(SelectedPrescription.Id, newPatientId);
                    if (result.IsSuccess)
                    {
                        await LoadDataAsync(); // 刷新列表
                        ShowSuccessMessage("处方复制成功");
                        _logger.LogInformation("处方复制成功: {PrescriptionId}", SelectedPrescription.Id);
                    }
                    else
                    {
                        ErrorMessage = result.ErrorMessage;
                        _logger.LogWarning("处方复制失败: {Error}", result.ErrorMessage);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复制处方时发生错误");
                ErrorMessage = $"复制处方时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 验证处方
        /// </summary>
        private async Task ValidatePrescriptionAsync()
        {
            if (SelectedPrescription == null)
                return;

            try
            {
                await ExecuteWithLoadingAsync(async () =>
                {
                    var result = await _prescriptionService.ValidatePrescriptionAsync(SelectedPrescription);
                    if (result.IsSuccess)
                    {
                        var validationMessages = result.Data;
                        var message = string.Join("\n", validationMessages);
                        ShowValidationResult(message);
                        _logger.LogInformation("处方验证完成: {PrescriptionId}", SelectedPrescription.Id);
                    }
                    else
                    {
                        ErrorMessage = result.ErrorMessage;
                        _logger.LogWarning("处方验证失败: {Error}", result.ErrorMessage);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证处方时发生错误");
                ErrorMessage = $"验证处方时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 打印处方
        /// </summary>
        private async Task PrintPrescriptionAsync()
        {
            if (SelectedPrescription == null)
                return;

            try
            {
                await ExecuteWithLoadingAsync(async () =>
                {
                    var result = await _prescriptionService.PrintPrescriptionAsync(SelectedPrescription.Id);
                    if (result.IsSuccess)
                    {
                        ShowSuccessMessage("处方打印成功");
                        _logger.LogInformation("处方打印成功: {PrescriptionId}", SelectedPrescription.Id);
                    }
                    else
                    {
                        ErrorMessage = result.ErrorMessage;
                        _logger.LogWarning("处方打印失败: {Error}", result.ErrorMessage);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打印处方时发生错误");
                ErrorMessage = $"打印处方时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 作废处方
        /// </summary>
        private async Task VoidPrescriptionAsync()
        {
            if (SelectedPrescription == null)
                return;

            try
            {
                var confirmResult = MessageBox.Show(
                    $"确定要作废处方 '{SelectedPrescription.PatientName}' 的处方吗？\n此操作不可撤销。",
                    "确认作废",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    // TODO: 输入作废原因的对话框
                    var reason = "用户操作作废"; // 临时使用固定原因

                    await ExecuteWithLoadingAsync(async () =>
                    {
                        var result = await _prescriptionService.VoidPrescriptionAsync(SelectedPrescription.Id, reason);
                        if (result.IsSuccess)
                        {
                            await LoadDataAsync(); // 刷新列表
                            ShowSuccessMessage("处方作废成功");
                            _logger.LogInformation("处方作废成功: {PrescriptionId}", SelectedPrescription.Id);
                        }
                        else
                        {
                            ErrorMessage = result.ErrorMessage;
                            _logger.LogWarning("处方作废失败: {Error}", result.ErrorMessage);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "作废处方时发生错误");
                ErrorMessage = $"作废处方时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 提交处方
        /// </summary>
        private async Task SubmitPrescriptionAsync()
        {
            if (SelectedPrescription == null)
                return;

            try
            {
                await ExecuteWithLoadingAsync(async () =>
                {
                    var result = await _prescriptionService.SubmitPrescriptionAsync(SelectedPrescription.Id);
                    if (result.IsSuccess)
                    {
                        await LoadDataAsync(); // 刷新列表
                        ShowSuccessMessage("处方提交成功");
                        _logger.LogInformation("处方提交成功: {PrescriptionId}", SelectedPrescription.Id);
                    }
                    else
                    {
                        ErrorMessage = result.ErrorMessage;
                        _logger.LogWarning("处方提交失败: {Error}", result.ErrorMessage);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提交处方时发生错误");
                ErrorMessage = $"提交处方时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 保存草稿
        /// </summary>
        private async Task SaveDraftAsync()
        {
            if (SelectedPrescription == null)
                return;

            try
            {
                await ExecuteWithLoadingAsync(async () =>
                {
                    var result = await _prescriptionService.SavePrescriptionDraftAsync(SelectedPrescription);
                    if (result.IsSuccess)
                    {
                        ShowSuccessMessage("草稿保存成功");
                        _logger.LogInformation("处方草稿保存成功: {PrescriptionId}", SelectedPrescription.Id);
                    }
                    else
                    {
                        ErrorMessage = result.ErrorMessage;
                        _logger.LogWarning("处方草稿保存失败: {Error}", result.ErrorMessage);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存处方草稿时发生错误");
                ErrorMessage = $"保存处方草稿时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 显示处方模板
        /// </summary>
        private async Task ShowTemplatesAsync()
        {
            try
            {
                var dialogParameters = new DialogParameters
                {
                    { "Title", "处方模板" }
                };

                _dialogService.ShowDialog("PrescriptionTemplatesDialog", dialogParameters, (Action<IDialogResult>)null!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "显示处方模板时发生错误");
                ErrorMessage = $"显示处方模板时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 更新命令状态
        /// </summary>
        private void UpdateCommandStates()
        {
            EditPrescriptionCommand?.RaiseCanExecuteChanged();
            ViewPrescriptionCommand?.RaiseCanExecuteChanged();
            CopyPrescriptionCommand?.RaiseCanExecuteChanged();
            ValidatePrescriptionCommand?.RaiseCanExecuteChanged();
            PrintPrescriptionCommand?.RaiseCanExecuteChanged();
            VoidPrescriptionCommand?.RaiseCanExecuteChanged();
            SubmitPrescriptionCommand?.RaiseCanExecuteChanged();
            SaveDraftCommand?.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 是否可以编辑处方
        /// </summary>
        private bool CanEditPrescription()
        {
            return SelectedPrescription != null && 
                   SelectedPrescription.Status == PrescriptionStatus.Draft;
        }

        /// <summary>
        /// 是否可以打印处方
        /// </summary>
        private bool CanPrintPrescription()
        {
            return SelectedPrescription != null && 
                   SelectedPrescription.Status != PrescriptionStatus.Draft;
        }

        /// <summary>
        /// 是否可以作废处方
        /// </summary>
        private bool CanVoidPrescription()
        {
            return SelectedPrescription != null && 
                   SelectedPrescription.Status != PrescriptionStatus.Completed;
        }

        /// <summary>
        /// 是否可以提交处方
        /// </summary>
        private bool CanSubmitPrescription()
        {
            return SelectedPrescription != null && 
                   SelectedPrescription.Status == PrescriptionStatus.Draft;
        }

        /// <summary>
        /// 显示成功消息
        /// </summary>
        private void ShowSuccessMessage(string message)
        {
            // TODO: 实现更好的成功消息提示
            MessageBox.Show(message, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 显示验证结果
        /// </summary>
        private void ShowValidationResult(string message)
        {
            MessageBox.Show(message, "验证结果", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 生成模拟处方数据
        /// </summary>
        private List<PrescriptionDto> GenerateMockPrescriptions()
        {
            return new List<PrescriptionDto>
            {
                new PrescriptionDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "张三",
                    DoctorId = Guid.NewGuid(),
                    DoctorName = "王医生",
                    Diagnosis = "风寒感冒",
                    DosageCount = 7,
                    SingleDosePrice = 42.5m,
                    TotalPrice = 297.5m,
                    TotalWeight = 196.5m,
                    Status = PrescriptionStatus.Completed,
                    Advice = "温水送服，忌食生冷",
                    Items = new List<PrescriptionItemDto>(),
                    CreateTime = DateTime.Now.AddDays(-15),
                    UpdateTime = DateTime.Now.AddDays(-14)
                },
                new PrescriptionDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "李四",
                    DoctorId = Guid.NewGuid(),
                    DoctorName = "李医生",
                    Diagnosis = "脾胃虚弱",
                    DosageCount = 14,
                    SingleDosePrice = 38.8m,
                    TotalPrice = 543.2m,
                    TotalWeight = 280.0m,
                    Status = PrescriptionStatus.Draft,
                    Advice = "饭前30分钟温服",
                    Items = new List<PrescriptionItemDto>(),
                    CreateTime = DateTime.Now.AddDays(-8),
                    UpdateTime = DateTime.Now.AddDays(-7)
                },
                new PrescriptionDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "王五",
                    DoctorId = Guid.NewGuid(),
                    DoctorName = "赵医生",
                    Diagnosis = "肝郁气滞",
                    DosageCount = 10,
                    SingleDosePrice = 55.2m,
                    TotalPrice = 552.0m,
                    TotalWeight = 310.5m,
                    Status = PrescriptionStatus.Draft,
                    Advice = "情志调畅，规律服药",
                    Items = new List<PrescriptionItemDto>(),
                    CreateTime = DateTime.Now.AddDays(-3),
                    UpdateTime = DateTime.Now.AddDays(-2)
                },
                new PrescriptionDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "陈六",
                    DoctorId = Guid.NewGuid(),
                    DoctorName = "孙医生",
                    Diagnosis = "肾阳虚",
                    DosageCount = 21,
                    SingleDosePrice = 62.3m,
                    TotalPrice = 1308.3m,
                    TotalWeight = 420.0m,
                    Status = PrescriptionStatus.Completed,
                    Advice = "温补肾阳，慎起居",
                    Items = new List<PrescriptionItemDto>(),
                    CreateTime = DateTime.Now.AddHours(-6),
                    UpdateTime = DateTime.Now.AddHours(-4)
                }
            };
        }

        #endregion
    }
}