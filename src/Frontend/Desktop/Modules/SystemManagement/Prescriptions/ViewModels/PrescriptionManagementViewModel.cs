using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.WPF.Client.Core.Models.Prescriptions;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Modules.SystemManagement.Common.ViewModels;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Prism.Commands;

using LYBT.WPF.Client.Core.Interfaces.Services;
namespace LYBT.WPF.Client.Modules.SystemManagement.Prescriptions.ViewModels
{
    /// <summary>
    /// 处方管理视图模型
    /// </summary>
    public class PrescriptionManagementViewModel : BaseManagementViewModel<PrescriptionInfo, IPrescriptionsApiService>
    {
        private readonly ICommonDialogService _commonDialogService;

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

        #endregion

        protected override string ModuleName => "处方";

        public PrescriptionManagementViewModel(IPrescriptionsApiService service,
            ICommonDialogService commonDialogService)
            : base(service)
        {
            _commonDialogService = commonDialogService;
            // 初始化状态选项
            StatusOptions = new List<PrescriptionStatusOption>
            {
                new(null, "全部状态"),
                new(PrescriptionStatus.Draft, "草稿"),
                new(PrescriptionStatus.Issued, "已开具"),
                new(PrescriptionStatus.Confirmed, "已确认"),
                new(PrescriptionStatus.Dispensed, "已调配"),
                new(PrescriptionStatus.Completed, "已完成"),
                new(PrescriptionStatus.Cancelled, "已取消"),
                new(PrescriptionStatus.Voided, "已作废")
            };

            // 初始化扩展命令
            ViewDetailsCommand = new DelegateCommand<PrescriptionInfo>(ExecuteViewDetails);
            PrintCommand = new DelegateCommand<PrescriptionInfo>(ExecutePrint);
            VoidCommand = new DelegateCommand<PrescriptionInfo>(ExecuteVoid);
            ClearFiltersCommand = new DelegateCommand(ExecuteClearFilters);
            ExportCommand = new DelegateCommand(ExecuteExport);

            // 设置默认时间范围（最近30天）
            EndDate = DateTime.Today;
            StartDate = DateTime.Today.AddDays(-30);
        }

        protected override async Task<ServiceResult<PagedResult<PrescriptionInfo>>> LoadDataFromServiceAsync(PaginationRequest request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"开始加载处方列表，页码: {request.CurrentPage}");

                var response = await Service.GetListAsync(
                    page: request.CurrentPage,
                    pageSize: request.PageSize,
                    keyword: SearchKeyword,
                    patientName: PatientName,
                    doctorName: DoctorName,
                    diagnosis: Diagnosis,
                    status: SelectedStatus,
                    startDate: StartDate,
                    endDate: EndDate
                );

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var paginatedResult = response.Content;
                    
                    // 转换为前端模型
                    var prescriptionInfos = paginatedResult.Items.Select(ConvertToPrescriptionInfo).ToList();

                    var result = new PagedResult<PrescriptionInfo>
                    {
                        Items = prescriptionInfos,
                        TotalCount = paginatedResult.TotalCount,
                        CurrentPage = paginatedResult.CurrentPage,
                        PageSize = paginatedResult.PageSize
                    };

                    return ServiceResult<PagedResult<PrescriptionInfo>>.Success(result);
                }
                else
                {
                    var error = response.Error?.Content ?? "获取处方列表失败";
                    return ServiceResult<PagedResult<PrescriptionInfo>>.Failure(error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载处方列表异常: {ex.Message}");
                return ServiceResult<PagedResult<PrescriptionInfo>>.Failure($"加载处方列表失败: {ex.Message}");
            }
        }

        protected override async Task<ServiceResult<bool>> DeleteFromServiceAsync(PrescriptionInfo item)
        {
            try
            {
                var response = await Service.DeleteAsync(item.Id);
                
                if (response.IsSuccessStatusCode)
                {
                    return ServiceResult<bool>.Success(true);
                }
                else
                {
                    var error = response.Error?.Content ?? "删除处方失败";
                    return ServiceResult<bool>.Failure(error);
                }
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
            
            return result ;
        }

        private PrescriptionInfo ConvertToPrescriptionInfo(PrescriptionDto dto)
        {
            return new PrescriptionInfo
            {
                Id = dto.Id,
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                CreateTime = dto.CreateTime,
                Status = dto.Status,
                // TODO: 从其他服务获取患者和医生姓名
                PatientName = "患者" + dto.PatientId.ToString()[..8],
                DoctorName = "医生" + dto.DoctorId.ToString()[..8],
                PrescriptionNumber = GeneratePrescriptionNumber(dto.Id, dto.CreateTime)
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

        private void ExecutePrint(PrescriptionInfo prescription)
        {
            if (prescription == null) return;

            try
            {
                // TODO: 实现处方打印功能
                _commonDialogService.ShowInformationAsync($"处方打印功能开发中...\n处方编号：{prescription.PrescriptionNumber}", "提示").GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打印处方失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private async void ExecuteVoid(PrescriptionInfo prescription)
        {
            if (prescription == null) return;

            // 检查是否可以作废
            if (prescription/* .Status = */= PrescriptionStatus.Voided || 
                prescription/* .Status = */= PrescriptionStatus.Cancelled)
            {
                await _commonDialogService.ShowWarningAsync("该处方已被作废或取消，无法再次作废", "无法作废");
                return;
            }

            var result = await _commonDialogService.ShowConfirmationAsync($"确定要作废该处方吗？\n患者：{prescription.PatientName}\n处方编号：{prescription.PrescriptionNumber}\n\n作废后将无法恢复！", "确认作废");

            if (result )
            {
                try
                {
                    IsLoading = true;
                    var response = await Service.CancelAsync(prescription.Id);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _commonDialogService.ShowInformationAsync("处方作废成功", "成功").GetAwaiter().GetResult();
                        RefreshCommand.Execute();
                    }
                    else
                    {
                        var error = response.Error?.Content ?? "作废处方失败";
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
                // TODO: 实现处方导出功能
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
                dialog/* .Title = */ "新增处方";
                
                // 创建 ViewModel
                var viewModel = new AddPrescriptionDialogViewModel(Service, _commonDialogService);
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
                dialog/* .Title = */ "编辑处方";
                
                // 创建 ViewModel
                var viewModel = new EditPrescriptionDialogViewModel(Service, item.Id, _commonDialogService);
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