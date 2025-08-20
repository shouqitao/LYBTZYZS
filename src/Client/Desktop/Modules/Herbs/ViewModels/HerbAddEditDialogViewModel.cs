using System.Windows;
using LYBT.Desktop.Herbs.Services;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.Desktop.Herbs.ViewModels
{
    /// <summary>
    /// 中药材新增/编辑对话框视图模型
    /// </summary>
    public class HerbAddEditDialogViewModel : BindableBase
    {
        private readonly HerbModuleService _herbApiService;
        private readonly HerbDto? _originalHerb;
        private bool _isEditMode;

        #region Properties

        private string _dialogTitle = "新增中药材";
        public string DialogTitle
        {
            get => _dialogTitle;
            set => SetProperty(ref _dialogTitle, value);
        }

        private string _herbName = string.Empty;
        public string HerbName
        {
            get => _herbName;
            set
            {
                if (SetProperty(ref _herbName, value))
                {
                    // 自动生成拼音码和五笔码（仅新增时）
                    if (!_isEditMode)
                    {
                        GenerateCodes();
                    }
                }
            }
        }

        private string _pinYinCode = string.Empty;
        public string PinYinCode
        {
            get => _pinYinCode;
            set => SetProperty(ref _pinYinCode, value);
        }


        private string _origin = string.Empty;
        public string Origin
        {
            get => _origin;
            set => SetProperty(ref _origin, value);
        }

        private string _spec = string.Empty;
        public string Spec
        {
            get => _spec;
            set => SetProperty(ref _spec, value);
        }

        private string _unit = "克";
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        private decimal _price = 0;
        public decimal Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        private string _effect = string.Empty;
        public string Effect
        {
            get => _effect;
            set => SetProperty(ref _effect, value);
        }

        private string _usage = string.Empty;
        public string Usage
        {
            get => _usage;
            set => SetProperty(ref _usage, value);
        }

        private string _remark = string.Empty;
        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        #endregion

        #region Commands

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region Callbacks

        /// <summary>
        /// 保存完成回调
        /// </summary>
        public Action<bool>? SaveCompleteCallback { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="herbApiService">中药材API服务</param>
        /// <param name="herb">要编辑的药材信息（null表示新增模式）</param>
        public HerbAddEditDialogViewModel(HerbModuleService herbApiService, HerbDto? herb = null)
        {
            _herbApiService = herbApiService ?? throw new ArgumentNullException(nameof(herbApiService));
            _originalHerb = herb;
            _isEditMode = herb != null;

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await ExecuteSaveAsync(), CanExecuteSave)
                .ObservesProperty(() => HerbName)
                .ObservesProperty(() => Unit)
                .ObservesProperty(() => Price);
            
            CancelCommand = new DelegateCommand(ExecuteCancel);

            // 如果是编辑模式，初始化数据
            if (_isEditMode && herb != null)
            {
                InitializeEditData(herb);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 初始化编辑数据
        /// </summary>
        private void InitializeEditData(HerbDto herb)
        {
            DialogTitle = "编辑中药材";
            HerbName = herb.Name;
            PinYinCode = herb.PinYinCode ?? string.Empty;
            Origin = herb.Origin ?? string.Empty;
            Spec = herb.Spec ?? string.Empty;
            Unit = herb.Unit ?? "克";
            Price = herb.Price;
            Effect = herb.Effect ?? string.Empty;
            Usage = herb.Usage ?? string.Empty;
            Remark = herb.Remark ?? string.Empty;
        }

        /// <summary>
        /// 自动生成拼音码和五笔码
        /// </summary>
        private void GenerateCodes()
        {
            if (!string.IsNullOrWhiteSpace(HerbName))
            {
                PinYinCode = CommonHelper.GetPinyinCode(HerbName);
            }
            else
            {
                PinYinCode = string.Empty;
            }
        }

        /// <summary>
        /// 判断是否可以保存
        /// </summary>
        private bool CanExecuteSave()
        {
            return !string.IsNullOrWhiteSpace(HerbName) &&
                   !string.IsNullOrWhiteSpace(Unit) &&
                   Price > 0;
        }

        /// <summary>
        /// 执行保存
        /// </summary>
        private async Task ExecuteSaveAsync()
        {
            try
            {
                bool result;

                if (_isEditMode && _originalHerb != null)
                {
                    // 编辑模式
                    var updateDto = new HerbUpdateDto
                    {
                        Id = _originalHerb.Id,
                        Name = HerbName.Trim(),
                        PinYinCode = PinYinCode,
                        Origin = Origin?.Trim(),
                        Spec = Spec?.Trim(),
                        Unit = Unit,
                        Price = Price,
                        Effect = Effect?.Trim(),
                        Usage = Usage?.Trim(),
                        Remark = Remark?.Trim()
                    };

                    var response = await _herbApiService.UpdateAsync(updateDto);
                    result = response.IsSuccess;
                    
                    if (!result)
                    {
                        MessageBox.Show($"编辑中药材失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // 新增模式
                    var createDto = new HerbCreateDto
                    {
                        Name = HerbName.Trim(),
                        PinYinCode = PinYinCode,
                        Origin = Origin?.Trim(),
                        Spec = Spec?.Trim(),
                        Unit = Unit,
                        Price = Price,
                        Effect = Effect?.Trim(),
                        Usage = Usage?.Trim(),
                        Remark = Remark?.Trim(),
                        Status = CommonStatus.Enabled
                    };

                    var response = await _herbApiService.CreateAsync(createDto);
                    result = response.IsSuccess;
                    
                    if (!result)
                    {
                        MessageBox.Show($"新增中药材失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                // 调用回调
                SaveCompleteCallback?.Invoke(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存中药材时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                SaveCompleteCallback?.Invoke(false);
            }
        }

        /// <summary>
        /// 执行取消
        /// </summary>
        private void ExecuteCancel()
        {
            SaveCompleteCallback?.Invoke(false);
        }

        #endregion
    }
}