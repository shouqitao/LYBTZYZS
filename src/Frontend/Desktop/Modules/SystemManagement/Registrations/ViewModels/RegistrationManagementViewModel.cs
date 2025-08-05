using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Registration;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using LYBT.WPF.Client.Core.Models.Doctors;

namespace LYBT.WPF.Client.Modules.SystemManagement.Registrations.ViewModels
{
    /// <summary>
    /// 挂号管理视图模型
    /// </summary>
    public class RegistrationManagementViewModel : BindableBase
    {
        private readonly IRegistrationService _registrationService;

        #region Properties

        private ObservableCollection<RegistrationInfo> _registrations = new();
        public ObservableCollection<RegistrationInfo> Registrations
        {
            get => _registrations;
            set => SetProperty(ref _registrations, value);
        }

        private RegistrationInfo? _selectedRegistration;
        public RegistrationInfo? SelectedRegistration
        {
            get => _selectedRegistration;
            set => SetProperty(ref _selectedRegistration, value);
        }

        private string _searchPatientName = string.Empty;
        public string SearchPatientName
        {
            get => _searchPatientName;
            set => SetProperty(ref _searchPatientName, value);
        }

        private DateTime? _searchDate;
        public DateTime? SearchDate
        {
            get => _searchDate;
            set => SetProperty(ref _searchDate, value);
        }

        private string _selectedStatus = "全部";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }

        private string _selectedDepartment = "全部";
        public string SelectedDepartment
        {
            get => _selectedDepartment;
            set => SetProperty(ref _selectedDepartment, value);
        }

        private ObservableCollection<string> _statusList = new();
        public ObservableCollection<string> StatusList
        {
            get => _statusList;
            set => SetProperty(ref _statusList, value);
        }

        private ObservableCollection<string> _departmentList = new();
        public ObservableCollection<string> DepartmentList
        {
            get => _departmentList;
            set => SetProperty(ref _departmentList, value);
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
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                    CurrentPage = 1;
                    _ = LoadRegistrationsAsync();
                }
            }
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Commands

        public DelegateCommand SearchCommand { get; }
        public DelegateCommand ResetCommand { get; }
        public DelegateCommand AddRegistrationCommand { get; }
        public DelegateCommand BatchCancelCommand { get; }
        public DelegateCommand ExportCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand<RegistrationInfo> ViewCommand { get; }
        public DelegateCommand<RegistrationInfo> EditCommand { get; }
        public DelegateCommand<RegistrationInfo> CancelCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }

        #endregion

        public RegistrationManagementViewModel(IRegistrationService registrationService)
        {
            _registrationService = registrationService;

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await SearchRegistrations());
            ResetCommand = new DelegateCommand(async () => await ResetSearch());
            AddRegistrationCommand = new DelegateCommand(AddRegistration);
            BatchCancelCommand = new DelegateCommand(async () => await BatchCancel());
            ExportCommand = new DelegateCommand(async () => await ExportRegistrations());
            RefreshCommand = new DelegateCommand(async () => await LoadRegistrationsAsync());
            ViewCommand = new DelegateCommand<RegistrationInfo>(ViewRegistration);
            EditCommand = new DelegateCommand<RegistrationInfo>(EditRegistration);
            CancelCommand = new DelegateCommand<RegistrationInfo>(async (r) => await CancelRegistration(r));
            PreviousPageCommand = new DelegateCommand(PreviousPage, CanPreviousPage)
                .ObservesProperty(() => CurrentPage);
            NextPageCommand = new DelegateCommand(NextPage, CanNextPage)
                .ObservesProperty(() => CurrentPage)
                .ObservesProperty(() => TotalPages);

            // 初始化数据
            InitializeLists();
            
            // 初始化加载数据
            _ = LoadRegistrationsAsync();
        }

        private void InitializeLists()
        {
            // 初始化状态列表
            StatusList.Clear();
            StatusList.Add("全部");
            StatusList.Add("已预约");
            StatusList.Add("已到达");
            StatusList.Add("就诊中");
            StatusList.Add("已完成");
            StatusList.Add("已取消");
            StatusList.Add("爽约");
            StatusList.Add("已过期");

            // 初始化科室列表
            DepartmentList.Clear();
            DepartmentList.Add("全部");
            DepartmentList.Add("内科");
            DepartmentList.Add("外科");
            DepartmentList.Add("妇科");
            DepartmentList.Add("儿科");
            DepartmentList.Add("中医科");
            DepartmentList.Add("皮肤科");
            DepartmentList.Add("骨科");
            DepartmentList.Add("眼科");
            DepartmentList.Add("耳鼻喉科");
        }

        private async Task LoadRegistrationsAsync()
        {
            try
            {
                IsLoading = true;

                var status = SelectedStatus == "全部" ? null : SelectedStatus;
                var department = SelectedDepartment == "全部" ? null : SelectedDepartment;

                var result = await _registrationService.GetPagedAsync(
                    CurrentPage, 
                    PageSize, 
                    SearchPatientName, 
                    SearchDate, 
                    SearchDate, 
                    status, 
                    null);
                
                Registrations.Clear();
                foreach (var item in result.Items)
                {
                    Registrations.Add(item);
                }
                TotalCount = result.TotalCount;
                RaisePropertyChanged(nameof(TotalPages));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载挂号列表时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private RegistrationStatus? ConvertToRegistrationStatus(string status)
        {
            return status switch
            {
                "已预约" => RegistrationStatus.Scheduled,
                "已到达" => RegistrationStatus.Arrived,
                "就诊中" => RegistrationStatus.InConsultation,
                "已完成" => RegistrationStatus.Completed,
                "已取消" => RegistrationStatus.Cancelled,
                "爽约" => RegistrationStatus.NoShow,
                "已过期" => RegistrationStatus.Expired,
                _ => null
            };
        }

        private async Task SearchRegistrations()
        {
            CurrentPage = 1;
            await LoadRegistrationsAsync();
        }

        private async Task ResetSearch()
        {
            SearchPatientName = string.Empty;
            SearchDate = null;
            SelectedStatus = "全部";
            SelectedDepartment = "全部";
            CurrentPage = 1;
            await LoadRegistrationsAsync();
        }

        private void AddRegistration()
        {
            try
            {
                var dialog = new Views.AddRegistrationDialog();
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    _ = LoadRegistrationsAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开新增挂号对话框失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task BatchCancel()
        {
            var selectedItems = Registrations.Where(r => r.IsSelected && r.CanCancel).ToList();
            if (!selectedItems.Any())
            {
                MessageBox.Show("请选择要取消的挂号记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"确定要取消选中的 {selectedItems.Count} 条挂号记录吗？", 
                "确认取消", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var ids = selectedItems.Select(r => r.Id).ToList();
                    var response = await _registrationService.BatchCancelAsync(ids);
                    if (response.IsSuccess)
                    {
                        MessageBox.Show($"成功取消 {selectedItems.Count} 条挂号记录", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadRegistrationsAsync();
                    }
                    else
                    {
                        MessageBox.Show($"批量取消失败：{response.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"批量取消时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private Task ExportRegistrations()
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    DefaultExt = "xlsx",
                    FileName = $"挂号列表_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // TODO: 实现导出功能
                    MessageBox.Show("导出功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            return Task.CompletedTask;
        }

        private void ViewRegistration(RegistrationInfo registration)
        {
            if (registration == null) return;

            try
            {
                var dialog = new Views.ViewRegistrationDialog(registration.Id);
                dialog.Owner = Application.Current.MainWindow;
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开挂号详情对话框失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditRegistration(RegistrationInfo registration)
        {
            if (registration == null || !registration.CanEdit) return;

            try
            {
                var dialog = new Views.EditRegistrationDialog(registration.Id);
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    _ = LoadRegistrationsAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开编辑挂号对话框失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CancelRegistration(RegistrationInfo registration)
        {
            if (registration == null || !registration.CanCancel) return;

            var result = MessageBox.Show($"确定要取消挂号单 {registration.RegistrationNo} 吗？", 
                "确认取消", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var response = await _registrationService.CancelAsync(registration.Id);
                    if (response.IsSuccess)
                    {
                        MessageBox.Show("挂号已取消", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadRegistrationsAsync();
                    }
                    else
                    {
                        MessageBox.Show($"取消挂号失败：{response.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"取消挂号时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanPreviousPage() => CurrentPage > 1;
        private void PreviousPage()
        {
            if (CanPreviousPage())
            {
                CurrentPage--;
                _ = LoadRegistrationsAsync();
            }
        }

        private bool CanNextPage() => CurrentPage < TotalPages;
        private void NextPage()
        {
            if (CanNextPage())
            {
                CurrentPage++;
                _ = LoadRegistrationsAsync();
            }
        }
    }
}