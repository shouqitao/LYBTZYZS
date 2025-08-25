using System;
using System.Collections.Generic;
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
    /// 患者管理视图模型（UltraThink v2.0 小诊所精简版）
    /// 移除过度设计的批量操作、多选功能，专注核心CRUD操作
    /// 适用于20人以下小诊所的简单直接的患者管理需求
    /// </summary>
    public class PatientManagementViewModel : NewBaseListViewModel<PatientDto>
    {
        #region Fields

        private readonly PatientModuleService _patientService;
        private readonly ICustomDialogService _dialogService;
        private readonly IMapper _mapper;
        
        // UltraThink v2.0: 直接使用DTO，移除复杂的ViewModel包装
        private PatientDto? _selectedPatient;

        #endregion

        #region Properties

        /// <summary>选中的患者 - UltraThink v2.0: 直接使用DTO</summary>
        public PatientDto? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (SetProperty(ref _selectedPatient, value))
                {
                    // 更新命令状态
                    EditCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                    ToggleStatusCommand.RaiseCanExecuteChanged();
                    ViewDetailsCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // UltraThink v2.0: 删除批量选择功能 - 20人以下小诊所不需要复杂的多选和批量操作
        // 基础搜索功能已经通过NewBaseListViewModel的SearchManager提供

        #endregion

        #region Commands

        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand<PatientDto> EditCommand { get; private set; }
        public DelegateCommand<PatientDto> DeleteCommand { get; private set; }
        public DelegateCommand<PatientDto> ToggleStatusCommand { get; private set; }
        public DelegateCommand<PatientDto> ViewDetailsCommand { get; private set; }

        // UltraThink v2.0: 删除过度设计功能 - 20人以下小诊所不需要以下复杂功能:
        // - BatchEnableCommand/BatchDisableCommand: 批量操作过度设计
        // - ClearSelectionCommand/SelectAllCommand: 多选功能过度设计

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
            
            // UltraThink v2.0: 删除复杂初始化逻辑
            // - 删除选择状态变化监听: 多选功能已移除
            // - 删除RefreshDataAsync(): 直接使用基类的数据加载机制
        }

        #endregion

        #region Command Initialization

        protected override void InitializeCommands()
        {
            AddCommand = new DelegateCommand(async () => await AddPatientAsync());
            EditCommand = new DelegateCommand<PatientDto>(async patient => await EditPatientAsync(patient), CanExecutePatientCommand);
            DeleteCommand = new DelegateCommand<PatientDto>(async patient => await DeletePatientAsync(patient), CanExecutePatientCommand);
            ToggleStatusCommand = new DelegateCommand<PatientDto>(async patient => await ToggleStatusAsync(patient), CanExecutePatientCommand);
            ViewDetailsCommand = new DelegateCommand<PatientDto>(async patient => await ViewDetailsAsync(patient), CanExecutePatientCommand);
            
            // UltraThink v2.0: 删除批量操作命令初始化 - 20人以下小诊所不需要复杂的批量操作
        }

        private bool CanExecutePatientCommand(PatientDto patient)
        {
            return patient != null && !IsLoading;
        }

        #endregion

        #region Data Loading Override

        protected override async Task<ServiceResult<PagedResult<PatientDto>>> LoadDataAsync(PagedQueryBaseDto request)
        {
            // UltraThink v2.0: 转换为PatientPagedQueryDto进行患者查询
            var patientQuery = new PatientPagedQueryDto
            {
                Keyword = request.Keyword,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
                SortField = request.SortField,
                IsDescending = request.IsDescending
            };
            return await _patientService.GetPagedAsync(patientQuery);
        }

        // UltraThink v2.0: 删除复杂的ViewModel转换和选择状态管理
        // 直接使用基类的标准数据加载处理，无需自定义OnDataLoaded和OnDataLoadFailed

        #endregion

        // UltraThink v2.0: 删除复杂的ViewModel管理 - 20人以下小诊所不需要复杂的选择状态管理
        // 直接使用基类提供的Data属性访问PagedResult<PatientDto>数据

        #region CRUD Operations

        private async Task AddPatientAsync()
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["IsEditMode"] = false
                };
                
                var result = await _dialogService.ShowDialogAsync("PatientAddEditDialog", parameters);
                
                if (result.Result == true)
                {
                    await RefreshDataAsync();
                    await _dialogService.ShowSuccessAsync("患者信息添加成功", "成功");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "添加患者失败");
                ShowError($"添加患者失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"添加患者失败: {ex.Message}", "错误");
            }
        }

        private async Task EditPatientAsync(PatientDto patient)
        {
            if (patient == null) return;
            
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["IsEditMode"] = true,
                    ["Patient"] = patient
                };
                
                var result = await _dialogService.ShowDialogAsync("PatientAddEditDialog", parameters);
                
                if (result.Result == true)
                {
                    await RefreshDataAsync();
                    await _dialogService.ShowSuccessAsync($"患者 {patient.Name} 信息更新成功", "成功");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "编辑患者失败: {PatientId}", patient.Id);
                ShowError($"编辑患者失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"编辑患者失败: {ex.Message}", "错误");
            }
        }

        private async Task DeletePatientAsync(PatientDto patient)
        {
            if (patient == null) return;
            
            // 患者信息不支持真正删除，只能禁用
            await ToggleStatusAsync(patient);
        }

        #endregion

        #region Business Operations

        private async Task ToggleStatusAsync(PatientDto patient)
        {
            if (patient == null) return;

            var isEnabled = patient.Status == CommonStatus.Enabled;
            var action = isEnabled ? "禁用" : "启用";
            
            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要{action}患者 {patient.Name} 吗？",
                $"{action}患者");

            if (confirm)
            {
                try
                {
                    ServiceResult result;
                    if (isEnabled)
                    {
                        result = await _patientService.DisableAsync(patient.Id);
                    }
                    else
                    {
                        result = await _patientService.EnableAsync(patient.Id);
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
                    LogError(ex, "切换患者状态失败: {PatientId}", patient.Id);
                    ShowError($"患者{action}失败: {ex.Message}");
                    await _dialogService.ShowErrorAsync($"患者{action}失败: {ex.Message}", "错误");
                }
            }
        }

        private async Task ViewDetailsAsync(PatientDto patient)
        {
            if (patient == null) return;

            try
            {
                var result = await _patientService.GetByIdAsync(patient.Id);
                
                if (result.IsSuccess && result.Data != null)
                {
                    var patientDetail = result.Data;
                    var detailInfo = $"患者详情：\n\n" +
                                   $"姓名: {patientDetail.Name}\n" +
                                   $"性别: {(patientDetail.Gender == LYBT.Shared.Models.Enums.Gender.Male ? "男" : patientDetail.Gender == LYBT.Shared.Models.Enums.Gender.Female ? "女" : "未知")}\n" +
                                   $"年龄: {patientDetail.Age}岁\n" +
                                   $"电话: {patientDetail.PhoneNumber ?? "未填写"}\n" +
                                   $"证件号: {patientDetail.IdNumber ?? "未填写"}\n" +
                                   $"地址: {patientDetail.Address ?? "未填写"}\n" +
                                   $"状态: {(patientDetail.Status == CommonStatus.Enabled ? "正常" : "禁用")}\n" +
                                   $"过敏史: {patientDetail.AllergyHistory ?? "无"}";

                    await _dialogService.ShowInformationAsync(detailInfo, $"患者详情 - {patientDetail.Name}");
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
                LogError(ex, "查看患者详情失败: {PatientId}", patient.Id);
                ShowError($"查看患者详情失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"查看患者详情失败: {ex.Message}", "错误");
            }
        }

        #endregion

        // UltraThink v2.0: 删除所有批量操作功能 - 20人以下小诊所不需要复杂的批量操作
        // 包括: BatchEnableAsync, BatchDisableAsync 等功能

        // UltraThink v2.0: 删除所有选择管理功能 - 20人以下小诊所不需要复杂的多选功能
        // 包括: ClearSelection, SelectAll 等功能
    }
}