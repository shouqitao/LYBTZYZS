using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.Coordinators;
using LYBT.Desktop.Core.Managers;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.ViewModels.Patients;
using LYBT.Desktop.Services;
using LYBT.Desktop.Patients.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
// UltraThink四层架构重构：使用新的三层架构组件实现患者管理
// UltraThink v2.0: 添加SessionAware相关依赖
using LYBT.Desktop.Core.Interfaces.Services;

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>
    /// 患者管理视图模型（UltraThink架构重构版）
    /// 使用新的三层架构：PaginationCoordinator + SearchManager + NewBaseListViewModel
    /// 实现完全的关注点分离和单一职责原则
    /// </summary>
    public class PatientManagementViewModel : NewBaseListViewModel<PatientDto>
    {
        #region Fields

        private readonly PatientModuleService _patientService;
        private readonly ICustomDialogService _dialogService;
        private readonly IMapper _mapper;
        
        private ObservableCollection<PatientViewModel> _patientViewModels = new();
        private PatientViewModel? _selectedPatientViewModel;

        #endregion

        #region Properties

        /// <summary>患者视图模型集合 - 替代原始的PatientInfo集合</summary>
        public ObservableCollection<PatientViewModel> PatientViewModels
        {
            get => _patientViewModels;
            set => SetProperty(ref _patientViewModels, value);
        }

        /// <summary>选中的患者视图模型</summary>
        public PatientViewModel? SelectedPatientViewModel
        {
            get => _selectedPatientViewModel;
            set
            {
                if (SetProperty(ref _selectedPatientViewModel, value))
                {
                    // 更新命令状态
                    EditCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                    ToggleStatusCommand.RaiseCanExecuteChanged();
                    ViewDetailsCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>批量选中的患者数量</summary>
        public int SelectedPatientsCount => PatientViewModels.Count(p => p.IsSelected);

        /// <summary>是否有选中的患者</summary>
        public bool HasSelectedPatients => SelectedPatientsCount > 0;

        #endregion

        #region Commands

        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand<PatientViewModel> EditCommand { get; private set; }
        public DelegateCommand<PatientViewModel> DeleteCommand { get; private set; }
        public DelegateCommand<PatientViewModel> ToggleStatusCommand { get; private set; }
        public DelegateCommand<PatientViewModel> ViewDetailsCommand { get; private set; }
        public DelegateCommand BatchEnableCommand { get; private set; }
        public DelegateCommand BatchDisableCommand { get; private set; }
        public DelegateCommand ClearSelectionCommand { get; private set; }
        public DelegateCommand SelectAllCommand { get; private set; }

        #endregion

        #region Constructor

        public PatientManagementViewModel(
            PatientModuleService patientService,
            ICustomDialogService dialogService,
            IMapper mapper,
            ISessionManager sessionManager,
            INotificationService notificationService,
            ILogger<PatientManagementViewModel> logger,
            IPaginationCoordinator? paginationCoordinator = null,
            ISearchManager? searchManager = null)
            : base(sessionManager, notificationService, logger, paginationCoordinator, searchManager)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            InitializeCommands();
            
            // 监听选择状态变化
            PatientViewModels.CollectionChanged += (s, e) => UpdateSelectionProperties();
            
            // 初始化加载数据
            _ = RefreshDataAsync();
        }

        #endregion

        #region Command Initialization

        private void InitializeCommands()
        {
            AddCommand = new DelegateCommand(async () => await AddPatientAsync());
            EditCommand = new DelegateCommand<PatientViewModel>(async patient => await EditPatientAsync(patient), CanExecutePatientCommand);
            DeleteCommand = new DelegateCommand<PatientViewModel>(async patient => await DeletePatientAsync(patient), CanExecutePatientCommand);
            ToggleStatusCommand = new DelegateCommand<PatientViewModel>(async patient => await ToggleStatusAsync(patient), CanExecutePatientCommand);
            ViewDetailsCommand = new DelegateCommand<PatientViewModel>(async patient => await ViewDetailsAsync(patient), CanExecutePatientCommand);
            
            BatchEnableCommand = new DelegateCommand(async () => await BatchEnableAsync(), () => HasSelectedPatients);
            BatchDisableCommand = new DelegateCommand(async () => await BatchDisableAsync(), () => HasSelectedPatients);
            ClearSelectionCommand = new DelegateCommand(ClearSelection, () => HasSelectedPatients);
            SelectAllCommand = new DelegateCommand(SelectAll);
        }

        private bool CanExecutePatientCommand(PatientViewModel patient)
        {
            return patient != null && !IsLoading;
        }

        #endregion

        #region Data Loading Override

        protected override async Task<ServiceResult<PagedResult<PatientDto>>> LoadDataAsync(PagedQueryBaseDto request)
        {
            // UltraThink v2.0: 直接使用PagedQueryBaseDto
            var patientQuery = new PagedQueryBaseDto
            {
                PageIndex = request.CurrentPage,
                PageSize = request.PageSize,
                Keyword = request.SearchKeyword
            };

            return await _patientService.GetPagedAsync(patientQuery);
        }

        protected override void OnDataLoaded(PagedResult<PatientDto> data)
        {
            base.OnDataLoaded(data);
            
            // 将PatientDto转换为PatientViewModel
            UpdatePatientViewModels(data.Items);
        }

        protected override void OnDataLoadFailed(string errorMessage)
        {
            base.OnDataLoadFailed(errorMessage);
            
            // 清空患者视图模型
            PatientViewModels.Clear();
            UpdateSelectionProperties();
            
            // 显示错误
            _ = _dialogService.ShowErrorAsync(errorMessage, "加载失败");
        }

        #endregion

        #region Patient ViewModels Management

        private void UpdatePatientViewModels(System.Collections.Generic.List<PatientDto> patientDtos)
        {
            // 保存当前选择状态
            var selectedIds = PatientViewModels.Where(p => p.IsSelected).Select(p => p.Id).ToHashSet();
            
            // 清空并重新创建
            PatientViewModels.Clear();
            
            foreach (var dto in patientDtos)
            {
                // UltraThink v2.0: 直接使用DTO创建PatientViewModel
                var patientViewModel = PatientViewModel.Create(dto);
                
                // 恢复选择状态
                if (selectedIds.Contains(patientViewModel.Id))
                {
                    patientViewModel.IsSelected = true;
                }
                
                // 监听选择状态变化
                patientViewModel.State.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(PatientStateViewModel.IsSelected))
                    {
                        UpdateSelectionProperties();
                    }
                };
                
                PatientViewModels.Add(patientViewModel);
            }
            
            UpdateSelectionProperties();
        }

        private void UpdateSelectionProperties()
        {
            RaisePropertyChanged(nameof(SelectedPatientsCount));
            RaisePropertyChanged(nameof(HasSelectedPatients));
            
            BatchEnableCommand.RaiseCanExecuteChanged();
            BatchDisableCommand.RaiseCanExecuteChanged();
            ClearSelectionCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region CRUD Operations

        private async Task AddPatientAsync()
        {
            try
            {
                // TODO: 实现患者创建对话框
                await _dialogService.ShowInformationAsync("新增患者功能开发中", "提示");
            }
            catch (Exception ex)
            {
                LogError(ex, "添加患者失败");
                ShowError($"添加患者失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"添加患者失败: {ex.Message}", "错误");
            }
        }

        private async Task EditPatientAsync(PatientViewModel patientViewModel)
        {
            if (patientViewModel == null) return;
            
            try
            {
                // TODO: 实现患者编辑对话框
                await _dialogService.ShowInformationAsync($"编辑患者 {patientViewModel.DisplayName} 功能开发中", "提示");
            }
            catch (Exception ex)
            {
                LogError(ex, "编辑患者失败: {PatientId}", patientViewModel.Id);
                ShowError($"编辑患者失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"编辑患者失败: {ex.Message}", "错误");
            }
        }

        private async Task DeletePatientAsync(PatientViewModel patientViewModel)
        {
            if (patientViewModel == null) return;
            
            // 患者信息不支持真正删除，只能禁用
            await ToggleStatusAsync(patientViewModel);
        }

        #endregion

        #region Business Operations

        private async Task ToggleStatusAsync(PatientViewModel patientViewModel)
        {
            if (patientViewModel == null) return;

            var isEnabled = patientViewModel.PatientData.Status == CommonStatus.Enabled;
            var action = isEnabled ? "禁用" : "启用";
            
            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要{action}患者 {patientViewModel.DisplayName} 吗？",
                $"{action}患者");

            if (confirm)
            {
                try
                {
                    patientViewModel.IsLoading = true;
                    
                    ServiceResult result;
                    if (isEnabled)
                    {
                        result = await _patientService.DisableAsync(patientViewModel.Id);
                    }
                    else
                    {
                        result = await _patientService.EnableAsync(patientViewModel.Id);
                    }

                    if (result.IsSuccess)
                    {
                        await RefreshDataAsync();
                        await _dialogService.ShowInformationAsync($"患者{action}成功", "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                            result.ErrorMessage ?? $"患者{action}失败",
                            "错误");
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "切换患者状态失败: {PatientId}", patientViewModel.Id);
                    ShowError($"患者{action}失败: {ex.Message}");
                    await _dialogService.ShowErrorAsync($"患者{action}失败: {ex.Message}", "错误");
                }
                finally
                {
                    patientViewModel.IsLoading = false;
                }
            }
        }

        private async Task ViewDetailsAsync(PatientViewModel patientViewModel)
        {
            if (patientViewModel == null) return;

            try
            {
                patientViewModel.IsLoading = true;
                
                var result = await _patientService.GetByIdAsync(patientViewModel.Id);
                
                if (result.IsSuccess && result.Data != null)
                {
                    var patient = result.Data;
                    var detailInfo = $"患者详情：\n\n" +
                                   $"姓名: {patient.Name}\n" +
                                   $"性别: {(patient.Gender == LYBT.Shared.Models.Enums.Gender.Male ? "男" : patient.Gender == LYBT.Shared.Models.Enums.Gender.Female ? "女" : "未知")}\n" +
                                   $"年龄: {patient.Age}岁\n" +
                                   $"电话: {patient.PhoneNumber ?? "未填写"}\n" +
                                   $"证件号: {patient.IdNumber ?? "未填写"}\n" +
                                   $"地址: {patient.Address ?? "未填写"}\n" +
                                   $"状态: {(patient.Status == CommonStatus.Enabled ? "正常" : "禁用")}\n" +
                                   $"过敏史: {patient.AllergyHistory ?? "无"}";

                    await _dialogService.ShowInformationAsync(detailInfo, $"患者详情 - {patient.Name}");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(
                        result.ErrorMessage ?? "获取患者详情失败", 
                        "错误");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "查看患者详情失败: {PatientId}", patientViewModel.Id);
                ShowError($"查看患者详情失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"查看患者详情失败: {ex.Message}", "错误");
            }
            finally
            {
                patientViewModel.IsLoading = false;
            }
        }

        #endregion

        #region Batch Operations

        private async Task BatchEnableAsync()
        {
            var selectedPatients = PatientViewModels.Where(p => p.IsSelected).ToList();
            if (!selectedPatients.Any()) return;

            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要启用选中的 {selectedPatients.Count} 个患者吗？",
                "批量启用");

            if (confirm)
            {
                try
                {
                    // UltraThink v2.0: 使用循环调用单个操作替代批量操作（简化原则）
                    int successCount = 0;
                    foreach (var patient in selectedPatients)
                    {
                        var result = await _patientService.EnableAsync(patient.Id);
                        if (result.IsSuccess)
                        {
                            successCount++;
                        }
                    }

                    await RefreshDataAsync();
                    await _dialogService.ShowInformationAsync($"已成功启用 {successCount} 个患者", "成功");
                }
                catch (Exception ex)
                {
                    LogError(ex, "批量启用患者失败");
                    ShowError($"批量启用失败: {ex.Message}");
                    await _dialogService.ShowErrorAsync($"批量启用失败: {ex.Message}", "错误");
                }
            }
        }

        private async Task BatchDisableAsync()
        {
            var selectedPatients = PatientViewModels.Where(p => p.IsSelected).ToList();
            if (!selectedPatients.Any())
            {
                await _dialogService.ShowWarningAsync("没有选中的患者", "警告");
                return;
            }

            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要禁用选中的 {selectedPatients.Count} 个患者吗？",
                "批量禁用");

            if (confirm)
            {
                try
                {
                    // UltraThink v2.0: 使用循环调用单个操作替代批量操作（简化原则）
                    int successCount = 0;
                    foreach (var patient in selectedPatients)
                    {
                        var result = await _patientService.DisableAsync(patient.Id);
                        if (result.IsSuccess)
                        {
                            successCount++;
                        }
                    }

                    await RefreshDataAsync();
                    await _dialogService.ShowInformationAsync($"已成功禁用 {successCount} 个患者", "成功");
                }
                catch (Exception ex)
                {
                    LogError(ex, "批量禁用患者失败");
                    ShowError($"批量禁用失败: {ex.Message}");
                    await _dialogService.ShowErrorAsync($"批量禁用失败: {ex.Message}", "错误");
                }
            }
        }

        #endregion

        #region Selection Management

        private void ClearSelection()
        {
            foreach (var patient in PatientViewModels)
            {
                patient.IsSelected = false;
            }
        }

        private void SelectAll()
        {
            foreach (var patient in PatientViewModels)
            {
                patient.IsSelected = true;
            }
        }

        #endregion
    }
}