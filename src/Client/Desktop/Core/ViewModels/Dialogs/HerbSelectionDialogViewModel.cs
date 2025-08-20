using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Core.Models.Common;
// UltraThink v2.0: 直接使用HerbDto，移除Info模型引用
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Desktop.Core.Mvvm;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Desktop.Core.Extensions;

namespace LYBT.Desktop.Core.ViewModels.Dialogs
{
    /// <summary>
    /// 中药材选择对话框ViewModel（兼容新对话框系统）
    /// </summary>
    public class HerbSelectionDialogViewModel : ObservableObject, ICustomDialogAware
    {
        private readonly LYBT.Shared.Interfaces.Services.IHerbService _herbService;
        private string _title = "选择中药材";
        private string _searchKeyword = string.Empty;
        private bool _isLoading;
        private HerbDto? _selectedHerb;
        private decimal _quantity = 10;
        private string _unit = "g";

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    SearchCommand?.Execute(null);
                }
            }
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// 选中的中药材
        /// </summary>
        public HerbDto? SelectedHerb
        {
            get => _selectedHerb;
            set
            {
                if (SetProperty(ref _selectedHerb, value))
                {
                    if (_selectedHerb != null)
                    {
                        Unit = _selectedHerb.Unit ?? "g";
                    }
                    ConfirmCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 用量
        /// </summary>
        public decimal Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        /// <summary>
        /// 单位
        /// </summary>
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        /// <summary>
        /// 中药材列表
        /// </summary>
        public ObservableCollection<HerbDto> Herbs { get; } = new();

        /// <summary>
        /// 搜索命令
        /// </summary>
        public ICommand SearchCommand { get; }

        /// <summary>
        /// 确认命令
        /// </summary>
        public RelayCommand ConfirmCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// 请求关闭事件
        /// </summary>
        public event Action<CustomDialogResult> RequestClose = delegate { };

        /// <summary>
        /// 构造函数
        /// </summary>
        public HerbSelectionDialogViewModel(LYBT.Shared.Interfaces.Services.IHerbService herbService)
        {
            _herbService = herbService;

            SearchCommand = new RelayCommand(async () => await ExecuteSearchAsync());
            ConfirmCommand = new RelayCommand(ExecuteConfirm, CanConfirm);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        /// <summary>
        /// 检查是否可以关闭对话框
        /// </summary>
        public bool CanCloseDialog() => true;

        /// <summary>
        /// 对话框打开时调用
        /// </summary>
        public void OnDialogOpened(Dictionary<string, object> parameters)
        {
            if (parameters.ContainsKey("Title"))
            {
                Title = parameters["Title"]?.ToString() ?? "选择中药材";
            }

            if (parameters.ContainsKey("DefaultQuantity"))
            {
                if (decimal.TryParse(parameters["DefaultQuantity"]?.ToString(), out var defaultQuantity))
                {
                    Quantity = defaultQuantity;
                }
            }

            // 加载中药材列表
            Task.Run(async () => await LoadHerbsAsync());
        }

        /// <summary>
        /// 对话框关闭时调用
        /// </summary>
        public void OnDialogClosed()
        {
            // 清理资源
        }

        /// <summary>
        /// 加载中药材列表
        /// </summary>
        private async Task LoadHerbsAsync()
        {
            try
            {
                IsLoading = true;

                var result = await _herbService.GetListAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Herbs.Clear();
                        foreach (var herbDto in result.Data)
                        {
                            Herbs.Add(herbDto);
                        }
                    });
                }
            }
            catch (Exception)
            {
                // 错误处理
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 执行搜索
        /// </summary>
        private async Task ExecuteSearchAsync()
        {
            try
            {
                IsLoading = true;

                var result = await _herbService.GetListAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    var filteredHerbs = string.IsNullOrWhiteSpace(SearchKeyword)
                        ? result.Data
                        : result.Data.Where(h => h.Name.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase));

                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Herbs.Clear();
                        foreach (var herbDto in filteredHerbs)
                        {
                            Herbs.Add(herbDto);
                        }
                    });
                }
            }
            catch (Exception)
            {
                // 错误处理
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 检查是否可以确认
        /// </summary>
        private bool CanConfirm()
        {
            return SelectedHerb != null && Quantity > 0;
        }

        /// <summary>
        /// 执行确认
        /// </summary>
        private void ExecuteConfirm()
        {
            if (SelectedHerb == null) return;

            var result = CustomDialogResult.Success();
            result.Parameters["SelectedHerb"] = SelectedHerb;
            result.Parameters["Quantity"] = Quantity;
            result.Parameters["Unit"] = Unit;

            RequestClose?.Invoke(result);
        }

        /// <summary>
        /// 执行取消
        /// </summary>
        private void ExecuteCancel()
        {
            RequestClose?.Invoke(CustomDialogResult.Cancel());
        }
    }
}