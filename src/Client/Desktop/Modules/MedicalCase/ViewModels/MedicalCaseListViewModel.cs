using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Modules.MedicalCase.ViewModels
{
    /// <summary>
    /// 病历列表视图模型 - UltraThink精简架构
    /// 基于UnifiedViewModelBase实现病历列表管理功能
    /// </summary>
    public class MedicalCaseListViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        private readonly IMedicalCaseService _medicalCaseService;

        #endregion

        #region 数据属性

        private ObservableCollection<MedicalCaseDto> _medicalCases = new();
        private MedicalCaseDto? _selectedMedicalCase;
        private string _searchText = string.Empty;
        private int _totalCount;
        private int _currentPage = 1;
        private int _pageSize = 20;

        /// <summary>
        /// 病历列表
        /// </summary>
        public ObservableCollection<MedicalCaseDto> MedicalCases
        {
            get => _medicalCases;
            set => SetProperty(ref _medicalCases, value);
        }

        /// <summary>
        /// 选中的病历
        /// </summary>
        public MedicalCaseDto? SelectedMedicalCase
        {
            get => _selectedMedicalCase;
            set
            {
                if (SetProperty(ref _selectedMedicalCase, value))
                {
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>
        /// 搜索关键字
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        #endregion

        #region 命令

        /// <summary>
        /// 加载数据命令
        /// </summary>
        public DelegateCommand LoadDataCommand { get; }

        /// <summary>
        /// 搜索命令
        /// </summary>
        public DelegateCommand SearchCommand { get; }

        /// <summary>
        /// 创建病历命令
        /// </summary>
        public DelegateCommand CreateCommand { get; }

        /// <summary>
        /// 编辑病历命令
        /// </summary>
        public DelegateCommand EditCommand { get; }

        /// <summary>
        /// 删除病历命令
        /// </summary>
        public DelegateCommand DeleteCommand { get; }

        /// <summary>
        /// 查看详情命令
        /// </summary>
        public DelegateCommand ViewDetailCommand { get; }

        /// <summary>
        /// 上一页命令
        /// </summary>
        public DelegateCommand PreviousPageCommand { get; }

        /// <summary>
        /// 下一页命令
        /// </summary>
        public DelegateCommand NextPageCommand { get; }

        #endregion

        #region 构造函数

        public MedicalCaseListViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IMedicalCaseService medicalCaseService,
            ISessionManager? sessionManager = null,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, errorHandlingService)
        {
            _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));

            // 初始化命令
            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
            SearchCommand = new DelegateCommand(async () => await SearchAsync());
            CreateCommand = new DelegateCommand(Create);
            EditCommand = new DelegateCommand(Edit, CanEdit);
            DeleteCommand = new DelegateCommand(async () => await DeleteAsync(), CanDelete);
            ViewDetailCommand = new DelegateCommand(ViewDetail, CanViewDetail);
            PreviousPageCommand = new DelegateCommand(async () => await PreviousPageAsync(), CanPreviousPage);
            NextPageCommand = new DelegateCommand(async () => await NextPageAsync(), CanNextPage);

            // 属性变更时刷新命令状态
            PropertyChanged += (s, e) => UpdateCommandStates();
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 页面加载时调用
        /// </summary>
        protected override async Task OnNavigatedToAsync(NavigationContext navigationContext)
        {
            await base.OnNavigatedToAsync(navigationContext);
            await LoadDataAsync();
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 加载数据
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                SetIsBusy(true, "正在加载病历列表...");

                var result = await _medicalCaseService.GetPagedAsync(CurrentPage, PageSize, SearchText);
                if (result.IsSuccess && result.Data != null)
                {
                    MedicalCases.Clear();
                    foreach (var item in result.Data.Items)
                    {
                        MedicalCases.Add(item);
                    }
                    TotalCount = result.Data.TotalCount;
                }
                else
                {
                    await ShowErrorMessageAsync($"加载病历列表失败: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载病历列表时发生异常");
                await ShowErrorMessageAsync("加载病历列表时发生系统错误，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 搜索
        /// </summary>
        private async Task SearchAsync()
        {
            CurrentPage = 1;
            await LoadDataAsync();
        }

        /// <summary>
        /// 创建病历
        /// </summary>
        private void Create()
        {
            NavigateTo("MainRegion", "CreateMedicalCaseView");
        }

        /// <summary>
        /// 编辑病历
        /// </summary>
        private void Edit()
        {
            if (SelectedMedicalCase != null)
            {
                var parameters = new NavigationParameters
                {
                    { "MedicalCaseId", SelectedMedicalCase.Id }
                };
                NavigateTo("MainRegion", "MedicalCaseDetailView", parameters);
            }
        }

        /// <summary>
        /// 删除病历
        /// </summary>
        private async Task DeleteAsync()
        {
            if (SelectedMedicalCase == null) return;

            var confirmed = await ShowConfirmMessageAsync($"确定要删除病历 '{SelectedMedicalCase.CaseNumber}' 吗？");
            if (!confirmed) return;

            try
            {
                SetIsBusy(true, "正在删除病历...");

                var result = await _medicalCaseService.DeleteAsync(SelectedMedicalCase.Id);
                if (result.IsSuccess)
                {
                    await ShowSuccessMessageAsync("病历删除成功");
                    await LoadDataAsync();
                }
                else
                {
                    await ShowErrorMessageAsync($"删除病历失败: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "删除病历时发生异常");
                await ShowErrorMessageAsync("删除病历时发生系统错误，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 查看详情
        /// </summary>
        private void ViewDetail()
        {
            if (SelectedMedicalCase != null)
            {
                var parameters = new NavigationParameters
                {
                    { "MedicalCaseId", SelectedMedicalCase.Id },
                    { "IsReadOnly", true }
                };
                NavigateTo("MainRegion", "MedicalCaseDetailView", parameters);
            }
        }

        /// <summary>
        /// 上一页
        /// </summary>
        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadDataAsync();
            }
        }

        /// <summary>
        /// 下一页
        /// </summary>
        private async Task NextPageAsync()
        {
            var totalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            if (CurrentPage < totalPages)
            {
                CurrentPage++;
                await LoadDataAsync();
            }
        }

        #endregion

        #region 命令状态检查

        private bool CanEdit() => SelectedMedicalCase != null && !IsBusy;
        private bool CanDelete() => SelectedMedicalCase != null && !IsBusy;
        private bool CanViewDetail() => SelectedMedicalCase != null;
        private bool CanPreviousPage() => CurrentPage > 1 && !IsBusy;
        private bool CanNextPage()
        {
            var totalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            return CurrentPage < totalPages && !IsBusy;
        }

        private void UpdateCommandStates()
        {
            EditCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            ViewDetailCommand.RaiseCanExecuteChanged();
            PreviousPageCommand.RaiseCanExecuteChanged();
            NextPageCommand.RaiseCanExecuteChanged();
        }

        #endregion
    }
}