using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Registration;
using Prism.Commands;
using Prism.Mvvm;
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
        private readonly IRegistrationService _registrationService;
        private string _searchKeyword = string.Empty;
        private string _searchType = "all";
        private RegistrationInfo _selectedRegistration;
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalCount = 0;
        private bool _isLoading = false;

        public RegistrationMainViewModel(IRegistrationService registrationService)
        {
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
                // TODO: 实现挂号新增对话框
                MessageBox.Show("新增挂号功能正在开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开新增挂号对话框失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteEditRegistration(RegistrationInfo registration)
        {
            if (registration == null) return;
            
            try
            {
                // TODO: 实现挂号编辑对话框
                MessageBox.Show($"编辑挂号 '{registration.RegistrationNumber}' 功能正在开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开编辑挂号对话框失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteDeleteRegistration(RegistrationInfo registration)
        {
            if (registration == null) return;
            
            var result = MessageBox.Show($"确定要删除挂号 '{registration.RegistrationNumber}' 吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var response = await _registrationService.DeleteRegistrationAsync(registration.Id);
                    if (response.IsSuccess)
                    {
                        MessageBox.Show("挂号删除成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadRegistrations(); // 刷新列表
                    }
                    else
                    {
                        MessageBox.Show($"删除挂号失败: {response.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除挂号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ExecuteCancelRegistration(RegistrationInfo registration)
        {
            if (registration == null) return;
            
            var result = MessageBox.Show($"确定要取消挂号 '{registration.RegistrationNumber}' 吗？", "确认取消", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var response = await _registrationService.CancelRegistrationAsync(registration.Id);
                    if (response.IsSuccess)
                    {
                        MessageBox.Show("挂号取消成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadRegistrations(); // 刷新列表
                    }
                    else
                    {
                        MessageBox.Show($"取消挂号失败: {response.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"取消挂号失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteCheckIn(RegistrationInfo registration)
        {
            if (registration == null) return;
            
            try
            {
                // TODO: 实现签到功能
                MessageBox.Show($"患者 '{registration.PatientName}' 签到功能正在开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"签到失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"加载挂号列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }
}