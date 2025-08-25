using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Herbs.Services;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using Prism.Commands;
using Prism.Events;

namespace LYBT.Desktop.Herbs.ViewModels
{
    /// <summary>
    /// 中药材新增/编辑对话框视图模型
    /// </summary>
    public class HerbAddEditDialogViewModel : DialogViewModel, ICustomDialogAware
    {
        private readonly HerbModule _herbApiService;
        private readonly HerbDto? _originalHerb;
        private bool _isEditMode;

        #region Properties

        private string _herbName = string.Empty;
        public string HerbName
        {
            get => _herbName;
            set
            {
                if (SetProperty(ref _herbName, value))
                {
                    // 自动生成拼音码（仅新增时）
                    if (!_isEditMode)
                    {
                        GenerateCodes();
                    }
                    SaveCommand.RaiseCanExecuteChanged();
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

        private string _unit = SystemConstants.DefaultHerbUnit;
        public string Unit
        {
            get => _unit;
            set
            {
                if (SetProperty(ref _unit, value))
                {
                    SaveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private decimal _price = 0;
        public decimal Price
        {
            get => _price;
            set
            {
                if (SetProperty(ref _price, value))
                {
                    SaveCommand.RaiseCanExecuteChanged();
                }
            }
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

        #region Constructor

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="herbApiService">中药材API服务</param>
        /// <param name="eventAggregator">事件聚合器</param>
        /// <param name="errorHandlingService">错误处理服务</param>
        /// <param name="herb">要编辑的药材信息（null表示新增模式）</param>
        public HerbAddEditDialogViewModel(
            HerbModule herbApiService, 
            IEventAggregator eventAggregator,
            IErrorHandlingService errorHandlingService,
            HerbDto? herb = null)
            : base(eventAggregator, errorHandlingService)
        {
            _herbApiService = herbApiService ?? throw new ArgumentNullException(nameof(herbApiService));
            _originalHerb = herb;
            _isEditMode = herb != null;

            // 如果是编辑模式，初始化数据
            if (_isEditMode && herb != null)
            {
                InitializeEditData(herb);
            }
            else
            {
                DialogTitle = SystemConstants.AddHerbDialogTitle;
            }

            InitializeDialog();
        }

        /// <summary>
        /// 兼容性构造函数
        /// </summary>
        public HerbAddEditDialogViewModel(
            HerbModule herbApiService,
            IEventAggregator eventAggregator,
            HerbDto? herb = null)
            : base(eventAggregator)
        {
            _herbApiService = herbApiService ?? throw new ArgumentNullException(nameof(herbApiService));
            _originalHerb = herb;
            _isEditMode = herb != null;

            // 如果是编辑模式，初始化数据
            if (_isEditMode && herb != null)
            {
                InitializeEditData(herb);
            }
            else
            {
                DialogTitle = SystemConstants.AddHerbDialogTitle;
            }

            InitializeDialog();
        }

        #endregion

        #region DialogViewModel Implementation

        protected override async Task<bool> SaveAsync()
        {
            try
            {
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

                    var response = await _herbApiService.UpdateAsync(_originalHerb.Id, updateDto);
                    
                    if (!response.IsSuccess)
                    {
                        ErrorMessage = response.ErrorMessage ?? "编辑中药材失败";
                        return false;
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
                    
                    if (!response.IsSuccess)
                    {
                        ErrorMessage = response.ErrorMessage ?? "新增中药材失败";
                        return false;
                    }
                }

                // 保存成功，关闭对话框
                RaiseRequestClose(true);
                return true;
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("保存中药材", ex);
                return false;
            }
        }

        protected override bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(HerbName) &&
                   !string.IsNullOrWhiteSpace(Unit) &&
                   Price > 0;
        }

        protected override void InitializeDialog()
        {
            base.InitializeDialog();
            
            // 监听属性变化以更新Command状态  
            SaveCommand.ObservesProperty(() => HerbName);
            SaveCommand.ObservesProperty(() => Unit);
            SaveCommand.ObservesProperty(() => Price);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 初始化编辑数据
        /// </summary>
        private void InitializeEditData(HerbDto herb)
        {
            DialogTitle = SystemConstants.EditHerbDialogTitle;
            HerbName = herb.Name;
            PinYinCode = herb.PinYinCode ?? string.Empty;
            Origin = herb.Origin ?? string.Empty;
            Spec = herb.Spec ?? string.Empty;
            Unit = herb.Unit ?? SystemConstants.DefaultHerbUnit;
            Price = herb.Price;
            Effect = herb.Effect ?? string.Empty;
            Usage = herb.Usage ?? string.Empty;
            Remark = herb.Remark ?? string.Empty;
        }

        /// <summary>
        /// 自动生成拼音码
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

        #endregion

        #region ICustomDialogAware Implementation

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title => DialogTitle ?? (_isEditMode ? "编辑中药材" : "新增中药材");

        /// <summary>
        /// 请求关闭对话框事件
        /// </summary>
        public event Action<CustomDialogResult> RequestClose = delegate { };

        /// <summary>
        /// 检查是否可以关闭对话框
        /// </summary>
        public bool CanCloseDialog()
        {
            return !IsSaving && !IsLoading;
        }

        /// <summary>
        /// 对话框打开时调用
        /// </summary>
        /// <param name="parameters">传入的参数</param>
        public void OnDialogOpened(Dictionary<string, object> parameters)
        {
            if (parameters?.ContainsKey("IsEditMode") == true && parameters["IsEditMode"] is bool isEditMode)
            {
                _isEditMode = isEditMode;
            }

            if (parameters?.ContainsKey("Herb") == true && parameters["Herb"] is HerbDto herb)
            {
                InitializeEditData(herb);
            }

            DialogTitle = _isEditMode ? "编辑中药材" : "新增中药材";
        }

        /// <summary>
        /// 对话框关闭时调用
        /// </summary>
        public void OnDialogClosed()
        {
            // 清理资源或执行其他关闭操作
        }

        /// <summary>
        /// 重写取消操作以使用ICustomDialogAware接口
        /// </summary>
        protected override void ExecuteCancel()
        {
            OnDialogClosing();
            RaiseRequestClose(false);
        }

        /// <summary>
        /// 触发关闭对话框请求
        /// </summary>
        protected void RaiseRequestClose(bool? dialogResult)
        {
            var result = dialogResult == true 
                ? CustomDialogResult.Success(new Dictionary<string, object>())
                : CustomDialogResult.Cancel();
                
            RequestClose?.Invoke(result);
        }

        #endregion
    }
}