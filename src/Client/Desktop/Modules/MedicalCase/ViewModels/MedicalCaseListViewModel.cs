using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using LYBT.Desktop.Core.Models.MedicalCase;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.MedicalCase.Services.Interfaces;
using AutoMapper;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Common;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;
using LYBT.Desktop.Core.Interfaces.Services;
using Prism.Mvvm;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 医疗案例列表视图模型 - UltraThink架构重构版
    /// UltraThink模块化架构：使用IMedicalCaseModuleService，实现模块自包含
    /// </summary>
    public class MedicalCaseListViewModel : BindableBase
    {
        private readonly IMedicalCaseModuleService _medicalCaseModuleService;
        private readonly ICustomDialogService _dialogService;
        private readonly IDialogService _prismDialogService;
        private readonly IRegionManager _regionManager;
        private readonly IMapper _mapper;

        #region Properties

        private ObservableCollection<MedicalCaseInfo> _medicalCases = new();
        public ObservableCollection<MedicalCaseInfo> MedicalCases
        {
            get => _medicalCases;
            set => SetProperty(ref _medicalCases, value);
        }

        private MedicalCaseInfo? _selectedMedicalCase;
        public MedicalCaseInfo? SelectedMedicalCase
        {
            get => _selectedMedicalCase;
            set => SetProperty(ref _selectedMedicalCase, value);
        }

        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        private MedicalCaseStatus? _filterStatus;
        public MedicalCaseStatus? FilterStatus
        {
            get => _filterStatus;
            set => SetProperty(ref _filterStatus, value);
        }

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        private int _pageSize = 20;
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        private int _totalPages;
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }
        
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Commands

        public DelegateCommand LoadDataCommand { get; }
        public DelegateCommand SearchCommand { get; }
        public DelegateCommand AddCommand { get; }
        public new DelegateCommand RefreshCommand { get; }
        public DelegateCommand<MedicalCaseInfo> ViewDetailCommand { get; }
        public DelegateCommand<MedicalCaseInfo> StartConsultationCommand { get; }
        public DelegateCommand<MedicalCaseInfo> EditCommand { get; }
        public DelegateCommand<MedicalCaseInfo> DeleteCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }

        #endregion

        public MedicalCaseListViewModel(
            IMedicalCaseModuleService medicalCaseModuleService,
            ICustomDialogService dialogService,
            IDialogService prismDialogService,
            IRegionManager regionManager,
            IMapper mapper)
        {
            _medicalCaseModuleService = medicalCaseModuleService ?? throw new ArgumentNullException(nameof(medicalCaseModuleService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _prismDialogService = prismDialogService ?? throw new ArgumentNullException(nameof(prismDialogService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            MedicalCases = new ObservableCollection<MedicalCaseInfo>();

            // Initialize Commands
            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
            SearchCommand = new DelegateCommand(async () => await SearchAsync());
            AddCommand = new DelegateCommand(async () => await AddMedicalCaseAsync());
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            ViewDetailCommand = new DelegateCommand<MedicalCaseInfo>(async (item) => await ViewDetailAsync(item));
            StartConsultationCommand = new DelegateCommand<MedicalCaseInfo>(async (item) => await StartConsultationAsync(item));
            EditCommand = new DelegateCommand<MedicalCaseInfo>(async (item) => await EditAsync(item));
            DeleteCommand = new DelegateCommand<MedicalCaseInfo>(async (item) => await DeleteAsync(item));
            PreviousPageCommand = new DelegateCommand(async () => await PreviousPageAsync(), () => CurrentPage > 1);
            NextPageCommand = new DelegateCommand(async () => await NextPageAsync(), () => CurrentPage < TotalPages);

            // Load initial data
            LoadDataCommand.Execute();
        }

        #region Private Methods

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                // UltraThink四层架构：使用模块化服务获取分页数据
                var query = new PagedQueryBaseDto
                {
                    PageIndex = CurrentPage,
                    PageSize = PageSize,
                    Keyword = SearchKeyword
                };

                var result = await _medicalCaseModuleService.GetPagedAsync(query);

                if (!result.IsSuccess)
                {
                    await _dialogService.ShowErrorAsync($"加载数据失败: {result.ErrorMessage}", "错误");
                    return;
                }

                if (result.Data != null)
                {
                    // 应用状态筛选（前端过滤）
                    var allItems = result.Data.Items;
                    if (FilterStatus.HasValue)
                    {
                        allItems = allItems.Where(item => item.Status == FilterStatus.Value).ToList();
                    }
                    
                    TotalCount = result.Data.TotalCount;
                    TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

                    MedicalCases.Clear();
                    foreach (var item in allItems)
                    {
                        MedicalCases.Add(item);
                    }
                }

                // Update command states
                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"加载数据失败: {ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchAsync()
        {
            CurrentPage = 1;
            await LoadDataAsync();
        }

        private async Task RefreshAsync()
        {
            SearchKeyword = string.Empty;
            FilterStatus = null;
            CurrentPage = 1;
            await LoadDataAsync();
        }

        private async Task AddMedicalCaseAsync()
        {
            try
            {
                var dialogParameters = new DialogParameters();
                
                _prismDialogService.ShowDialog("CreateMedicalCaseDialog", dialogParameters, result =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        // 刷新列表
                        LoadDataCommand.Execute();
                    }
                });
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"操作失败: {ex.Message}", "错误");
            }
        }

        private async Task ViewDetailAsync(MedicalCaseInfo item)
        {
            if (item == null) return;

            try
            {
                // 导航到详情界面 - 使用字符串参数方式
                _regionManager.RequestNavigate("MainContentRegion", $"MedicalCaseDetailView?MedicalCaseId={item.Id}&ViewMode=Detail");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"查看详情失败: {ex.Message}", "错误");
            }
        }

        private async Task StartConsultationAsync(MedicalCaseInfo item)
        {
            if (item == null) return;

            try
            {
                // 使用模块化服务开始看诊
                var updateResult = await _medicalCaseModuleService.StartConsultationAsync(item.Id);
                
                if (updateResult.IsSuccess)
                {
                    // 导航到看诊界面 - 使用字符串参数方式
                    _regionManager.RequestNavigate("MainContentRegion", $"ConsultationMainView?MedicalCaseId={item.Id}&PatientId={item.PatientId}&ConsultationMode=Start");
                    
                    // 刷新列表显示最新状态
                    await LoadDataAsync();
                }
                else
                {
                    await _dialogService.ShowErrorAsync(updateResult.ErrorMessage ?? "无法开始看诊", "错误");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"启动看诊失败: {ex.Message}", "错误");
            }
        }

        private async Task EditAsync(MedicalCaseInfo item)
        {
            if (item == null) return;

            try
            {
                var dialogParameters = new DialogParameters()
                {
                    { "MedicalCaseId", item.Id },
                    { "EditMode", true }
                };
                
                _prismDialogService.ShowDialog("CreateMedicalCaseDialog", dialogParameters, result =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        LoadDataCommand.Execute();
                    }
                });
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"编辑失败: {ex.Message}", "错误");
            }
        }

        private async Task DeleteAsync(MedicalCaseInfo item)
        {
            if (item == null) return;

            try
            {
                // 首先检查是否可以删除
                var canDeleteResult = await _medicalCaseModuleService.CanDeleteAsync(item.Id);
                if (!canDeleteResult.IsSuccess || !canDeleteResult.Data)
                {
                    await _dialogService.ShowErrorAsync(
                        canDeleteResult.ErrorMessage ?? "当前医疗案例状态不允许删除", 
                        "无法删除");
                    return;
                }

                var confirmed = await _dialogService.ShowConfirmationAsync(
                    $"确定要删除患者 '{item.PatientName}' 的医疗案例吗？\n此操作不可恢复。", 
                    "确认删除");

                if (!confirmed) return;

                IsLoading = true;
                var result = await _medicalCaseModuleService.DeleteAsync(item.Id);
                
                if (result.IsSuccess)
                {
                    await _dialogService.ShowSuccessAsync("删除成功!", "操作完成");
                    await LoadDataAsync();
                }
                else
                {
                    await _dialogService.ShowErrorAsync(result.ErrorMessage ?? "删除失败", "错误");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"删除失败: {ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadDataAsync();
            }
        }

        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadDataAsync();
            }
        }

        #endregion
    }
}