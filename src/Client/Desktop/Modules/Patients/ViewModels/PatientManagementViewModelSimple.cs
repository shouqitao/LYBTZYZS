using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Patients.Services.Interfaces;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Common;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.Desktop.Core.Interfaces.Services;
// UltraThink模块化架构：使用PatientModuleService实现模块化业务逻辑

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>
    /// 患者管理视图模型（UltraThink模块化重构版）
    /// </summary>
    public class PatientManagementViewModelSimple : BindableBase
    {
        private readonly IPatientModuleService _patientModuleService;
        private readonly ICustomDialogService _dialogService;
        private readonly Prism.Events.IEventAggregator _eventAggregator;

        public string ModuleName => "患者管理";

        #region Properties

        private ObservableCollection<PatientInfo> _items = new();
        public ObservableCollection<PatientInfo> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        private PatientInfo? _selectedItem;
        public PatientInfo? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage = "就绪";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
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

        #endregion

        #region Commands

        // 基础CRUD命令
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand AddCommand { get; }
        public DelegateCommand<PatientInfo> EditCommand { get; }
        public DelegateCommand<PatientInfo> DeleteCommand { get; }

        // 分页命令
        public DelegateCommand FirstPageCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand LastPageCommand { get; }

        // 患者特有命令
        public DelegateCommand<PatientInfo> ToggleStatusCommand { get; }
        public DelegateCommand<PatientInfo> ViewDetailsCommand { get; }

        #endregion

        public PatientManagementViewModelSimple(
            IPatientModuleService patientModuleService,
            ICustomDialogService dialogService,
            Prism.Events.IEventAggregator eventAggregator)
        {
            _patientModuleService = patientModuleService ?? throw new ArgumentNullException(nameof(patientModuleService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            // 初始化基础CRUD命令
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            AddCommand = new DelegateCommand(async () => await AddAsync());
            EditCommand = new DelegateCommand<PatientInfo>(async (item) => await EditAsync(item));
            DeleteCommand = new DelegateCommand<PatientInfo>(async (item) => await DeleteAsync(item));

            // 初始化分页命令
            FirstPageCommand = new DelegateCommand(async () => await FirstPageAsync());
            PreviousPageCommand = new DelegateCommand(async () => await PreviousPageAsync());
            NextPageCommand = new DelegateCommand(async () => await NextPageAsync());
            LastPageCommand = new DelegateCommand(async () => await LastPageAsync());

            // 初始化患者特有命令
            ToggleStatusCommand = new DelegateCommand<PatientInfo>(async patient => await ToggleStatusAsync(patient));
            ViewDetailsCommand = new DelegateCommand<PatientInfo>(async patient => await ViewDetailsAsync(patient));

            // 初始化数据
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await RefreshAsync();
        }

        #region 数据操作方法

        public async Task RefreshAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载患者列表...";

                var queryRequest = new PagedQueryBaseDto
                {
                    PageIndex = CurrentPage,
                    PageSize = PageSize,
                    Keyword = SearchKeyword
                };

                var result = await _patientModuleService.GetPagedAsync(queryRequest);
                
                if (result.IsSuccess && result.Data != null)
                {
                    Items.Clear();
                    foreach (var item in result.Data.Items)
                    {
                        Items.Add(item);
                    }
                    
                    TotalCount = result.Data.TotalCount;
                    StatusMessage = $"已加载 {Items.Count} 条患者记录";
                }
                else
                {
                    StatusMessage = result.ErrorMessage ?? "加载患者列表失败";
                    await _dialogService.ShowErrorAsync(StatusMessage, "错误");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"刷新失败: {ex.Message}";
                await _dialogService.ShowErrorAsync(StatusMessage, "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region 分页方法

        public async Task FirstPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage = 1;
                await RefreshAsync();
            }
        }

        public async Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await RefreshAsync();
            }
        }

        public async Task NextPageAsync()
        {
            var totalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            if (CurrentPage < totalPages)
            {
                CurrentPage++;
                await RefreshAsync();
            }
        }

        public async Task LastPageAsync()
        {
            var totalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            if (CurrentPage < totalPages)
            {
                CurrentPage = totalPages;
                await RefreshAsync();
            }
        }

        #endregion

        #region CRUD操作方法

        public async Task AddAsync()
        {
            try
            {
                StatusMessage = "新增患者功能开发中...";
                await _dialogService.ShowInformationAsync("新增患者功能正在开发中", "提示");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"添加患者失败: {ex.Message}", "错误");
            }
        }

        public async Task EditAsync(PatientInfo? item)
        {
            if (item == null) return;

            try
            {
                StatusMessage = $"编辑患者 '{item.Name}' 功能开发中...";
                await _dialogService.ShowInformationAsync($"编辑患者 '{item.Name}' 功能正在开发中", "提示");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"编辑患者失败: {ex.Message}", "错误");
            }
        }

        public async Task DeleteAsync(PatientInfo? item)
        {
            if (item == null) return;

            // 患者信息不支持删除，只能禁用
            await ToggleStatusAsync(item);
        }

        #endregion

        #region 患者特有操作方法

        /// <summary>
        /// 切换患者状态
        /// </summary>
        private async Task ToggleStatusAsync(PatientInfo? patient)
        {
            if (patient == null) return;

            var action = patient.Status == CommonStatus.Enabled ? "禁用" : "启用";
            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要{action}患者 {patient.Name} 吗？",
                $"{action}患者");

            if (confirm)
            {
                try
                {
                    IsLoading = true;
                    StatusMessage = $"正在{action}患者...";

                    // UltraThink模块化架构：使用PatientModuleService
                    ServiceResult result;
                    if (patient.Status == CommonStatus.Enabled)
                    {
                        result = await _patientModuleService.DisableAsync(patient.Id);
                    }
                    else
                    {
                        result = await _patientModuleService.EnableAsync(patient.Id);
                    }

                    if (result.IsSuccess)
                    {
                        await RefreshAsync();
                        await _dialogService.ShowInformationAsync($"患者{action}成功", "成功");
                        StatusMessage = $"{action}成功";
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                            result.ErrorMessage ?? $"患者{action}失败",
                            "错误");
                        StatusMessage = $"{action}失败";
                    }
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorAsync($"{action}患者时发生错误: {ex.Message}", "错误");
                    StatusMessage = $"{action}失败";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// 查看患者详情
        /// </summary>
        private async Task ViewDetailsAsync(PatientInfo? patient)
        {
            if (patient == null) return;

            try
            {
                StatusMessage = $"正在获取患者 '{patient.Name}' 的详情...";

                // UltraThink模块化架构：使用PatientModuleService获取详情
                var detailResult = await _patientModuleService.GetByIdAsync(patient.Id);
                if (detailResult.IsSuccess && detailResult.Data != null)
                {
                    var patientDetail = detailResult.Data;
                    var detailInfo = $"患者详情：\n\n" +
                                   $"姓名: {patientDetail.Name}\n" +
                                   $"性别: {patientDetail.GenderDisplay}\n" +
                                   $"年龄: {patientDetail.AgeText}\n" +
                                   $"电话: {patientDetail.PhoneNumber ?? "未填写"}\n" +
                                   $"身份证: {patientDetail.IdCard ?? "未填写"}\n" +
                                   $"地址: {patientDetail.Address ?? "未填写"}\n" +
                                   $"状态: {patientDetail.StatusText}\n" +
                                   $"过敏史: {patientDetail.AllergyDisplay}\n" +
                                   $"职业: {patientDetail.Occupation ?? "未填写"}\n" +
                                   $"紧急联系人: {patientDetail.EmergencyContact ?? "未填写"}\n" +
                                   $"紧急联系电话: {patientDetail.EmergencyPhone ?? "未填写"}\n" +
                                   $"创建时间: {patientDetail.CreateTimeText}";

                    await _dialogService.ShowInformationAsync(detailInfo, $"患者详情 - {patientDetail.Name}");
                    StatusMessage = "查看详情完成";
                }
                else
                {
                    await _dialogService.ShowErrorAsync(
                        detailResult.ErrorMessage ?? "获取患者详情失败", 
                        "错误");
                    StatusMessage = "获取详情失败";
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync($"查看患者详情失败: {ex.Message}", "错误");
                StatusMessage = "查看详情失败";
            }
        }

        #endregion
    }
}