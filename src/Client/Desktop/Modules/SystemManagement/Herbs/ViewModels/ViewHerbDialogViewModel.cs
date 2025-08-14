using System;
using System.Windows;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models.Herbs;
using LYBT.Shared.Models.Enums;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Dialogs;
using LYBT.Desktop.Core.Extensions;

namespace LYBT.Desktop.Admin.Herbs.ViewModels
{
    /// <summary>
    /// 查看中药材详情对话框视图模型
    /// </summary>
    public class ViewHerbDialogViewModel : BindableBase
    {
        private string _title = "详情";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }


        private readonly IDialogService _commonDialogService;
        private readonly IHerbService _herbService;

        #region 属性

        private HerbInfo? _herb;
        private bool _isLoading = true;

        /// <summary>中药材信息</summary>
        public HerbInfo? Herb
        {
            get => _herb;
            set => SetProperty(ref _herb, value);
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region 计算属性

        /// <summary>库存状态描述</summary>
        public string StockStatusDescription
        {
            get => "库存信息不可用"; // Stock字段已删除
        }

        /// <summary>库存状态颜色</summary>
        public string StockStatusColor
        {
            get => "#6C757D"; // 灰色 - Stock字段已删除
        }

        /// <summary>状态描述</summary>
        public string StatusDescription => Herb?.Status == CommonStatus.Enabled ? "正常" : "停用";

        /// <summary>状态颜色</summary>
        public string StatusColor => Herb?.Status == CommonStatus.Enabled ? "#28A745" : "#DC3545";

        /// <summary>启用状态描述</summary>
        public string ActiveStatusDescription => Herb?.Status == CommonStatus.Enabled ? "已启用" : "已禁用";

        /// <summary>启用状态颜色</summary>
        public string ActiveStatusColor => Herb?.Status == CommonStatus.Enabled ? "#28A745" : "#DC3545";

        // CreateTime和UpdateTime已按优化标准移除

        /// <summary>价格描述</summary>
        public string PriceDescription => $"￥{Herb?.Price:F2} / {Herb?.Unit}";

        /// <summary>库存描述</summary>
        public string StockDescription => "-"; // Stock字段已删除

        /// <summary>过期时间描述</summary>
        public string ExpireDateDescription => "-"; // ExpireDate字段已删除

        /// <summary>是否即将过期</summary>
        public bool IsExpiringSoon => false; // Herb?.ExpireDate 字段已移除

        /// <summary>过期状态颜色</summary>
        public string ExpireDateColor => IsExpiringSoon ? "#FFC107" : "#6C757D";

        #endregion

        #region 命令

        public DelegateCommand CloseCommand { get; }
        public DelegateCommand PrintCommand { get; }
        public DelegateCommand EditCommand { get; }

        #endregion

        public ViewHerbDialogViewModel(IHerbService herbService,
            IDialogService commonDialogService)
        {
            Title = "药材详情";
            _commonDialogService = commonDialogService;
            _herbService = herbService;

            CloseCommand = new DelegateCommand(ExecuteClose);
            PrintCommand = new DelegateCommand(ExecutePrint);
            EditCommand = new DelegateCommand(ExecuteEdit);
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("herbId"))
            {
                var id = parameters.GetValue<Guid>("herbId");
                _ = LoadHerbAsync(id);
            }

        }

        private async System.Threading.Tasks.Task LoadHerbAsync(Guid herbId)
        {
            try
            {
                IsLoading = true;
                var herb = await _herbService.GetByIdAsync(herbId);

                if (herb != null)
                {
                    Herb = herb;
                    UpdateComputedProperties();
                }
                else
                {
                    await _commonDialogService.ShowErrorAsync("未找到指定的中药材信息", "错误");
                    RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"加载中药材信息失败：{ex.Message}", "错误");
                RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteClose()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.OK));
        }

        private void ExecutePrint()
        {
            _commonDialogService.ShowInformationAsync("中药材信息打印功能开发中...", "提示").GetAwaiter().GetResult();
        }

        private void ExecuteEdit()
        {
            if (Herb != null)
            {
                var parameters = new DialogParameters
                {
                    { "herb", Herb }
                };
                RaiseRequestClose(new DialogResult(ButtonResult.OK));
            }
        }

        private void UpdateComputedProperties()
        {
            // 触发计算属性更新
            RaisePropertyChanged(nameof(StockStatusDescription));
            RaisePropertyChanged(nameof(StockStatusColor));
            RaisePropertyChanged(nameof(StatusDescription));
            RaisePropertyChanged(nameof(StatusColor));
            RaisePropertyChanged(nameof(ActiveStatusDescription));
            RaisePropertyChanged(nameof(ActiveStatusColor));
            // CreateTimeDescription和UpdateTimeDescription已按优化标准移除
            RaisePropertyChanged(nameof(PriceDescription));
            RaisePropertyChanged(nameof(StockDescription));
            RaisePropertyChanged(nameof(ExpireDateDescription));
            RaisePropertyChanged(nameof(IsExpiringSoon));
            RaisePropertyChanged(nameof(ExpireDateColor));
        }
        // 临时占位方法 - 等待IDialogAware问题解决
        private void RaiseRequestClose(IDialogResult dialogResult)
        {
            // 等待IDialogAware接口实现
        }
    }
}