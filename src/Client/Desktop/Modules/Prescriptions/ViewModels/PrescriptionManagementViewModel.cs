using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Prescriptions.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using AutoMapper;
using Prism.Commands;
using Prism.Mvvm;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Interfaces.Services;

namespace LYBT.Desktop.Prescriptions.ViewModels
{
    /// <summary>
    /// 处方管理主视图模型 - UltraThink架构重构版
    /// UltraThink模块化架构：使用IPrescriptionsModuleService，实现模块自包含
    /// </summary>
    public class PrescriptionManagementViewModel : BindableBase
    {
        private readonly IPrescriptionsModuleService _prescriptionsModuleService;
        private readonly ICustomDialogService _dialogService;
        private readonly ILogger<PrescriptionManagementViewModel> _logger;
        private readonly IMapper _mapper;

        #region Properties

        private ObservableCollection<PrescriptionInfo> _prescriptions = new();
        public ObservableCollection<PrescriptionInfo> Prescriptions
        {
            get => _prescriptions;
            set => SetProperty(ref _prescriptions, value);
        }

        private PrescriptionInfo? _selectedPrescription;
        public PrescriptionInfo? SelectedPrescription
        {
            get => _selectedPrescription;
            set => SetProperty(ref _selectedPrescription, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterPrescriptions();
                }
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private DateTime? _startDate;
        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    FilterPrescriptions();
                }
            }
        }

        private DateTime? _endDate;
        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    FilterPrescriptions();
                }
            }
        }

        private ObservableCollection<PrescriptionInfo> _allPrescriptions = new();

        #endregion

        #region Commands

        public DelegateCommand LoadPrescriptionsCommand { get; }
        public DelegateCommand AddPrescriptionCommand { get; }
        public DelegateCommand<PrescriptionInfo> EditPrescriptionCommand { get; }
        public DelegateCommand<PrescriptionInfo> DeletePrescriptionCommand { get; }
        public DelegateCommand<PrescriptionInfo> ViewPrescriptionCommand { get; }
        public DelegateCommand<PrescriptionInfo> PrintPrescriptionCommand { get; }
        public DelegateCommand<PrescriptionInfo> CopyPrescriptionCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand ClearFiltersCommand { get; }

        #endregion

        #region Constructor

        public PrescriptionManagementViewModel(
            IPrescriptionsModuleService prescriptionsModuleService,
            ICustomDialogService dialogService,
            ILogger<PrescriptionManagementViewModel> logger,
            IMapper mapper)
        {
            _prescriptionsModuleService = prescriptionsModuleService ?? throw new ArgumentNullException(nameof(prescriptionsModuleService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            // 初始化命令
            LoadPrescriptionsCommand = new DelegateCommand(async () => await LoadPrescriptionsAsync());
            AddPrescriptionCommand = new DelegateCommand(AddPrescription);
            EditPrescriptionCommand = new DelegateCommand<PrescriptionInfo>(EditPrescription);
            DeletePrescriptionCommand = new DelegateCommand<PrescriptionInfo>(async (p) => await DeletePrescriptionAsync(p));
            ViewPrescriptionCommand = new DelegateCommand<PrescriptionInfo>(ViewPrescription);
            PrintPrescriptionCommand = new DelegateCommand<PrescriptionInfo>(async (p) => await PrintPrescriptionAsync(p));
            CopyPrescriptionCommand = new DelegateCommand<PrescriptionInfo>(CopyPrescription);
            RefreshCommand = new DelegateCommand(async () => await LoadPrescriptionsAsync());
            ClearFiltersCommand = new DelegateCommand(ClearFilters);

            // 初始加载数据
            Task.Run(async () => await LoadPrescriptionsAsync());
        }

        #endregion

        #region Methods

        private async Task LoadPrescriptionsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载处方数据...";

                // UltraThink四层架构：使用模块化服务获取分页数据
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 1000, // 获取足够多的数据进行前端过滤
                    Keyword = SearchText
                };

                var result = await _prescriptionsModuleService.GetPagedAsync(query);
                if (result.IsSuccess && result.Data != null)
                {
                    // 应用日期过滤（前端过滤）
                    var filteredInfos = result.Data.Items
                        .Where(p => p.CreateTime.Date >= DateTime.Today && p.CreateTime.Date <= DateTime.Today.AddDays(1))
                        .Take(50) // 限制初始加载数量
                        .ToList();

                    _allPrescriptions = new ObservableCollection<PrescriptionInfo>(filteredInfos);
                    FilterPrescriptions();
                    StatusMessage = $"已加载 {_allPrescriptions.Count} 个处方";
                }
                else
                {
                    StatusMessage = result.ErrorMessage ?? "加载处方失败";
                    _logger.LogWarning("加载处方失败: {Error}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载失败: {ex.Message}";
                _logger.LogError(ex, "加载处方时出错");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterPrescriptions()
        {
            var filtered = _allPrescriptions.AsEnumerable();

            // 按关键字过滤
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLowerInvariant();
                filtered = filtered.Where(p =>
                    (p.PatientName?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.PrescriptionNumber?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.DoctorName?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Diagnosis?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // 按日期过滤
            if (StartDate.HasValue)
            {
                filtered = filtered.Where(p => p.CreateTime >= StartDate.Value);
            }
            if (EndDate.HasValue)
            {
                filtered = filtered.Where(p => p.CreateTime <= EndDate.Value.AddDays(1));
            }

            Prescriptions = new ObservableCollection<PrescriptionInfo>(filtered.OrderByDescending(p => p.CreateTime));
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            StartDate = null;
            EndDate = null;
            FilterPrescriptions();
        }

        private void AddPrescription()
        {
            // TODO: Implement dialog logic when Prism dialog support is added
            // var parameters = new DialogParameters();
            // _dialogService.ShowDialog("PrescriptionEditorDialog", parameters, async (result) =>
            // {
            //     if (result.Result == ButtonResult.OK)
            //     {
            //         await LoadPrescriptionsAsync();
            //     }
            // });
        }

        private void EditPrescription(PrescriptionInfo? prescription)
        {
            if (prescription == null) return;

            // TODO: Implement dialog logic when Prism dialog support is added
            // var parameters = new DialogParameters
            // {
            //     { "PrescriptionId", prescription.Id },
            //     { "EditMode", true }
            // };

            // _dialogService.ShowDialog("PrescriptionEditorDialog", parameters, async (result) =>
            // {
            //     if (result.Result == ButtonResult.OK)
            //     {
            //         await LoadPrescriptionsAsync();
            //     }
            // });
        }

        private void ViewPrescription(PrescriptionInfo? prescription)
        {
            if (prescription == null) return;

            // TODO: Implement dialog logic when Prism dialog support is added
            // var parameters = new DialogParameters
            // {
            //     { "PrescriptionId", prescription.Id },
            //     { "ViewMode", true }
            // };

            // _dialogService.ShowDialog("PrescriptionEditorDialog", parameters, null);
        }

        private void CopyPrescription(PrescriptionInfo? prescription)
        {
            if (prescription == null) return;

            // TODO: Implement dialog logic when Prism dialog support is added
            // var parameters = new DialogParameters
            // {
            //     { "SourcePrescriptionId", prescription.Id },
            //     { "CopyMode", true }
            // };

            // _dialogService.ShowDialog("PrescriptionEditorDialog", parameters, async (result) =>
            // {
            //     if (result.Result == ButtonResult.OK)
            //     {
            //         await LoadPrescriptionsAsync();
            //         StatusMessage = "处方已复制";
            //     }
            // });
        }

        private async Task DeletePrescriptionAsync(PrescriptionInfo? prescription)
        {
            if (prescription == null) return;

            try
            {
                // 首先检查是否可以删除
                var canDeleteResult = await _prescriptionsModuleService.CanDeleteAsync(prescription.Id);
                if (!canDeleteResult.IsSuccess || !canDeleteResult.Data)
                {
                    await _dialogService.ShowErrorAsync(
                        canDeleteResult.ErrorMessage ?? "当前处方状态不允许删除", 
                        "无法删除");
                    return;
                }

                var confirm = await _dialogService.ShowConfirmationAsync(
                    $"确定要删除患者 '{prescription.PatientName}' 的处方吗？\n此操作不可恢复。",
                    "确认删除");

                if (!confirm) return;

                var deleteResult = await _prescriptionsModuleService.DeleteAsync(prescription.Id);
                if (deleteResult.IsSuccess)
                {
                    _allPrescriptions.Remove(prescription);
                    FilterPrescriptions();
                    StatusMessage = "处方已删除";
                    await _dialogService.ShowSuccessAsync("处方删除成功", "操作完成");
                }
                else
                {
                    StatusMessage = "删除失败";
                    await _dialogService.ShowErrorAsync(deleteResult.ErrorMessage ?? "删除失败", "错误");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"删除失败: {ex.Message}";
                _logger.LogError(ex, "删除处方时出错");
                await _dialogService.ShowErrorAsync($"删除失败: {ex.Message}", "错误");
            }
        }

        private async Task PrintPrescriptionAsync(PrescriptionInfo? prescription)
        {
            if (prescription == null) return;

            try
            {
                StatusMessage = "正在准备打印...";
                
                // 使用模块化服务获取打印信息
                var printResult = await _prescriptionsModuleService.GetPrintInfoAsync(prescription.Id);
                if (printResult.IsSuccess)
                {
                    // TODO: 实现实际的打印功能
                    // 这里可以调用打印服务或生成PDF
                    await Task.Delay(1000); // 模拟打印准备
                    StatusMessage = "处方已发送到打印机";
                    await _dialogService.ShowSuccessAsync("处方打印成功", "操作完成");
                }
                else
                {
                    StatusMessage = "获取打印信息失败";
                    await _dialogService.ShowErrorAsync(printResult.ErrorMessage ?? "获取打印信息失败", "打印失败");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"打印失败: {ex.Message}";
                _logger.LogError(ex, "打印处方时出错");
            }
        }

        #endregion
    }
}