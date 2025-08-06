using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Registration;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace LYBT.WPF.Client.Modules.Registration.ViewModels
{
    /// <summary>
    /// 挂号管理视图模型
    /// </summary>
    public class RegistrationMainViewModel : BindableBase
    {
        private readonly ICommonDialogService _commonDialogService;

        private readonly IRegistrationService _registrationService;
        private string _searchKeyword = string.Empty;
        private string _searchType = "all";
        private RegistrationInfo _selectedRegistration;
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalCount = 0;
        private bool _isLoading = false;

        public RegistrationMainViewModel(IRegistrationService registrationService,
            ICommonDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            _registrationService = registrationService;
            
            Registrations = new ObservableCollection<RegistrationInfo>();
            RegistrationsView = CollectionViewSource.GetDefaultView(Registrations);

            InitializeCommands();
            LoadRegistrations();
        }

        #region Properties

        public ObservableCollection<RegistrationInfo> Registrations { get; }
        public ICollectionView RegistrationsView { get; }

        /// <summary>搜索关键词</summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        /// <summary>搜索类型</summary>
        public string SearchType
        {
            get => _searchType;
            set => SetProperty(ref _searchType, value);
        }

        /// <summary>选中的挂号</summary>
        public RegistrationInfo SelectedRegistration
        {
            get => _selectedRegistration;
            set => SetProperty(ref _selectedRegistration, value);
        }

        /// <summary>当前页码</summary>
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    RaisePropertyChanged(nameof(StatusText));
                    RaisePropertyChanged(nameof(CanGoFirstPage));
                    RaisePropertyChanged(nameof(CanGoPreviousPage));
                    RaisePropertyChanged(nameof(CanGoNextPage));
                    RaisePropertyChanged(nameof(CanGoLastPage));
                    
                    // 更新命令状态
                    FirstPageCommand?.RaiseCanExecuteChanged();
                    PreviousPageCommand?.RaiseCanExecuteChanged();
                    NextPageCommand?.RaiseCanExecuteChanged();
                    LastPageCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>页大小</summary>
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        /// <summary>总记录数</summary>
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (SetProperty(ref _totalCount, value))
                {
                    RaisePropertyChanged(nameof(StatusText));
                    RaisePropertyChanged(nameof(TotalPages));
                    RaisePropertyChanged(nameof(CanGoFirstPage));
                    RaisePropertyChanged(nameof(CanGoPreviousPage));
                    RaisePropertyChanged(nameof(CanGoNextPage));
                    RaisePropertyChanged(nameof(CanGoLastPage));
                    
                    // 更新命令状态
                    FirstPageCommand?.RaiseCanExecuteChanged();
                    PreviousPageCommand?.RaiseCanExecuteChanged();
                    NextPageCommand?.RaiseCanExecuteChanged();
                    LastPageCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>状态文本</summary>
        public string StatusText => $"共 {TotalCount} 条挂号记录，第 {CurrentPage} 页，共 {TotalPages} 页";

        /// <summary>总页数</summary>
        public int TotalPages => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

        /// <summary>是否可以跳转到第一页</summary>
        public bool CanGoFirstPage => CurrentPage > 1;

        /// <summary>是否可以跳转到上一页</summary>
        public bool CanGoPreviousPage => CurrentPage > 1;

        /// <summary>是否可以跳转到下一页</summary>
        public bool CanGoNextPage => CurrentPage < TotalPages;

        /// <summary>是否可以跳转到最后一页</summary>
        public bool CanGoLastPage => CurrentPage < TotalPages;

        #endregion

        #region Commands

        public DelegateCommand SearchCommand { get; private set; }
        public DelegateCommand RefreshCommand { get; private set; }
        public DelegateCommand AddRegistrationCommand { get; private set; }
        public DelegateCommand<RegistrationInfo> EditRegistrationCommand { get; private set; }
        public DelegateCommand<RegistrationInfo> DeleteRegistrationCommand { get; private set; }
        public DelegateCommand<RegistrationInfo> CancelRegistrationCommand { get; private set; }
        public DelegateCommand<RegistrationInfo> CheckInCommand { get; private set; }
        public DelegateCommand FirstPageCommand { get; private set; }
        public DelegateCommand PreviousPageCommand { get; private set; }
        public DelegateCommand NextPageCommand { get; private set; }
        public DelegateCommand LastPageCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            SearchCommand = new DelegateCommand(ExecuteSearch);
            RefreshCommand = new DelegateCommand(ExecuteRefresh);
            AddRegistrationCommand = new DelegateCommand(ExecuteAddRegistration);
            EditRegistrationCommand = new DelegateCommand<RegistrationInfo>(ExecuteEditRegistration);
            DeleteRegistrationCommand = new DelegateCommand<RegistrationInfo>(ExecuteDeleteRegistration);
            CancelRegistrationCommand = new DelegateCommand<RegistrationInfo>(ExecuteCancelRegistration);
            CheckInCommand = new DelegateCommand<RegistrationInfo>(ExecuteCheckIn);
            FirstPageCommand = new DelegateCommand(ExecuteFirstPage, CanExecuteFirstPage);
            PreviousPageCommand = new DelegateCommand(ExecutePreviousPage, CanExecutePreviousPage);
            NextPageCommand = new DelegateCommand(ExecuteNextPage, CanExecuteNextPage);
            LastPageCommand = new DelegateCommand(ExecuteLastPage, CanExecuteLastPage);
        }

        #endregion

        #region Command Handlers

        private void ExecuteSearch()
        {
            System.Diagnostics.Debug.WriteLine($"执行搜索，关键词: '{SearchKeyword}', 类型: '{SearchType}'");
            CurrentPage = 1; // 搜索时重置到第一页
            LoadRegistrations();
        }

        private void ExecuteRefresh()
        {
            LoadRegistrations();
        }

        private void ExecuteAddRegistration()
        {
            try
            {
                // TODO: 需要通过依赖注入获取服务实例来创建ViewModel
                // 暂时显示提示信息，避免运行时错误
                _commonDialogService.ShowInformationAsync("新增挂号功能需要配置依赖注入服务后才能使用。\n请在SystemManagement模块中使用该功能。", "提示").GetAwaiter().GetResult();
                
                // 正确的实现方式应该是：
                // 1. 通过构造函数注入IContainerProvider或IServiceProvider
                // 2. 使用容器解析ViewModel及其依赖
                // 3. 创建对话框并设置DataContext
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开新增挂号对话框失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private void ExecuteEditRegistration(RegistrationInfo registration)
        {
            if (registration == null) return;
            
            try
            {
                // 使用SystemManagement模块的View对话框，直接传递挂号ID
                var dialog = new LYBT.WPF.Client.Modules.SystemManagement.Registrations.Views.ViewRegistrationDialog();
                dialog.Owner = Application.Current.MainWindow;
                
                // 设置要查看的挂号数据
                if (dialog.DataContext is LYBT.WPF.Client.Modules.SystemManagement.Registrations.ViewModels.ViewRegistrationDialogViewModel viewModel)
                {
                    // 设置挂号ID进行加载
                    viewModel.SetRegistrationId(registration.Id);
                }

                dialog.ShowDialog(); // 查看对话框不需要返回结果
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开挂号详情对话框失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private async void ExecuteDeleteRegistration(RegistrationInfo registration)
        {
            if (registration == null) return;
            
            var result = await _commonDialogService.ShowConfirmationAsync($"确定要删除挂号 '{registration.RegistrationNumber}' 吗？", "确认删除");
            if (result )
            {
                try
                {
                    var response = await _registrationService.DeleteRegistrationAsync(registration.Id);
                    if (response.IsSuccess)
                    {
                        _commonDialogService.ShowInformationAsync("挂号删除成功", "成功").GetAwaiter().GetResult();
                        LoadRegistrations(); // 刷新列表
                    }
                    else
                    {
                        _commonDialogService.ShowErrorAsync($"删除挂号失败: {response.ErrorMessage}", "错误").GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    _commonDialogService.ShowErrorAsync($"删除挂号失败: {ex.Message}", "错误").GetAwaiter().GetResult();
                }
            }
        }

        private async void ExecuteCancelRegistration(RegistrationInfo registration)
        {
            if (registration == null) return;
            
            var result = await _commonDialogService.ShowConfirmationAsync($"确定要取消挂号 '{registration.RegistrationNumber}' 吗？", "确认取消");
            if (result )
            {
                try
                {
                    var response = await _registrationService.CancelRegistrationAsync(registration.Id);
                    if (response.IsSuccess)
                    {
                        _commonDialogService.ShowInformationAsync("挂号取消成功", "成功").GetAwaiter().GetResult();
                        LoadRegistrations(); // 刷新列表
                    }
                    else
                    {
                        _commonDialogService.ShowErrorAsync($"取消挂号失败: {response.ErrorMessage}", "错误").GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    _commonDialogService.ShowErrorAsync($"取消挂号失败: {ex.Message}", "错误").GetAwaiter().GetResult();
                }
            }
        }

        private async void ExecuteCheckIn(RegistrationInfo registration)
        {
            if (registration == null) return;
            
            // 检查当前状态是否可以签到
            if (registration.Status != LYBT.Shared.Models.Enums.RegistrationStatus.Scheduled)
            {
                await _commonDialogService.ShowWarningAsync($"患者 '{registration.PatientName}' 当前状态为 '{registration.StatusText}'，无法签到", "无法签到");
                return;
            }
            
            // 确认签到操作
            var result = await _commonDialogService.ShowConfirmationAsync($"确认患者 '{registration.PatientName}' 签到？\n将状态改为'已到达'", "确认签到");
            if (result != MessageBoxResult.Yes) return;
            
            try
            {
                // 创建更新DTO，将状态改为已到达
                var updateDto = new LYBT.Shared.Models.Contracts.Registration.RegistrationEditDto
                {
                    Id = registration.Id,
                    PatientId = registration.PatientId,
                    DoctorId = registration.DoctorId,
                    /* Department = /* registration.Department */ "", */
                    RegistrationType = registration.RegistrationType,
                    VisitDate = registration.VisitDate,
                    TimeSlot = registration.TimeSlot,
                    Fee = registration.Fee,
                    Status = LYBT.Shared.Models.Enums.RegistrationStatus.Arrived, // 设置为已到达状态
                    Remark = registration.Remark
                };

                // 调用更新API
                var response = await _registrationService.UpdateRegistrationAsync(updateDto);
                if (response.IsSuccess)
                {
                    _commonDialogService.ShowInformationAsync($"患者 '{registration.PatientName}' 签到成功", "成功").GetAwaiter().GetResult();
                    LoadRegistrations(); // 刷新列表
                }
                else
                {
                    _commonDialogService.ShowErrorAsync($"签到失败: {response.ErrorMessage}", "错误").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"签到失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private void ExecuteFirstPage()
        {
            CurrentPage = 1;
            LoadRegistrations();
        }

        private bool CanExecuteFirstPage()
        {
            return CurrentPage > 1;
        }

        private void ExecutePreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadRegistrations();
            }
        }

        private bool CanExecutePreviousPage()
        {
            return CurrentPage > 1;
        }

        private void ExecuteNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                LoadRegistrations();
            }
        }

        private bool CanExecuteNextPage()
        {
            return CurrentPage < TotalPages;
        }

        private void ExecuteLastPage()
        {
            CurrentPage = TotalPages;
            LoadRegistrations();
        }

        private bool CanExecuteLastPage()
        {
            return CurrentPage < TotalPages;
        }

        #endregion

        #region Private Methods

        private async void LoadRegistrations()
        {
            IsLoading = true;
            try
            {
                System.Diagnostics.Debug.WriteLine($"开始加载挂号列表，搜索关键词: '{SearchKeyword}', 搜索类型: '{SearchType}', 页码: {CurrentPage}");
                
                var request = new RegistrationPagedQueryDto
                {
                    CurrentPage = CurrentPage,
                    PageSize = PageSize
                };

                // 根据搜索类型设置查询参数
                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    switch (SearchType)
                    {
                        case "all":
                            request.SearchKeyword = SearchKeyword;
                            break;
                        case "patient":
                            request.PatientName = SearchKeyword;
                            break;
                        case "doctor":
                            request.DoctorName = SearchKeyword;
                            break;
                        case "number":
                            request.RegistrationNumber = SearchKeyword;
                            break;
                        default:
                            request.SearchKeyword = SearchKeyword;
                            break;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"发送请求: SearchKeyword={request.SearchKeyword}, PatientName={request.PatientName}, Page={request.CurrentPage}, PageSize={request.PageSize}");
                
                var result = await _registrationService.SearchRegistrationsAsync(request);

                Registrations.Clear();
                foreach (var registration in result.Items)
                {
                    Registrations.Add(registration);
                }

                TotalCount = result.TotalCount;

                RaisePropertyChanged(nameof(StatusText));
                RaisePropertyChanged(nameof(TotalPages));

                // 更新分页命令状态
                FirstPageCommand.RaiseCanExecuteChanged();
                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
                LastPageCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"加载挂号列表失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }
}