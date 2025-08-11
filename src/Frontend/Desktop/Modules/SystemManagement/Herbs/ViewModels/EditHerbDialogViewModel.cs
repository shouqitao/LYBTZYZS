using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models.Herbs;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Threading.Tasks;
using System.Windows;

using Prism.Dialogs;
using LYBT.Desktop.Core.Extensions;
namespace LYBT.Desktop.Admin.Herbs.ViewModels
{
    /// <summary>
    /// 编辑药材对话框视图模型
    /// </summary>
    public class EditHerbDialogViewModel : BindableBase
    {
        private readonly IDialogService _commonDialogService;

        private readonly IHerbService _herbService;
        private readonly Window _window;
        private HerbInfo? _originalHerb;

        private string _herbName = string.Empty;
        private string _pinYinCode = string.Empty;
        private string _wuBiCode = string.Empty;
        private string _origin = string.Empty;
        private string _spec = string.Empty;
        private string _unit = "克";
        private decimal _price = 0;
        private int _stock = 0;
        private int _status = 1;
        private string _effect = string.Empty;
        private string _usage = string.Empty;
        private string _remark = string.Empty;

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand RegeneratePinYinCommand { get; }
        public DelegateCommand RegenerateWuBiCommand { get; }

        /// <summary>药材名称</summary>
        public string HerbName
        {
            get => _herbName;
            set => SetProperty(ref _herbName, value);
        }

        /// <summary>拼音码（可手动修改）</summary>
        public string PinYinCode
        {
            get => _pinYinCode;
            set => SetProperty(ref _pinYinCode, value);
        }

        /// <summary>五笔码（可手动修改）</summary>
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

        /// <summary>库存（只读）</summary>
        public int Stock
        {
            get => _stock;
            set => SetProperty(ref _stock, value);
        }

        /// <summary>状态</summary>
        public int Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
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

        public EditHerbDialogViewModel(IHerbService herbService,
            IDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            _herbService = herbService;

            SaveCommand = new DelegateCommand(async () => await ExecuteSave(), CanExecuteSave);
            CancelCommand = new DelegateCommand(ExecuteCancel);
            RegeneratePinYinCommand = new DelegateCommand(ExecuteRegeneratePinYin);
            RegenerateWuBiCommand = new DelegateCommand(ExecuteRegenerateWuBi);

            // 获取当前窗口实例
            _window = Application.Current.Windows[Application.Current.Windows.Count - 1];
        }

        /// <summary>
        /// 设置要编辑的药材信息
        /// </summary>
        public void SetHerb(HerbInfo herb)
        {
            _originalHerb = herb ?? throw new ArgumentNullException(nameof(herb));

            HerbName = herb.Name ?? string.Empty;
            PinYinCode = herb.PinYinCode ?? string.Empty;
            // Stock = herb.Stock; // 字段已移除
            Origin = herb.Origin ?? string.Empty;
            Spec = herb.Spec ?? string.Empty;
            Unit = herb.Unit ?? "克";
            Price = herb.Price;
            // BatchNo = herb.BatchNo; // 字段已移除
            Status = 0; // herb.Status 字段已移除
            Effect = herb.Effect ?? string.Empty;
            Usage = herb.Usage ?? string.Empty;
            Remark = herb.Remark ?? string.Empty;
        }

        /// <summary>
        /// 重新生成拼音码
        /// </summary>
        private void ExecuteRegeneratePinYin()
        {
            if (!string.IsNullOrWhiteSpace(HerbName))
            {
                PinYinCode = CommonHelper.GetPinyinCode(HerbName);
            }
        }

        /// <summary>
        /// 重新生成五笔码
        /// </summary>
        private void ExecuteRegenerateWuBi()
        {
            if (!string.IsNullOrWhiteSpace(HerbName))
            {
                WuBiCode = CommonHelper.GetWuBiCode(HerbName);
            }
        }

        private bool CanExecuteSave()
        {
            return !string.IsNullOrWhiteSpace(HerbName) &&
                   !string.IsNullOrWhiteSpace(Unit) &&
                   Price > 0;
        }

        private async Task ExecuteSave()
        {
            try
            {
                if (_originalHerb == null)
                {
                    await _commonDialogService.ShowErrorAsync("原始药材信息不能为空", "错误");
                    return;
                }

                var dto = new HerbUpdateDto
                {
                    Id = _originalHerb.Id,
                    Name = HerbName.Trim(),
                    PinYinCode = PinYinCode?.Trim(),
                    WuBiCode = WuBiCode?.Trim(),
                    Origin = Origin?.Trim(),
                    Spec = Spec?.Trim(),
                    Unit = Unit,
                    Price = Price,
                    /* Stock = Stock, */ // 库存在编辑中不应该被修改，但保持原值
                    Effect = Effect?.Trim(),
                    Usage = Usage?.Trim(),
                    Remark = Remark?.Trim(),
                    Status = (CommonStatus)Status // 使用CommonStatus枚举
                    // IsActive已按优化标准移除
                };

                var response = await _herbService.UpdateHerbAsync(dto);
                if (response.IsSuccess)
                {
                    _commonDialogService.ShowInformationAsync("药材更新成功", "成功").GetAwaiter().GetResult();
                    _window.DialogResult = true;
                    _window.Close();
                }
                else
                {
                    _commonDialogService.ShowErrorAsync($"更新药材失败: {response.ErrorMessage}", "错误").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"更新药材失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private void ExecuteCancel()
        {
            _window.DialogResult = false;
            _window.Close();
        }
    }
}