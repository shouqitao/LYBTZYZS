using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Models.MedicalCase;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Prism.Commands;
using Prism.Events;
using Prism.Dialogs;
using LYBT.Desktop.Core.Extensions;
using Prism.Navigation.Regions;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 医疗案例列表视图模型 - 完整版
    /// </summary>
    public class MedicalCaseListViewModel : ServiceViewModel
    {
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly IDialogService _dialogService;
        private readonly IDialogService _prismDialogService;
        private readonly IRegionManager _regionManager;

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
            IMedicalCaseService medicalCaseService,
            IDialogService dialogService,
            IDialogService prismDialogService,
            IRegionManager regionManager,
            IEventAggregator eventAggregator)
            : base(eventAggregator)
        {
            _medicalCaseService = medicalCaseService;
            _dialogService = dialogService;
            _prismDialogService = prismDialogService;
            _regionManager = regionManager;

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

                var result = await _medicalCaseService.GetPagedAsync(CurrentPage, PageSize);

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    await _dialogService.ShowErrorAsync($"加载数据失败: {result.ErrorMessage}", "错误");
                    return;
                }

                TotalCount = result.TotalCount;
                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

                MedicalCases.Clear();
                foreach (var item in result.Items)
                {
                    // 应用搜索和筛选
                    if (!string.IsNullOrEmpty(SearchKeyword) && 
                        !item.PatientName.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase) &&
                        !item.DoctorName.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (FilterStatus.HasValue && item.Status != FilterStatus.Value)
                    {
                        continue;
                    }

                    MedicalCases.Add(item);
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
                // 更新状态为看诊中
                var updateResult = await _medicalCaseService.UpdateStatusAsync(item.Id, MedicalCaseStatus.InConsultation);
                
                if (updateResult.IsSuccess)
                {
                    // 导航到看诊界面 - 使用字符串参数方式
                    _regionManager.RequestNavigate("MainContentRegion", $"ConsultationMainView?MedicalCaseId={item.Id}&PatientId={item.PatientId}&ConsultationMode=Start");
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
                var confirmed = await _dialogService.ShowConfirmationAsync(
                    $"确定要删除患者 '{item.PatientName}' 的医疗案例吗？\n此操作不可恢复。", 
                    "确认删除");

                if (!confirmed) return;

                IsLoading = true;
                var result = await _medicalCaseService.DeleteAsync(item.Id);
                
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