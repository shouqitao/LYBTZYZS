using System;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Herbs;

using LYBT.WPF.Client.Core.Interfaces.Services;
namespace LYBT.WPF.Client.Modules.SystemManagement.Herbs.ViewModels
{
    /// <summary>
    /// 库存管理对话框视图模型
    /// </summary>
    public class StockManagementDialogViewModel : BindableBase
    {
        private readonly ICommonDialogService _commonDialogService;

        private readonly IHerbApiService _herbService;

        #region 属性

        private HerbInfo _herb = new();
        public HerbInfo Herb
        {
            get => _herb;
            set => SetProperty(ref _herb, value);
        }

        private int _currentStock;
        public int CurrentStock
        {
            get => _currentStock;
            set => SetProperty(ref _currentStock, value);
        }

        private int _adjustmentQuantity = 0;
        public int AdjustmentQuantity
        {
            get => _adjustmentQuantity;
            set
            {
                if (SetProperty(ref _adjustmentQuantity, value))
                {
                    RaisePropertyChanged(nameof(NewStock));
                    RaisePropertyChanged(nameof(AdjustmentType));
                }
            }
        }

        private string _adjustmentReason = string.Empty;
        public string AdjustmentReason
        {
            get => _adjustmentReason;
            set => SetProperty(ref _adjustmentReason, value);
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>调整后库存</summary>
        public int NewStock => CurrentStock + AdjustmentQuantity;

        /// <summary>调整类型</summary>
        public string AdjustmentType => AdjustmentQuantity > 0 ? "入库" : AdjustmentQuantity < 0 ? "出库" : "无变化";

        /// <summary>库存状态描述</summary>
        public string StockStatusDescription
        {
            get
            {
                if (NewStock <= 0) return "库存不足";
                if (NewStock <= 10) return "库存预警";
                return "库存正常";
            }
        }

        #endregion

        #region 命令

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand QuickAddCommand { get; }
        public DelegateCommand QuickReduceCommand { get; }

        #endregion

        #region 构造函数

        public StockManagementDialogViewModel(IHerbApiService herbService,
            ICommonDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            _herbService = herbService;

            SaveCommand = new DelegateCommand(async () => await ExecuteSave(), CanExecuteSave);
            CancelCommand = new DelegateCommand(ExecuteCancel);
            QuickAddCommand = new DelegateCommand(() => AdjustmentQuantity += 10);
            QuickReduceCommand = new DelegateCommand(() => AdjustmentQuantity -= 10);
        }

        #endregion

        #region 回调

        public Action? CloseDialogCallback { get; set; }
        public Action<bool>? SaveCompleteCallback { get; set; }

        #endregion

        #region 方法

        /// <summary>
        /// 设置要管理的药材
        /// </summary>
        public void SetHerb(HerbInfo herb)
        {
            Herb = herb;
            CurrentStock = herb.Stock;
            AdjustmentQuantity = 0;
            AdjustmentReason = string.Empty;
        }

        private bool CanExecuteSave()
        {
            return AdjustmentQuantity != 0 && !string.IsNullOrWhiteSpace(AdjustmentReason) && !IsLoading;
        }

        private async Task ExecuteSave()
        {
            try
            {
                IsLoading = true;

                // 验证库存调整合理性
                if (NewStock < 0)
                {
                    await _commonDialogService.ShowWarningAsync("调整后库存不能为负数", "调整失败");
                    return;
                }

                // 创建库存调整DTO
                var updateDto = new HerbUpdateDto
                {
                    Id = Herb.Id,
                    Name = Herb.Name,
                    Origin = Herb.Origin,
                    Spec = Herb.Spec,
                    Unit = Herb.Unit,
                    Price = Herb.Price,
                    Stock = NewStock, // 更新后的库存
                    BatchNo = Herb.BatchNo,
                    PinYinCode = Herb.PinYinCode,
                    WuBiCode = Herb.WuBiCode,
                    Remark = $"{Herb.StatusDescription}\n[库存调整] {DateTime.Now:yyyy-MM-dd HH:mm} {AdjustmentType} {Math.Abs(AdjustmentQuantity)} {Herb.Unit}，原因：{AdjustmentReason}"
                };

                var result = await _herbService.UpdateHerbAsync(updateDto);
                if (result.IsSuccessStatusCode)
                {
                    _commonDialogService.ShowInformationAsync($"库存调整成功！\n{Herb.Name} 库存从 {CurrentStock} 调整为 {NewStock}", "调整成功").GetAwaiter().GetResult();
                    
                    SaveCompleteCallback?.Invoke(true);
                    CloseDialogCallback?.Invoke();
                }
                else
                {
                    var error = result.Error?.Content ?? "库存调整失败";
                    _commonDialogService.ShowErrorAsync($"库存调整失败：{error}", "调整失败").GetAwaiter().GetResult();
                    SaveCompleteCallback?.Invoke(false);
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"库存调整失败：{ex.Message}", "调整失败").GetAwaiter().GetResult();
                SaveCompleteCallback?.Invoke(false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteCancel()
        {
            CloseDialogCallback?.Invoke();
        }

        #endregion
    }
}