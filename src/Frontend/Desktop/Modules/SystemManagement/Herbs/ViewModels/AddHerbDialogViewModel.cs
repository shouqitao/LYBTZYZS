using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Threading.Tasks;
using System.Windows;

using Prism.Dialogs;
using LYBT.WPF.Client.Core.Extensions;
namespace LYBT.WPF.Client.Modules.SystemManagement.Herbs.ViewModels
{
    /// <summary>
    /// 新增药材对话框视图模型
    /// </summary>
    public class AddHerbDialogViewModel : BindableBase
    {
        private readonly IDialogService _commonDialogService;

        private readonly IHerbService _herbService;
        private readonly Window _window;

        private string _herbName = string.Empty;
        private string _pinYinCode = string.Empty;
        private string _wuBiCode = string.Empty;
        private string _origin = string.Empty;
        private string _spec = string.Empty;
        private string _unit = "克";
        private decimal _price = 0;
        private int _stock = 0;
        private string _effect = string.Empty;
        private string _usage = string.Empty;
        private string _remark = string.Empty;

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        /// <summary>药材名称</summary>
        public string HerbName
        {
            get => _herbName;
            set
            {
                if (SetProperty(ref _herbName, value))
                {
                    // 自动生成拼音码和五笔码
                    GenerateCodes();
                }
            }
        }

        /// <summary>拼音码（自动生成）</summary>
        public string PinYinCode
        {
            get => _pinYinCode;
            set => SetProperty(ref _pinYinCode, value);
        }

        /// <summary>五笔码（自动生成）</summary>
        public string WuBiCode
        {
            get => _wuBiCode;
            set => SetProperty(ref _wuBiCode, value);
        }

        /// <summary>产地</summary>
        public string Origin
        {
            get => _origin;
            set => SetProperty(ref _origin, value);
        }

        /// <summary>规格</summary>
        public string Spec
        {
            get => _spec;
            set => SetProperty(ref _spec, value);
        }

        /// <summary>单位</summary>
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        /// <summary>单价</summary>
        public decimal Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        /// <summary>库存</summary>
        public int Stock
        {
            get => _stock;
            set => SetProperty(ref _stock, value);
        }

        /// <summary>功效说明</summary>
        public string Effect
        {
            get => _effect;
            set => SetProperty(ref _effect, value);
        }

        /// <summary>用法</summary>
        public string Usage
        {
            get => _usage;
            set => SetProperty(ref _usage, value);
        }

        /// <summary>备注</summary>
        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        public AddHerbDialogViewModel(IHerbService herbService,
            IDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            _herbService = herbService;

            SaveCommand = new DelegateCommand(ExecuteSaveWrapper, CanExecuteSave)
                .ObservesProperty(() => HerbName)
                .ObservesProperty(() => Unit)
                .ObservesProperty(() => Price)
                .ObservesProperty(() => Stock);
            CancelCommand = new DelegateCommand(ExecuteCancel);

            // 获取当前窗口实例
            _window = Application.Current.Windows[Application.Current.Windows.Count - 1];
        }

        /// <summary>
        /// 自动生成拼音码和五笔码
        /// </summary>
        private void GenerateCodes()
        {
            if (!string.IsNullOrWhiteSpace(HerbName))
            {
                // 使用CommonHelper生成拼音码
                PinYinCode = CommonHelper.GetPinyinCode(HerbName);

                // 使用CommonHelper生成五笔码
                WuBiCode = CommonHelper.GetWuBiCode(HerbName);
            }
            else
            {
                PinYinCode = string.Empty;
                WuBiCode = string.Empty;
            }
        }

        private bool CanExecuteSave()
        {
            return !string.IsNullOrWhiteSpace(HerbName) &&
                   !string.IsNullOrWhiteSpace(Unit) &&
                   Price > 0 &&
                   Stock >= 0;
        }

        private async void ExecuteSaveWrapper()
        {
            await ExecuteSave();
        }

        private async Task ExecuteSave()
        {
            try
            {
                var dto = new HerbCreateDto
                {
                    Name = HerbName.Trim(),
                    PinYinCode = PinYinCode,
                    WuBiCode = WuBiCode,
                    Origin = Origin?.Trim(),
                    Spec = Spec?.Trim(),
                    Unit = Unit,
                    Price = Price,
                    /* Stock = Stock, */
                    Effect = Effect?.Trim(),
                    Usage = Usage?.Trim(),
                    Remark = Remark?.Trim(),
                    Status = CommonStatus.Enabled
                };

                var response = await _herbService.CreateHerbAsync(dto);
                if (response.IsSuccess)
                {
                    _commonDialogService.ShowInformationAsync("药材新增成功", "成功").GetAwaiter().GetResult();
                    _window.DialogResult = true;
                    _window.Close();
                }
                else
                {
                    _commonDialogService.ShowErrorAsync($"新增药材失败: {response.ErrorMessage}", "错误").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"新增药材失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private void ExecuteCancel()
        {
            _window.DialogResult = false;
            _window.Close();
        }
    }
}