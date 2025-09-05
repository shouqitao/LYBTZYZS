using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Coordinators;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Managers;
using LYBT.Desktop.Core.Mvvm; // ✅ 添加AsyncRelayCommand支持
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;
using Prism.Commands;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 医疗案例管理视图模型 - UltraThink双层架构UI层
    /// 采用UltraThink架构标准，使用C# 12现代化特性
    /// 职责：医疗案例列表管理、搜索过滤、分页展示、CRUD操作交互
    /// 基于NewBaseListViewModel统一列表管理模式，集成AsyncRelayCommand异步命令
    /// 支持医案创建、查看、编辑、删除、处方开具等完整诊疗流程管理
    /// 适配中医诊所医案管理界面，确保用户体验和数据操作安全性
    /// </summary>
#pragma warning disable CS0618 // NewBaseListViewModel已过时，计划未来架构升级
    public class MedicalCaseManagementViewModel : NewBaseListViewModel<MedicalCaseDto>
#pragma warning restore CS0618
    {
        #region Fields

        private readonly IMedicalCaseService _medicalCaseService;
        private readonly ICustomDialogService _dialogService;
        private readonly ILogger<MedicalCaseManagementViewModel> _logger;

        #endregion

        #region Properties

        private string _searchKeyword = string.Empty;
        /// <summary>
        /// 搜索关键词（患者姓名或案例编号）
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        private string _filterStatus = "全部状态";
        /// <summary>
        /// 过滤状态
        /// </summary>
        public string FilterStatus
        {
            get => _filterStatus;
            set => SetProperty(ref _filterStatus, value);
        }

        private DateTime? _startDate;
        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        private DateTime? _endDate;
        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime? EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        // 暴露基类的分页和搜索属性供XAML绑定
        public int CurrentPage => PaginationCoordinator.CurrentPage;
        public int TotalPages => PaginationCoordinator.TotalPages;
        public string StatusText => $"共 {PaginationCoordinator.TotalCount} 条记录";

        #endregion

        #region Commands

        // ✅ 使用AsyncRelayCommand替代DelegateCommand避免async void
        public AsyncRelayCommand SearchCommand { get; private set; } = null!;
        // 注意：RefreshCommand由基类NewBaseListViewModel提供，已修复async void问题
        public AsyncRelayCommand AddCommand { get; private set; } = null!;
        public AsyncRelayCommand<MedicalCaseDto> ViewDetailsCommand { get; private set; } = null!;
        public AsyncRelayCommand<MedicalCaseDto> EditCommand { get; private set; } = null!;
        public AsyncRelayCommand<MedicalCaseDto> ViewConsultationCommand { get; private set; } = null!;
        public AsyncRelayCommand<MedicalCaseDto> CreatePrescriptionCommand { get; private set; } = null!;
        public AsyncRelayCommand<MedicalCaseDto> PrintCommand { get; private set; } = null!;
        public AsyncRelayCommand<MedicalCaseDto> DeleteCommand { get; private set; } = null!;

        // 分页命令
        public DelegateCommand FirstPageCommand { get; private set; } = null!;
        public DelegateCommand PreviousPageCommand { get; private set; } = null!;
        public DelegateCommand NextPageCommand { get; private set; } = null!;
        public DelegateCommand LastPageCommand { get; private set; } = null!;

        #endregion

        #region Constructor

        public MedicalCaseManagementViewModel(
            IMedicalCaseService medicalCaseService,
            ICustomDialogService dialogService,
            ISessionManager sessionManager,
            INotificationService notificationService,
            ILogger<MedicalCaseManagementViewModel> logger)
            : base(sessionManager, notificationService, logger)
        {
            _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeData();
        }

        #endregion

        #region Methods

        protected override void InitializeCommands()
        {
            base.InitializeCommands();

            // ✅ 修复: 使用AsyncRelayCommand替代async void模式
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            // RefreshCommand由基类提供，已修复async void问题
            AddCommand = new AsyncRelayCommand(AddCaseAsync);
            ViewDetailsCommand = new AsyncRelayCommand<MedicalCaseDto>(ViewDetailsAsync);
            EditCommand = new AsyncRelayCommand<MedicalCaseDto>(EditCaseAsync);
            ViewConsultationCommand = new AsyncRelayCommand<MedicalCaseDto>(ViewConsultationAsync);
            CreatePrescriptionCommand = new AsyncRelayCommand<MedicalCaseDto>(CreatePrescriptionAsync);
            PrintCommand = new AsyncRelayCommand<MedicalCaseDto>(PrintCaseAsync);
            DeleteCommand = new AsyncRelayCommand<MedicalCaseDto>(DeleteCaseAsync);

            // 初始化分页命令
            FirstPageCommand = new DelegateCommand(async () => await PaginationCoordinator.GoToFirstPageAsync());
            PreviousPageCommand = new DelegateCommand(async () => await PaginationCoordinator.GoToPreviousPageAsync());
            NextPageCommand = new DelegateCommand(async () => await PaginationCoordinator.GoToNextPageAsync());
            LastPageCommand = new DelegateCommand(async () => await PaginationCoordinator.GoToLastPageAsync());
        }

        private void InitializeData()
        {
            // 设置默认的日期范围
            EndDate = DateTime.Today;
            StartDate = DateTime.Today.AddMonths(-1);
            FilterStatus = "全部状态";

            // 加载数据
            _ = Task.Run(async () => await RefreshDataAsync());
        }

        protected override async Task<ServiceResult<PagedResult<MedicalCaseDto>>> LoadDataAsync(PagedQueryBaseDto request)
        {
            try
            {
                _logger.LogInformation("加载医疗案例数据，页码: {CurrentPage}, 页大小: {PageSize}, 搜索关键词: {SearchKeyword}",
                    request.CurrentPage, request.PageSize, request.SearchKeyword);

                // UltraThink v1.0: 使用实际服务加载医疗案例数据
                var result = await _medicalCaseService.GetPagedAsync(request);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("医疗案例管理数据加载完成，共 {Count} 条记录", result.Data?.Items?.Count ?? 0);
                    return result;
                }
                else
                {
                    _logger.LogError("加载医疗案例数据失败: {ErrorMessage}", result.ErrorMessage);
                    return ServiceResult<PagedResult<MedicalCaseDto>>.Failure(
                        result.ErrorMessage ?? "加载数据失败",
                        result.Exception);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载医疗案例数据时发生异常");
                return ServiceResult<PagedResult<MedicalCaseDto>>.Failure("加载数据失败", ex);
            }
        }

        private async Task SearchAsync()
        {
            _logger.LogInformation("搜索医疗案例: 关键词={SearchKeyword}, 状态={FilterStatus}",
                SearchKeyword, FilterStatus);
            await RefreshDataAsync();
        }

        private async Task AddCaseAsync()
        {
            try
            {
                _logger.LogInformation("打开新建医疗案例对话框");

                var parameters = new Dictionary<string, object>();
                var result = await _dialogService.ShowDialogAsync("CreateMedicalCaseDialog", parameters);

                if (result.Result == true)
                {
                    _logger.LogInformation("医疗案例创建成功，刷新数据列表");
                    await RefreshDataAsync();
                    await _dialogService.ShowSuccessAsync("医疗案例创建成功", "成功");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建医疗案例时发生错误");
                await _dialogService.ShowErrorAsync($"创建医疗案例失败: {ex.Message}", "错误");
            }
        }

        private async Task ViewDetailsAsync(MedicalCaseDto medicalCase)
        {
            if (medicalCase == null)
            {
                return;
            }

            try
            {
                _logger.LogInformation("查看医疗案例详情: {CaseId}", medicalCase.Id);

                var result = await _medicalCaseService.GetByIdAsync(medicalCase.Id);
                if (result.IsSuccess && result.Data != null)
                {
                    var detailInfo = $"案例ID: {result.Data.Id}\n" +
                                   $"患者姓名: {result.Data.PatientName}\n" +
                                   $"医生: {result.Data.DoctorName}\n" +
                                   $"创建时间: {result.Data.CreateTime:yyyy-MM-dd HH:mm}\n" +
                                   $"状态: {result.Data.Status}\n" +
                                   $"诊断结果: {result.Data.DiagnosisResult ?? "暂无"}\n" +
                                   $"备注: {result.Data.Remark ?? "暂无"}";

                    await _dialogService.ShowInformationAsync(detailInfo, $"医疗案例详情 - {result.Data.PatientName}");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(
                        result.ErrorMessage ?? "获取医疗案例详情失败",
                        "错误");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查看医疗案例详情时发生错误");
                await _dialogService.ShowErrorAsync($"查看详情失败: {ex.Message}", "错误");
            }
        }

        private async Task EditCaseAsync(MedicalCaseDto medicalCase)
        {
            if (medicalCase == null)
            {
                return;
            }

            _logger.LogInformation("编辑医疗案例: {CaseId}", medicalCase.Id);
            // TODO: 实现编辑逻辑
            await Task.CompletedTask;
        }

        private async Task ViewConsultationAsync(MedicalCaseDto medicalCase)
        {
            if (medicalCase == null)
            {
                return;
            }

            _logger.LogInformation("查看看诊记录: {CaseId}", medicalCase.Id);
            // TODO: 实现查看看诊记录逻辑
            await Task.CompletedTask;
        }

        private async Task CreatePrescriptionAsync(MedicalCaseDto medicalCase)
        {
            if (medicalCase == null)
            {
                return;
            }

            try
            {
                _logger.LogInformation("从医案 {CaseId} 开具处方，患者: {PatientName}", medicalCase.Id, medicalCase.PatientName);

                // 创建处方编辑对话框参数，传递医案和患者信息
                var parameters = new Dictionary<string, object>
                {
                    ["IsEditMode"] = false,
                    ["MedicalCaseId"] = medicalCase.Id,
                    ["PatientId"] = medicalCase.PatientId,
                    ["PatientName"] = medicalCase.PatientName,
                    ["ContextMode"] = "MedicalCase"
                };

                var result = await _dialogService.ShowDialogAsync("PrescriptionEditorDialog", parameters);

                if (result.Result == true)
                {
                    _logger.LogInformation("处方创建成功，医案: {CaseId}", medicalCase.Id);
                    await _dialogService.ShowSuccessAsync(
                        $"为患者 {medicalCase.PatientName} 开具的处方已创建成功",
                        "处方创建完成");

                    // 可选：刷新医案状态或记录
                    // await RefreshDataAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从医案开具处方时发生错误: {CaseId}", medicalCase.Id);
                await _dialogService.ShowErrorAsync($"开具处方失败: {ex.Message}", "错误");
            }
        }

        private async Task PrintCaseAsync(MedicalCaseDto medicalCase)
        {
            if (medicalCase == null)
            {
                return;
            }

            _logger.LogInformation("打印医疗案例: {CaseId}", medicalCase.Id);
            // TODO: 实现打印逻辑
            await Task.CompletedTask;
        }

        private async Task DeleteCaseAsync(MedicalCaseDto medicalCase)
        {
            if (medicalCase == null)
            {
                return;
            }

            try
            {
                _logger.LogInformation("删除医疗案例: {CaseId}", medicalCase.Id);

                var confirm = await _dialogService.ShowConfirmationAsync(
                    $"确定要删除医疗案例吗？\n" +
                    $"患者: {medicalCase.PatientName}\n" +
                    $"创建时间: {medicalCase.CreateTime:yyyy-MM-dd HH:mm}\n" +
                    $"此操作不可恢复。",
                    "确认删除");

                if (confirm)
                {
                    var result = await _medicalCaseService.DeleteAsync(medicalCase.Id);
                    if (result.IsSuccess)
                    {
                        _logger.LogInformation("医疗案例删除成功: {CaseId}", medicalCase.Id);
                        await RefreshDataAsync();
                        await _dialogService.ShowInformationAsync("医疗案例删除成功", "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                            result.ErrorMessage ?? "删除失败",
                            "错误");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除医疗案例时发生错误");
                await _dialogService.ShowErrorAsync($"删除失败: {ex.Message}", "错误");
            }
        }

        #endregion
    }
}
