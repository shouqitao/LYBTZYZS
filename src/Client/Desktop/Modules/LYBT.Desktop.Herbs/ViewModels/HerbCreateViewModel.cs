using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Herbs.Components;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Herbs.ViewModels
{
    /// <summary>
    /// 创建药材视图模型 - CRUD统一模式
    /// 功能：药材创建表单，采用Region Navigation模式
    /// </summary>
    public class HerbCreateViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        private readonly HerbDataManager _dataManager;

        #endregion

        #region 用户输入属性

        private string _name = string.Empty;
        private string _pinYinCode = string.Empty;
        private string? _origin;
        private string? _spec;
        private string _unit = "克";
        private decimal _price;
        private decimal? _costPrice;
        private string? _effect;
        private string? _usage;

        /// <summary>
        /// 药材名称
        /// </summary>
        [Required(ErrorMessage = "药材名称不能为空")]
        [StringLength(50, ErrorMessage = "药材名称长度不能超过50个字符")]
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    // 自动生成拼音码
                    PinYinCode = PinYinHelper.GetPinYinCode(value);

                    ValidateProperty();
                    SubmitCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 拼音码（自动生成）
        /// </summary>
        public string PinYinCode
        {
            get => _pinYinCode;
            private set => SetProperty(ref _pinYinCode, value);
        }

        /// <summary>
        /// 产地
        /// </summary>
        [StringLength(100, ErrorMessage = "产地长度不能超过100个字符")]
        public string? Origin
        {
            get => _origin;
            set
            {
                if (SetProperty(ref _origin, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 规格
        /// </summary>
        [StringLength(50, ErrorMessage = "规格长度不能超过50个字符")]
        public string? Spec
        {
            get => _spec;
            set
            {
                if (SetProperty(ref _spec, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 单位
        /// </summary>
        [Required(ErrorMessage = "单位不能为空")]
        [StringLength(20, ErrorMessage = "单位长度不能超过20个字符")]
        public string Unit
        {
            get => _unit;
            set
            {
                if (SetProperty(ref _unit, value))
                {
                    ValidateProperty();
                    SubmitCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 单价
        /// </summary>
        [Required(ErrorMessage = "单价不能为空")]
        [Range(0, 999999.99, ErrorMessage = "单价必须在0-999999.99之间")]
        public decimal Price
        {
            get => _price;
            set
            {
                if (SetProperty(ref _price, value))
                {
                    ValidateProperty();
                    SubmitCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 成本价
        /// </summary>
        [Range(0, 999999.99, ErrorMessage = "成本价必须在0-999999.99之间")]
        public decimal? CostPrice
        {
            get => _costPrice;
            set
            {
                if (SetProperty(ref _costPrice, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 功效说明
        /// </summary>
        [StringLength(1000, ErrorMessage = "功效说明长度不能超过1000个字符")]
        public string? Effect
        {
            get => _effect;
            set
            {
                if (SetProperty(ref _effect, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 用法
        /// </summary>
        [StringLength(500, ErrorMessage = "用法长度不能超过500个字符")]
        public string? Usage
        {
            get => _usage;
            set
            {
                if (SetProperty(ref _usage, value))
                {
                    ValidateProperty();
                }
            }
        }

        #endregion

        #region 命令

        /// <summary>
        /// 提交命令（创建）
        /// </summary>
        public DelegateCommand SubmitCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 构造函数

        public HerbCreateViewModel(
            HerbDataManager dataManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));

            PageTitle = "创建中药材";

            // 初始化命令
            SubmitCommand = new DelegateCommand(async () => await SubmitAsync(), CanSubmit);
            CancelCommand = new DelegateCommand(Cancel);
        }

        #endregion

        #region Navigation模式方法

        /// <summary>
        /// 处理导航参数（同步）
        /// </summary>
        protected override void ProcessNavigationParameters(NavigationParameters parameters)
        {
            base.ProcessNavigationParameters(parameters);
            // 创建模式无需处理参数
        }

        /// <summary>
        /// 异步初始化数据
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);

            // 初始化表单默认值
            Name = string.Empty;
            PinYinCode = string.Empty;
            Origin = null;
            Spec = null;
            Unit = "克";
            Price = 0;
            CostPrice = null;
            Effect = null;
            Usage = null;

            Logger.LogDebug("HerbCreateViewModel 初始化完成");
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 提交表单（创建药材）
        /// </summary>
        private async Task SubmitAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在创建中药材...";

                var createDto = new HerbInputDto
                {
                    Name = Name.Trim(),
                    PinYinCode = string.IsNullOrWhiteSpace(PinYinCode) ? null : PinYinCode.Trim(),
                    Origin = string.IsNullOrWhiteSpace(Origin) ? null : Origin.Trim(),
                    Spec = string.IsNullOrWhiteSpace(Spec) ? null : Spec.Trim(),
                    Unit = Unit.Trim(),
                    Price = Price,
                    CostPrice = CostPrice,
                    Effect = string.IsNullOrWhiteSpace(Effect) ? null : Effect.Trim(),
                    Usage = string.IsNullOrWhiteSpace(Usage) ? null : Usage.Trim(),
                    Status = CommonStatus.Enabled
                };

                Logger.LogInformation("开始创建中药材: Name={Name}, Unit={Unit}, Price={Price}",
                    createDto.Name, createDto.Unit, createDto.Price);

                var result = await _dataManager.CreateAsync(createDto);

                if (result != null)
                {
                    Logger.LogInformation("中药材创建成功: HerbId={HerbId}, Name={Name}",
                        result.Id, result.Name);

                    await ShowSuccessMessageAsync($"中药材 '{result.Name}' 创建成功");

                    // 导航返回
                    NavigateBack("ContentRegion");
                }
                else
                {
                    Logger.LogError("创建中药材失败");
                    await ShowErrorMessageAsync("创建中药材失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "创建中药材异常: Name={Name}", Name);
                await ShowErrorMessageAsync($"创建中药材失败：{ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// 取消操作
        /// </summary>
        private void Cancel()
        {
            Logger.LogDebug("用户取消创建操作");
            NavigateBack("ContentRegion");
        }

        /// <summary>
        /// 是否可以提交
        /// </summary>
        private bool CanSubmit()
        {
            return !IsLoading &&
                   !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(Unit) &&
                   !HasErrors;
        }

        #endregion
    }
}
