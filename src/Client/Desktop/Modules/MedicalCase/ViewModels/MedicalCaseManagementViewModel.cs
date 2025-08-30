using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Managers;
using LYBT.Desktop.Core.Coordinators;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using Microsoft.Extensions.Logging;
using Prism.Commands;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 医疗案例管理视图模型 - UltraThink统一管理模块设计
    /// 用于展示和管理所有的医疗案例记录
    /// </summary>
    public class MedicalCaseManagementViewModel : NewBaseListViewModel<MedicalCaseDto>
    {
        #region Fields

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

        public DelegateCommand SearchCommand { get; private set; }
        // 注意：RefreshCommand由基类NewBaseListViewModel提供，不需要重复定义
        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand<MedicalCaseDto> ViewDetailsCommand { get; private set; }
        public DelegateCommand<MedicalCaseDto> EditCommand { get; private set; }
        public DelegateCommand<MedicalCaseDto> ViewConsultationCommand { get; private set; }
        public DelegateCommand<MedicalCaseDto> PrintCommand { get; private set; }
        public DelegateCommand<MedicalCaseDto> DeleteCommand { get; private set; }

        // 分页命令
        public DelegateCommand FirstPageCommand { get; private set; }
        public DelegateCommand PreviousPageCommand { get; private set; }
        public DelegateCommand NextPageCommand { get; private set; }
        public DelegateCommand LastPageCommand { get; private set; }

        #endregion

        #region Constructor

        public MedicalCaseManagementViewModel(
            ISessionManager sessionManager,
            INotificationService notificationService,
            ILogger<MedicalCaseManagementViewModel> logger)
            : base(sessionManager, notificationService, logger)
        {
            _logger = logger;
            
            InitializeData();
        }

        #endregion

        #region Methods

        protected override void InitializeCommands()
        {
            base.InitializeCommands();
            
            SearchCommand = new DelegateCommand(async () => await SearchAsync());
            // RefreshCommand由基类提供，已修复async void问题
            AddCommand = new DelegateCommand(async () => await AddCaseAsync());
            ViewDetailsCommand = new DelegateCommand<MedicalCaseDto>(async dto => await ViewDetailsAsync(dto));
            EditCommand = new DelegateCommand<MedicalCaseDto>(async dto => await EditCaseAsync(dto));
            ViewConsultationCommand = new DelegateCommand<MedicalCaseDto>(async dto => await ViewConsultationAsync(dto));
            PrintCommand = new DelegateCommand<MedicalCaseDto>(async dto => await PrintCaseAsync(dto));
            DeleteCommand = new DelegateCommand<MedicalCaseDto>(async dto => await DeleteCaseAsync(dto));
            
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

                // 模拟数据加载
                await Task.Delay(500);
                
                // TODO: 从实际服务加载医疗案例数据
                var items = new List<MedicalCaseDto>();
                
                var pagedResult = new PagedResult<MedicalCaseDto>(items, items.Count, request.CurrentPage, request.PageSize);

                _logger.LogInformation("医疗案例管理数据加载完成，共 {Count} 条记录", items.Count);
                return ServiceResult<PagedResult<MedicalCaseDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载医疗案例数据失败");
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
            _logger.LogInformation("新建医疗案例");
            // TODO: 实现新建医疗案例逻辑
            await Task.CompletedTask;
        }

        private async Task ViewDetailsAsync(MedicalCaseDto medicalCase)
        {
            if (medicalCase == null) return;
            
            _logger.LogInformation("查看医疗案例详情: {CaseId}", medicalCase.Id);
            // TODO: 实现查看详情逻辑
            await Task.CompletedTask;
        }

        private async Task EditCaseAsync(MedicalCaseDto medicalCase)
        {
            if (medicalCase == null) return;
            
            _logger.LogInformation("编辑医疗案例: {CaseId}", medicalCase.Id);
            // TODO: 实现编辑逻辑
            await Task.CompletedTask;
        }

        private async Task ViewConsultationAsync(MedicalCaseDto medicalCase)
        {
            if (medicalCase == null) return;
            
            _logger.LogInformation("查看看诊记录: {CaseId}", medicalCase.Id);
            // TODO: 实现查看看诊记录逻辑
            await Task.CompletedTask;
        }

        private async Task PrintCaseAsync(MedicalCaseDto medicalCase)
        {
            if (medicalCase == null) return;
            
            _logger.LogInformation("打印医疗案例: {CaseId}", medicalCase.Id);
            // TODO: 实现打印逻辑
            await Task.CompletedTask;
        }

        private async Task DeleteCaseAsync(MedicalCaseDto medicalCase)
        {
            if (medicalCase == null) return;
            
            _logger.LogInformation("删除医疗案例: {CaseId}", medicalCase.Id);
            // TODO: 实现删除确认和删除逻辑
            await Task.CompletedTask;
        }

        #endregion
    }
}