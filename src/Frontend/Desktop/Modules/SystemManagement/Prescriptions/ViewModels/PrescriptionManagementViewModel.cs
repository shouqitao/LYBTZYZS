using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Desktop.Admin.Common.ViewModels;
using LYBT.Desktop.Core.Models;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Prism.Commands;

using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Admin.Prescriptions.Services;
using Prism.Dialogs;
using LYBT.Desktop.Core.Extensions;
namespace LYBT.Desktop.Admin.Prescriptions.ViewModels
{
    /// <summary>
    /// 处方管理视图模型
    /// </summary>
    public class PrescriptionManagementViewModel : BaseManagementViewModel<PrescriptionInfo, IPrescriptionService>
    {
        private readonly IDialogService _commonDialogService;
        private readonly IPrescriptionValidationService _validationService;
        private readonly IHerbService _herbService;
        private readonly IUserSessionManager _userSessionManager;
        private readonly IPatientService _patientService;
        private readonly IPrescriptionPrintService _printService;
        
        #region 统计属性
        
        private int _todayPrescriptionCount;
        private int _weekPrescriptionCount;
        private int _monthPrescriptionCount;
        private int _pendingCount;
        private decimal _todayRevenue;
        private int _todayChange;
        private double _todayChangePercent;
        
        public int TodayPrescriptionCount
        {
            get => _todayPrescriptionCount;
            set => SetProperty(ref _todayPrescriptionCount, value);
        }
        
        public int WeekPrescriptionCount
        {
            get => _weekPrescriptionCount;
            set => SetProperty(ref _weekPrescriptionCount, value);
        }
        
        public int MonthPrescriptionCount
        {
            get => _monthPrescriptionCount;
            set => SetProperty(ref _monthPrescriptionCount, value);
        }
        
        public int PendingCount
        {
            get => _pendingCount;
            set => SetProperty(ref _pendingCount, value);
        }
        
        public decimal TodayRevenue
        {
            get => _todayRevenue;
            set => SetProperty(ref _todayRevenue, value);
        }
        
        public int TodayChange
        {
            get => _todayChange;
            set => SetProperty(ref _todayChange, value);
        }
        
        public double TodayChangePercent
        {
            get => _todayChangePercent;
            set => SetProperty(ref _todayChangePercent, value);
        }
        
        #endregion

        #region 搜索条件

        private string _patientName = string.Empty;
        private string _doctorName = string.Empty;
        private string _diagnosis = string.Empty;
        private PrescriptionStatus? _selectedStatus;
        private DateTime? _startDate;
        private DateTime? _endDate;

        /// <summary>患者姓名</summary>
        public string PatientName
        {
            get => _patientName;
            set => SetProperty(ref _patientName, value);
        }

        /// <summary>医生姓名</summary>
        public string DoctorName
        {
            get => _doctorName;
            set => SetProperty(ref _doctorName, value);
        }

        /// <summary>诊断信息</summary>
        public string Diagnosis
        {
            get => _diagnosis;
            set => SetProperty(ref _diagnosis, value);
        }

        /// <summary>选中的状态</summary>
        public PrescriptionStatus? SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }

        /// <summary>开始日期</summary>
        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        /// <summary>结束日期</summary>
        public DateTime? EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        /// <summary>状态选项列表</summary>
        public List<PrescriptionStatusOption> StatusOptions { get; }

        #endregion

        #region 扩展命令

        public DelegateCommand<PrescriptionInfo> ViewDetailsCommand { get; }
        public DelegateCommand<PrescriptionInfo> PrintCommand { get; }
        public DelegateCommand<PrescriptionInfo> VoidCommand { get; }
        public DelegateCommand ClearFiltersCommand { get; }
        public DelegateCommand ExportCommand { get; }
        public DelegateCommand ProcessPendingCommand { get; }
        public DelegateCommand BatchPrintCommand { get; }
        public DelegateCommand StatisticsCommand { get; }
        public DelegateCommand BatchPrintSelectedCommand { get; }
        public DelegateCommand BatchExportSelectedCommand { get; }
        public DelegateCommand BatchVoidSelectedCommand { get; }
        public DelegateCommand ClearSelectionCommand { get; }

        #endregion

        protected override string ModuleName => "处方";

        public PrescriptionManagementViewModel(
            IPrescriptionService service,
            IDialogService commonDialogService,
            IPrescriptionValidationService validationService,
            IHerbService herbService,
            IUserSessionManager userSessionManager,
            IPatientService patientService,
            IPrescriptionPrintService printService)
            : base(service)
        {
            _commonDialogService = commonDialogService;
            _validationService = validationService;
            _herbService = herbService;
            _userSessionManager = userSessionManager;
            _patientService = patientService;
            _printService = printService;
            // 初始化状态选项
            StatusOptions = new List<PrescriptionStatusOption>
            {
                new(null, "全部状态"),
                new(PrescriptionStatus.Draft, "编辑中"),
                new(PrescriptionStatus.Completed, "已完成")
                // 其他状态已按优化标准简化
            };

            // 初始化扩展命令
            ViewDetailsCommand = new DelegateCommand<PrescriptionInfo>(ExecuteViewDetails);
            PrintCommand = new DelegateCommand<PrescriptionInfo>(ExecutePrint);
            VoidCommand = new DelegateCommand<PrescriptionInfo>(async p => await ExecuteVoidAsync(p));
            ClearFiltersCommand = new DelegateCommand(ExecuteClearFilters);
            ExportCommand = new DelegateCommand(ExecuteExport);
            ProcessPendingCommand = new DelegateCommand(ExecuteProcessPending);
            BatchPrintCommand = new DelegateCommand(ExecuteBatchPrint);
            StatisticsCommand = new DelegateCommand(ExecuteStatistics);
            BatchPrintSelectedCommand = new DelegateCommand(ExecuteBatchPrintSelected);
            BatchExportSelectedCommand = new DelegateCommand(ExecuteBatchExportSelected);
            BatchVoidSelectedCommand = new DelegateCommand(async () => await ExecuteBatchVoidSelectedAsync());
            ClearSelectionCommand = new DelegateCommand(ExecuteClearSelection);

            // 设置默认时间范围（最近30天）
            EndDate = DateTime.Today;
            StartDate = DateTime.Today.AddDays(-30);
            
            // 加载统计数据
            _ = LoadStatisticsAsync();
        }

        protected override async Task<ServiceResult<PagedResult<PrescriptionInfo>>> LoadDataFromServiceAsync(PaginationRequest request)
        {
            try
            {
                // 转换为PagedQueryBaseDto
                var queryDto = new LYBT.Shared.Models.Contracts.Common.PagedQueryBaseDto
                {
                    PageIndex = request.CurrentPage,
                    PageSize = request.PageSize,
                    Keyword = request.SearchKeyword,
                    SortField = request.SortField,
                    IsDescending = request.IsDescending
                };

                // 使用服务层的分页查询方法
                var pagedResult = await Service.GetPagedAsync(queryDto);
                
                // 同步加载统计数据
                _ = LoadStatisticsAsync();

                if (!string.IsNullOrEmpty(pagedResult.ErrorMessage))
                {
                    return ServiceResult<PagedResult<PrescriptionInfo>>.Failure(pagedResult.ErrorMessage);
                }

                // 转换为前端模型
                var prescriptionInfos = pagedResult.Items.Select(ConvertToPrescriptionInfo).ToList();

                var result = new PagedResult<PrescriptionInfo>
                {
                    Items = prescriptionInfos,
                    TotalCount = pagedResult.TotalCount,
                    CurrentPage = pagedResult.CurrentPage,
                    PageSize = pagedResult.PageSize
                };

                return ServiceResult<PagedResult<PrescriptionInfo>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<PrescriptionInfo>>.Failure($"加载处方列表失败: {ex.Message}");
            }
        }

        protected override async Task<ServiceResult<bool>> DeleteFromServiceAsync(PrescriptionInfo item)
        {
            try
            {
                var result = await Service.DeleteAsync(item.Id);
                return result.IsSuccess
                    ? ServiceResult<bool>.Success(true)
                    : ServiceResult<bool>.Failure(result.ErrorMessage ?? "删除失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"删除处方失败: {ex.Message}");
            }
        }

        protected override string GetItemDisplayName(PrescriptionInfo item)
        {
            return $"患者：{item.PatientName}，诊断：{item.Diagnosis}";
        }

        protected override bool CanExecuteDelete(PrescriptionInfo item)
        {
            if (item == null) return false;

            // 只有草稿状态的处方可以删除
            if (item.Status != PrescriptionStatus.Draft)
            {
                _commonDialogService.ShowWarningAsync($"只有草稿状态的处方才能删除，当前状态：{item.StatusName}", "无法删除").GetAwaiter().GetResult();
                return false;
            }

            var result = _commonDialogService.ShowConfirmationAsync($"确定要删除处方吗？\n患者：{item.PatientName}\n诊断：{item.Diagnosis}\n创建时间：{item.CreateTime:yyyy-MM-dd HH:mm}", "确认删除").GetAwaiter().GetResult();

            return result;
        }

        private PrescriptionInfo ConvertToPrescriptionInfo(PrescriptionDto dto)
        {
            return new PrescriptionInfo
            {
                Id = dto.Id,
                PatientId = dto.PatientId,
                UserId = dto.DoctorId, // 医生ID（UserId）
                CreateTime = dto.CreateTime,
                Status = dto.Status,
                Diagnosis = dto.Diagnosis,
                DosageCount = dto.DosageCount,
                TotalPrice = dto.TotalPrice,
                Usage = dto.Usage,
                Remark = dto.Remark,
                // TODO: 从其他服务获取患者和医生姓名
                PatientName = dto.PatientName ?? "患者" + dto.PatientId.ToString()[..8],
                DoctorName = dto.DoctorName ?? "医生" + dto.DoctorId.ToString()[..8],
                PrescriptionNumber = GeneratePrescriptionNumber(dto.Id, dto.CreateTime),
                HerbCount = dto.Items?.Count ?? 0,
                // 设置可编辑和可作废状态
                CanEdit = dto.Status == PrescriptionStatus.Draft,
                CanVoid = dto.Status != PrescriptionStatus.Completed && dto.Status != PrescriptionStatus.Canceled
            };
        }

        private string GeneratePrescriptionNumber(Guid id, DateTime createTime)
        {
            // 生成处方编号：CF + 日期 + ID前6位
            return $"CF{createTime:yyyyMMdd}{id.ToString("N")[..6].ToUpper()}";
        }

        private void ExecuteViewDetails(PrescriptionInfo prescription)
        {
            if (prescription == null) return;

            try
            {
                // 创建处方详情查看对话框的ViewModel
                var dialogViewModel = new ViewPrescriptionDialogViewModel(Service, _commonDialogService);

                // Callbacks removed - handled through dialog result
                // TODO: 创建并显示对话框窗口
                _commonDialogService.ShowInformationAsync($"处方详情对话框功能已准备就绪\n处方编号：{prescription.PrescriptionNumber}\n患者：{prescription.PatientName}", "提示").GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开处方详情失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private async void ExecutePrint(PrescriptionInfo prescription)
        {
            if (prescription == null) return;

            try
            {
                // 加载完整处方数据
                var result = await Service.GetByIdAsync(prescription.Id);
                if (!result.IsSuccess || result.Data == null)
                {
                    await _commonDialogService.ShowErrorAsync("获取处方详情失败", "错误");
                    return;
                }

                // 映射到PrescriptionInfo
                var fullPrescription = ConvertToPrescriptionInfo(result.Data);
                fullPrescription.Items = result.Data.Items?.Select(item => new PrescriptionItemInfo
                {
                    Id = item.Id,
                    HerbId = item.HerbId,
                    HerbName = item.HerbName,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    Price = item.UnitPrice,
                    Subtotal = item.Subtotal
                }).ToList() ?? new List<PrescriptionItemInfo>();

                // 打开打印预览对话框
                var previewDialog = new Views.PrescriptionPrintPreviewDialog(_printService, fullPrescription)
                {
                    Owner = Application.Current.MainWindow
                };

                if (previewDialog.ShowDialog() == true)
                {
                    await _commonDialogService.ShowInformationAsync("打印成功", "提示");
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"打印处方失败: {ex.Message}", "错误");
            }
        }

        private async Task ExecuteVoidAsync(PrescriptionInfo prescription)
        {
            if (prescription == null) return;

            // 检查是否可以作废
            if (prescription.Status == PrescriptionStatus.Completed)
            {
                await _commonDialogService.ShowWarningAsync("该处方已完成，无法作废", "无法作废");
                return;
            }

            var result = await _commonDialogService.ShowConfirmationAsync($"确定要作废该处方吗？\n患者：{prescription.PatientName}\n处方编号：{prescription.PrescriptionNumber}\n\n作废后将无法恢复！", "确认作废");

            if (result)
            {
                try
                {
                    IsLoading = true;
                    var response = await Service.CancelAsync(prescription.Id);

                    if (response.IsSuccess)
                    {
                        _commonDialogService.ShowInformationAsync("处方作废成功", "成功").GetAwaiter().GetResult();
                        RefreshCommand.Execute();
                    }
                    else
                    {
                        var error = response.ErrorMessage ?? "作废处方失败";
                        _commonDialogService.ShowErrorAsync($"作废处方失败: {error}", "错误").GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    _commonDialogService.ShowErrorAsync($"作废处方失败: {ex.Message}", "错误").GetAwaiter().GetResult();
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void ExecuteClearFilters()
        {
            SearchKeyword = string.Empty;
            PatientName = string.Empty;
            DoctorName = string.Empty;
            Diagnosis = string.Empty;
            SelectedStatus = null;
            StartDate = DateTime.Today.AddDays(-30);
            EndDate = DateTime.Today;

            CurrentPage = 1;
            RefreshCommand.Execute();
        }

        private void ExecuteExport()
        {
            try
            {
                _commonDialogService.ShowInformationAsync("处方导出功能开发中...", "提示").GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"导出处方失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        protected override void ExecuteAdd()
        {
            try
            {
                var dialog = new Views.AddPrescriptionDialog();
                dialog.Owner = Application.Current.MainWindow;
                dialog.Title = "新增处方";

                // 创建 ViewModel
                var viewModel = new AddPrescriptionDialogViewModel(Service, _commonDialogService, _validationService, _herbService, _userSessionManager, _patientService);
                dialog.DataContext = viewModel;

                // 设置回调已移除

                if (dialog.ShowDialog() == true)
                {
                    RefreshCommand.Execute();
                    _commonDialogService.ShowInformationAsync("处方添加成功", "成功").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开新增处方对话框失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        protected override void ExecuteEdit(PrescriptionInfo item)
        {
            if (item == null) return;

            // 检查是否可以编辑
            if (item.Status != PrescriptionStatus.Draft)
            {
                _commonDialogService.ShowWarningAsync($"只有草稿状态的处方才能编辑，当前状态：{item.StatusName}", "无法编辑").GetAwaiter().GetResult();
                return;
            }

            try
            {
                var dialog = new Views.EditPrescriptionDialog();
                dialog.Owner = Application.Current.MainWindow;
                dialog.Title = "编辑处方";

                // 创建 ViewModel
                var viewModel = new EditPrescriptionDialogViewModel(Service, item.Id, _commonDialogService, _herbService);
                dialog.DataContext = viewModel;

                // 设置回调已移除

                if (dialog.ShowDialog() == true)
                {
                    RefreshCommand.Execute();
                    _commonDialogService.ShowInformationAsync("处方编辑成功", "成功").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开编辑处方对话框失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }
        
        #region 新增命令实现
        
        private void ExecuteProcessPending()
        {
            // 处理待审核处方（草稿状态）
            SelectedStatus = PrescriptionStatus.Draft;
            RefreshCommand.Execute();
        }
        
        private async void ExecuteBatchPrint()
        {
            try
            {
                // 获取选中的处方
                var selectedItems = Items.Where(i => i.IsSelected).ToList();
                if (!selectedItems.Any())
                {
                    await _commonDialogService.ShowWarningAsync("请先选择要批量打印的处方", "提示");
                    return;
                }

                var result = await _commonDialogService.ShowConfirmationAsync(
                    $"确定要批量打印{selectedItems.Count}个处方吗？",
                    "确认批量打印");

                if (result)
                {
                    IsLoading = true;
                    int successCount = await _printService.BatchPrintPrescriptions(selectedItems);
                    await _commonDialogService.ShowInformationAsync(
                        $"批量打印完成\n成功：{successCount}个\n失败：{selectedItems.Count - successCount}个",
                        "打印结果");
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"批量打印失败: {ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private void ExecuteStatistics()
        {
            // 统计分析功能
            _commonDialogService.ShowInformationAsync("统计分析功能正在开发中...", "提示").GetAwaiter().GetResult();
        }
        
        private async void ExecuteBatchPrintSelected()
        {
            var selectedItems = Items.Where(i => i.IsSelected).ToList();
            if (!selectedItems.Any())
            {
                await _commonDialogService.ShowWarningAsync("请先选择要打印的处方", "提示");
                return;
            }
            
            try
            {
                IsLoading = true;
                int successCount = await _printService.BatchPrintPrescriptions(selectedItems);
                await _commonDialogService.ShowInformationAsync(
                    $"批量打印完成\n成功：{successCount}个\n失败：{selectedItems.Count - successCount}个",
                    "打印结果");
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"批量打印失败: {ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private void ExecuteBatchExportSelected()
        {
            var selectedItems = Items.Where(i => i.IsSelected).ToList();
            if (!selectedItems.Any())
            {
                _commonDialogService.ShowWarningAsync("请先选择要导出的处方", "提示").GetAwaiter().GetResult();
                return;
            }
            
            // 批量导出选中项
            _commonDialogService.ShowInformationAsync($"正在导出{selectedItems.Count}个处方...", "导出").GetAwaiter().GetResult();
        }
        
        private async Task ExecuteBatchVoidSelectedAsync()
        {
            var selectedItems = Items.Where(i => i.IsSelected && i.CanVoid).ToList();
            if (!selectedItems.Any())
            {
                await _commonDialogService.ShowWarningAsync("请选择可作废的处方", "提示");
                return;
            }
            
            var result = await _commonDialogService.ShowConfirmationAsync(
                $"确定要作废选中的{selectedItems.Count}个处方吗？",
                "确认作废");
            
            if (result)
            {
                foreach (var item in selectedItems)
                {
                    await ExecuteVoidAsync(item);
                }
            }
        }
        
        private void ExecuteClearSelection()
        {
            foreach (var item in Items)
            {
                item.IsSelected = false;
            }
            RaisePropertyChanged(nameof(HasSelectedItems));
            RaisePropertyChanged(nameof(SelectedItemsCount));
        }
        
        private async Task LoadStatisticsAsync()
        {
            try
            {
                var today = DateTime.Today;
                var weekStart = today.AddDays(-(int)today.DayOfWeek);
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var yesterday = today.AddDays(-1);

                // 构建查询条件获取今日处方
                var todayQuery = new LYBT.Shared.Models.Contracts.Common.PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 1,
                    SortField = "CreateTime",
                    IsDescending = true
                };

                // 获取今日数据
                var todayResult = await Service.GetPagedAsync(todayQuery);
                if (todayResult != null)
                {
                    // 筛选今日的处方
                    var todayItems = todayResult.Items.Where(p => p.CreateTime.Date == today).ToList();
                    TodayPrescriptionCount = todayItems.Count;
                    
                    // 计算今日营收（处方总价）
                    TodayRevenue = todayItems.Sum(p => p.TotalPrice ?? 0);
                    
                    // 获取本周数据
                    var weekItems = todayResult.Items.Where(p => p.CreateTime.Date >= weekStart).ToList();
                    WeekPrescriptionCount = weekItems.Count;
                    
                    // 获取本月数据
                    var monthItems = todayResult.Items.Where(p => p.CreateTime.Date >= monthStart).ToList();
                    MonthPrescriptionCount = monthItems.Count;
                    
                    // 待审核数量（草稿状态）
                    PendingCount = todayResult.Items.Count(p => p.Status == PrescriptionStatus.Draft);
                    
                    // 计算较昨日变化
                    var yesterdayItems = todayResult.Items.Where(p => p.CreateTime.Date == yesterday).ToList();
                    var yesterdayCount = yesterdayItems.Count;
                    TodayChange = TodayPrescriptionCount - yesterdayCount;
                    TodayChangePercent = yesterdayCount > 0 ? (double)TodayChange / yesterdayCount : 0;
                }
                else
                {
                    // 如果获取失败，使用默认值
                    TodayPrescriptionCount = 0;
                    WeekPrescriptionCount = 0;
                    MonthPrescriptionCount = 0;
                    PendingCount = 0;
                    TodayRevenue = 0;
                    TodayChange = 0;
                    TodayChangePercent = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载统计数据失败: {ex.Message}");
                // 错误时使用默认值
                TodayPrescriptionCount = 0;
                WeekPrescriptionCount = 0;
                MonthPrescriptionCount = 0;
                PendingCount = 0;
                TodayRevenue = 0;
                TodayChange = 0;
                TodayChangePercent = 0;
            }
        }
        
        public bool HasSelectedItems => Items?.Any(i => i.IsSelected) ?? false;
        public int SelectedItemsCount => Items?.Count(i => i.IsSelected) ?? 0;
        
        #endregion
    }

    /// <summary>
    /// 处方状态选项
    /// </summary>
    public class PrescriptionStatusOption
    {
        public PrescriptionStatus? Value { get; set; }
        public string Display { get; set; } = string.Empty;

        public PrescriptionStatusOption(PrescriptionStatus? value, string display)
        {
            Value = value;
            Display = display;
        }
    }
}