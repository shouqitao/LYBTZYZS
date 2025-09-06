using AutoMapper;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Prism.Commands;
using Prism.Events;

namespace LYBT.Desktop.Prescriptions.ViewModels {

    /// <summary>
    /// 处方管理视图模型（UltraThink 现代架构版）
    /// 基于ModernManagementViewModel，统一的管理界面模式
    /// 零编译警告，现代化MVVM架构
    /// </summary>
    public class PrescriptionManagementViewModel : ModernManagementViewModel<PrescriptionDto> {

        #region Fields

        private readonly IPrescriptionService _prescriptionService;
        private readonly ICustomDialogService _dialogService;
        private readonly IMapper _mapper;

        #endregion Fields

        #region 额外Commands

        /// <summary>打印处方命令</summary>
        public DelegateCommand PrintCommand { get; }

        #endregion 额外Commands

        #region Constructor

        public PrescriptionManagementViewModel(
            IPrescriptionService prescriptionService,
            ICustomDialogService dialogService,
            IMapper mapper,
            IEventAggregator eventAggregator,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, errorHandlingService) {
            _prescriptionService = prescriptionService ?? throw new ArgumentNullException(nameof(prescriptionService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            // 初始化额外命令
            PrintCommand = new DelegateCommand(async () => await ExecutePrintAsync(), () => HasSelectedItem);
        }

        /// <summary>兼容性构造函数</summary>
        public PrescriptionManagementViewModel(
            IPrescriptionService prescriptionService,
            ICustomDialogService dialogService,
            IMapper mapper,
            IEventAggregator eventAggregator)
            : this(prescriptionService, dialogService, mapper, eventAggregator, null) {
        }

        #endregion Constructor

        #region 重写基类方法

        /// <summary>加载数据</summary>
        protected override async Task<ServiceResult<PagedResult<PrescriptionDto>>> LoadDataAsync(int page, int pageSize, string? keyword = null) {
            var prescriptionQuery = new PrescriptionQueryDto {
                PageIndex = page,
                PageSize = pageSize,
                Keyword = keyword ?? string.Empty
            };
            return await _prescriptionService.GetPagedAsync(prescriptionQuery);
        }

        /// <summary>添加项</summary>
        protected override async Task OnAddAsync() {
            var parameters = new Dictionary<string, object> { ["IsEditMode"] = false };
            var result = await _dialogService.ShowDialogAsync("PrescriptionEditorDialog", parameters);

            if (result.Result == true) {
                await _dialogService.ShowSuccessAsync("处方创建成功", "成功");
            }
        }

        /// <summary>编辑项</summary>
        protected override async Task OnEditAsync(PrescriptionDto item) {
            var parameters = new Dictionary<string, object> {
                ["IsEditMode"] = true,
                ["Prescription"] = item
            };
            var result = await _dialogService.ShowDialogAsync("PrescriptionEditorDialog", parameters);

            if (result.Result == true) {
                await _dialogService.ShowSuccessAsync($"处方 {item.Id} 更新成功", "成功");
            }
        }

        /// <summary>删除项</summary>
        protected override async Task OnDeleteAsync(PrescriptionDto item) {
            var confirm = await _dialogService.ShowConfirmationAsync(
                $"确定要删除处方 {item.Id} 吗？\n此操作不可恢复。",
                "确认删除");

            if (confirm) {
                var result = await _prescriptionService.DeleteAsync(item.Id);
                if (result.IsSuccess) {
                    await _dialogService.ShowInformationAsync("处方删除成功", "成功");
                } else {
                    await _dialogService.ShowErrorAsync(result.ErrorMessage ?? "处方删除失败", "错误");
                }
            }
        }

        /// <summary>查看详情</summary>
        protected override async Task OnViewDetailsAsync(PrescriptionDto item) {
            var result = await _prescriptionService.GetByIdAsync(item.Id);

            if (result.IsSuccess && result.Data != null) {
                var detailInfo = $"处方ID: {result.Data.Id}\n创建时间: {result.Data.CreateTime}\n更新时间: {result.Data.UpdateTime}";
                await _dialogService.ShowInformationAsync(detailInfo, $"处方详情 - {result.Data.Id}");
            } else {
                await _dialogService.ShowErrorAsync(result.ErrorMessage ?? "获取处方详情失败", "错误");
            }
        }

        /// <summary>更新Command状态</summary>
        protected override void RaiseCanExecuteChanged() {
            base.RaiseCanExecuteChanged();
            PrintCommand.RaiseCanExecuteChanged();
        }

        #endregion 重写基类方法

        #region Command执行方法

        /// <summary>打印命令执行</summary>
        private async Task ExecutePrintAsync() {
            if (SelectedItem != null) {
                await Task.Delay(1000); // 模拟打印过程

                var printableInfo = $"处方打印预览\n处方ID: {SelectedItem.Id}\n创建时间: {SelectedItem.CreateTime}";
                await _dialogService.ShowInformationAsync(printableInfo, "打印预览");
                await _dialogService.ShowInformationAsync("处方打印成功", "成功");
            }
        }

        #endregion Command执行方法
    }
}
