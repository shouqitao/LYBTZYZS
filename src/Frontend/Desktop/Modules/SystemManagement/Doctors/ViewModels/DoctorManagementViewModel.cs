using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Doctors;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Doctors;
using LYBT.WPF.Client.Core.Interfaces.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using System.ComponentModel;
using System.Windows.Data;
using System.Linq;

namespace LYBT.WPF.Client.Modules.SystemManagement.Doctors.ViewModels
{
    /// <summary>
    /// 医生管理视图模型
    /// </summary>
    public class DoctorManagementViewModel : BindableBase
    {
        private readonly IDoctorService _doctorService;
        
        private string _searchKeyword = string.Empty;
        private DoctorInfo? _selectedDoctor;
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalCount = 0;
        private bool _isLoading = false;

        public ObservableCollection<DoctorInfo> Doctors { get; }
        public ICollectionView DoctorsView { get; }

        // Commands
        public DelegateCommand SearchCommand { get; }
        public DelegateCommand AddDoctorCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand<DoctorInfo> EditDoctorCommand { get; }
        public DelegateCommand<DoctorInfo> ViewDoctorCommand { get; }
        public DelegateCommand<DoctorInfo> ToggleDoctorStatusCommand { get; }
        public DelegateCommand FirstPageCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand LastPageCommand { get; }

        /// <summary>搜索关键词</summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        /// <summary>选中的医生</summary>
        public DoctorInfo? SelectedDoctor
        {
            get => _selectedDoctor;
            set => SetProperty(ref _selectedDoctor, value);
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
                    RaisePropertyChanged(nameof(TotalPages));
                    RaisePropertyChanged(nameof(StatusText));
                    RaisePropertyChanged(nameof(CanGoFirstPage));
                    RaisePropertyChanged(nameof(CanGoPreviousPage));
                    RaisePropertyChanged(nameof(CanGoNextPage));
                    RaisePropertyChanged(nameof(CanGoLastPage));
                }
            }
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>总页数</summary>
        public int TotalPages => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

        /// <summary>状态文本</summary>
        public string StatusText => $"第 {CurrentPage} 页，共 {TotalPages} 页，总计 {TotalCount} 条记录";

        /// <summary>是否可以跳转到第一页</summary>
        public bool CanGoFirstPage => CurrentPage > 1;

        /// <summary>是否可以跳转到上一页</summary>
        public bool CanGoPreviousPage => CurrentPage > 1;

        /// <summary>是否可以跳转到下一页</summary>
        public bool CanGoNextPage => CurrentPage < TotalPages;

        /// <summary>是否可以跳转到最后一页</summary>
        public bool CanGoLastPage => CurrentPage < TotalPages;

        public DoctorManagementViewModel(IDoctorService doctorService)
        {
            _doctorService = doctorService;

            Doctors = new ObservableCollection<DoctorInfo>();
            DoctorsView = CollectionViewSource.GetDefaultView(Doctors);

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await LoadDoctorsAsync());
            AddDoctorCommand = new DelegateCommand(ExecuteAddDoctor);
            RefreshCommand = new DelegateCommand(async () => await LoadDoctorsAsync());
            EditDoctorCommand = new DelegateCommand<DoctorInfo>(ExecuteEditDoctor);
            ViewDoctorCommand = new DelegateCommand<DoctorInfo>(ExecuteViewDoctor);
            ToggleDoctorStatusCommand = new DelegateCommand<DoctorInfo>(async (doctor) => await ExecuteToggleDoctorStatus(doctor));
            
            FirstPageCommand = new DelegateCommand(async () => { CurrentPage = 1; await LoadDoctorsAsync(); }, () => CanGoFirstPage);
            PreviousPageCommand = new DelegateCommand(async () => { CurrentPage--; await LoadDoctorsAsync(); }, () => CanGoPreviousPage);
            NextPageCommand = new DelegateCommand(async () => { CurrentPage++; await LoadDoctorsAsync(); }, () => CanGoNextPage);
            LastPageCommand = new DelegateCommand(async () => { CurrentPage = TotalPages; await LoadDoctorsAsync(); }, () => CanGoLastPage);

            // 加载初始数据
            _ = LoadDoctorsAsync();
        }

        /// <summary>
        /// 加载医生列表
        /// </summary>
        private async Task LoadDoctorsAsync()
        {
            try
            {
                IsLoading = true;
                Doctors.Clear();

                var request = new PaginationRequest
                {
                    CurrentPage = CurrentPage,
                    PageSize = PageSize,
                    SearchKeyword = SearchKeyword
                };

                // 暂时使用GetDoctorsAsync，后续需要实现分页功能
                var result = await _doctorService.GetDoctorsAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    var allDoctors = result.Data;
                    // 手动实现分页
                    var pagedDoctors = allDoctors
                        .Where(d => string.IsNullOrEmpty(SearchKeyword) || 
                                    d.Name.Contains(SearchKeyword) || 
                                    d.Department?.Contains(SearchKeyword) == true)
                        .Skip((CurrentPage - 1) * PageSize)
                        .Take(PageSize)
                        .ToList();
                    
                    TotalCount = allDoctors.Count(d => string.IsNullOrEmpty(SearchKeyword) || 
                                                       d.Name.Contains(SearchKeyword) || 
                                                       d.Department?.Contains(SearchKeyword) == true);
                    
                    foreach (var doctor in pagedDoctors)
                    {
                        Doctors.Add(doctor);
                    }
                }
                else
                {
                    MessageBox.Show($"加载医生列表失败：{result.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载医生列表失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 新增医生
        /// </summary>
        private void ExecuteAddDoctor()
        {
            // TODO: 实现新增医生对话框
            MessageBox.Show("新增医生功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 编辑医生
        /// </summary>
        private void ExecuteEditDoctor(DoctorInfo doctor)
        {
            if (doctor == null) return;

            // TODO: 实现编辑医生对话框
            MessageBox.Show("编辑医生功能开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 查看医生详情
        /// </summary>
        private void ExecuteViewDoctor(DoctorInfo doctor)
        {
            if (doctor == null) return;

            // TODO: 实现查看医生详情对话框
            MessageBox.Show($"医生信息：\n姓名：{doctor.Name}\n工号：{doctor.Code}\n科室：{doctor.Department}", "医生详情", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 切换医生状态
        /// </summary>
        private async Task ExecuteToggleDoctorStatus(DoctorInfo doctor)
        {
            if (doctor == null) return;

            var action = doctor.IsActive ? "停用" : "启用";
            var confirmResult = MessageBox.Show($"确定要{action}医生 {doctor.Name} 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmResult != MessageBoxResult.Yes) return;

            try
            {
                // 暂时使用UpdateDoctorAsync，后续需要实现专门的状态切换功能
                doctor.IsActive = !doctor.IsActive;
                var result = await _doctorService.UpdateDoctorAsync(doctor);
                if (result.IsSuccess)
                {
                    doctor.IsActive = !doctor.IsActive;
                    DoctorsView.Refresh();
                    MessageBox.Show($"{action}成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"{action}失败：{result.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{action}失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}