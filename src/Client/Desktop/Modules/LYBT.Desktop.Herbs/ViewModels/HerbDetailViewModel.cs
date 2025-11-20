using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Herbs.Interfaces;
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
    /// 药材详情视图模型 - CRUD统一架构
    /// Issue #2168: 统一Create/Detail两种模式到单一ViewModel
    /// 注意：Herbs模块无Edit模式（中药基础数据创建后不允许修改）
    /// </summary>
    public class HerbDetailViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        private readonly IHerbRepository _herbRepository;

        #endregion

        #region 模式控制属性

    private Guid _herbId;
    private bool _isEditMode = true; // 默认为编辑模式

    /// <summary>
    /// 药材ID（空=Create模式，非空=Edit/View模式）
    /// </summary>
    public Guid HerbId
    {
        get => _herbId;
        set => SetProperty(ref _herbId, value);
    }

    /// <summary>
    /// 是否为编辑模式（false=View只读模式）
    /// </summary>
    public bool IsEditMode
    {
        get => _isEditMode;
        private set
        {
            if (SetProperty(ref _isEditMode, value))
            {
                RaisePropertyChanged(nameof(IsReadOnly));
                RaisePropertyChanged(nameof(IsNameEditable));
                SubmitCommand?.RaiseCanExecuteChanged();
                SwitchToEditModeCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// 是否只读模式
    /// </summary>
    public bool IsReadOnly => !IsEditMode;

    /// <summary>
    /// 是否为Create模式
    /// </summary>
    public bool IsCreateMode => HerbId == Guid.Empty;

    /// <summary>
    /// 是否为Edit或View模式
    /// </summary>
    public bool IsEditOrViewMode => HerbId != Guid.Empty;

    /// <summary>
    /// Name字段是否可编辑（仅Create模式可编辑）
    /// </summary>
    public bool IsNameEditable => IsCreateMode;

    #endregion

        #region 表单属性

        private string _name = string.Empty;
        private string _pinYinCode = string.Empty;
        private string? _origin;
        private string? _spec;
        private string _unit = "克";
        private decimal _price;
        private decimal? _costPrice;
        private string? _effect;
        private string? _usage;
        private string? _remark;
        private CommonStatus _status = CommonStatus.Enabled;

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
        [StringLength(10, ErrorMessage = "单位长度不能超过10个字符")]
        public string Unit
        {
            get => _unit;
            set
            {
                if (SetProperty(ref _unit, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 零售价
        /// </summary>
        [Range(0, 999999.99, ErrorMessage = "零售价必须在0-999999.99之间")]
        public decimal Price
        {
            get => _price;
            set
            {
                if (SetProperty(ref _price, value))
                {
                    ValidateProperty();
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
        /// 功效
        /// </summary>
        [StringLength(500, ErrorMessage = "功效长度不能超过500个字符")]
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
        /// 用法用量
        /// </summary>
        [StringLength(200, ErrorMessage = "用法用量长度不能超过200个字符")]
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

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark
        {
            get => _remark;
            set
            {
                if (SetProperty(ref _remark, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 状态（仅Detail模式显示）
        /// </summary>
        public CommonStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        #endregion

        #region 选项集合

        /// <summary>
        /// 状态选项
        /// </summary>
        public IEnumerable<CommonStatus> StatusOptions { get; }

        #endregion

        #region 命令

        /// <summary>
        /// 提交命令（Create）
        /// </summary>
        public DelegateCommand SubmitCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        /// <summary>
    /// 返回命令
    /// </summary>
    public DelegateCommand GoBackCommand { get; }

    /// <summary>
    /// 切换到编辑模式命令
    /// </summary>
    public DelegateCommand SwitchToEditModeCommand { get; }

    #endregion

        #region 构造函数

        public HerbDetailViewModel(
            IHerbRepository herbRepository,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));

            // 初始化选项
            StatusOptions = Enum.GetValues<CommonStatus>();

            // 初始化命令
        SubmitCommand = new DelegateCommand(async () => await SubmitAsync(), CanSubmit);
        CancelCommand = new DelegateCommand(Cancel);
        GoBackCommand = new DelegateCommand(() => NavigateBack("ContentRegion"));
        SwitchToEditModeCommand = new DelegateCommand(SwitchToEditMode, CanSwitchToEdit);
    }

        #endregion

        #region Navigation生命周期

        /// <summary>
        /// 处理导航参数（同步）
        /// </summary>
        protected override void ProcessNavigationParameters(NavigationParameters parameters)
        {
            base.ProcessNavigationParameters(parameters);

        // 提取HerbId参数
        if (parameters.ContainsKey("HerbId"))
        {
            HerbId = parameters.GetValue<Guid>("HerbId");
        }

        // 提取ReadOnly参数（默认为Edit模式）
        if (parameters.ContainsKey("ReadOnly"))
        {
            IsEditMode = !parameters.GetValue<bool>("ReadOnly");
        }
        else if (HerbId != Guid.Empty)
        {
            // 如果有HerbId但没有ReadOnly参数，默认为Edit模式
            IsEditMode = true;
        }
        }

        /// <summary>
        /// 异步初始化数据
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);

            if (HerbId != Guid.Empty)
            {
                // Detail模式：加载现有药材
                await LoadHerbAsync();
            }
            else
            {
                // Create模式：初始化空表单
                InitializeEmptyForm();
            }
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载药材数据（Detail模式）
        /// </summary>
        private async Task LoadHerbAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载药材信息...";

                Logger.LogInformation("开始加载药材数据: HerbId={HerbId}", HerbId);

                var herb = await _herbRepository.GetByIdAsync(HerbId);

                if (herb != null)
                {
                    // 填充表单字段
                    Name = herb.Name;
                    PinYinCode = herb.PinYinCode ?? PinYinHelper.GetPinYinCode(herb.Name);
                    Origin = herb.Origin;
                    Spec = herb.Spec;
                    Unit = herb.Unit;
                    Price = herb.Price;
                    CostPrice = herb.CostPrice;
                    Effect = herb.Effect;
                    Usage = herb.Usage;
                    Remark = herb.Remark;
                    Status = herb.Status;

                    // 更新页面标题（根据模式）
                PageTitle = IsEditMode ? $"编辑药材 - {Name}" : $"药材详情 - {Name}";

                    Logger.LogInformation("药材数据加载成功: Name={Name}", Name);
                }
                else
                {
                    Logger.LogWarning("未找到药材: HerbId={HerbId}", HerbId);
                    await ShowErrorMessageAsync("未找到药材信息");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载药材数据失败: HerbId={HerbId}", HerbId);
                await ShowErrorMessageAsync($"加载药材数据失败：{ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// 初始化空表单（Create模式）
        /// </summary>
        private void InitializeEmptyForm()
        {
            Name = string.Empty;
            PinYinCode = string.Empty;
            Origin = null;
            Spec = null;
            Unit = "克";
            Price = 0;
            CostPrice = null;
            Effect = null;
            Usage = null;
            Remark = null;
            Status = CommonStatus.Enabled;

            PageTitle = "创建药材";

            Logger.LogDebug("Create模式：空表单初始化完成");
        }

        #endregion

        #region 命令实现

        /// <summary>
    /// 提交表单（Create/Edit）
    /// </summary>
    private async Task SubmitAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = IsCreateMode ? "正在创建药材..." : "正在保存药材...";

            if (IsCreateMode)
            {
                await CreateHerbAsync();
            }
            else
            {
                await UpdateHerbAsync();
            }
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// 创建药材
        /// </summary>
        private async Task CreateHerbAsync()
        {
            try
            {
                var createDto = new HerbInputDto
                {
                    Name = Name.Trim(),
                    PinYinCode = PinYinCode?.Trim(),
                    Origin = Origin?.Trim(),
                    Spec = Spec?.Trim(),
                    Unit = Unit.Trim(),
                    Price = Price,
                    CostPrice = CostPrice,
                    Effect = Effect?.Trim(),
                    Usage = Usage?.Trim(),
                    Remark = Remark?.Trim(),
                    Status = CommonStatus.Enabled
                };

                Logger.LogInformation("开始创建药材: Name={Name}, Unit={Unit}, Price={Price}",
                    createDto.Name, createDto.Unit, createDto.Price);

                var result = await _herbRepository.CreateAsync(createDto);

                if (result != null)
                {
                    Logger.LogInformation("药材创建成功: HerbId={HerbId}, Name={Name}",
                        result.Id, result.Name);

                    // Issue #2166: 使用Navigation参数通知刷新，替代事件
                    NavigateBack("ContentRegion", new NavigationParameters
                    {
                        { "RefreshList", true }
                    });
                }
                else
                {
                    Logger.LogError("创建药材失败");
                    await ShowErrorMessageAsync("创建药材失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "创建药材异常: Name={Name}", Name);
                await ShowErrorMessageAsync($"创建药材失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 取消操作
        /// </summary>
    /// <summary>
    /// 更新药材
    /// </summary>
    private async Task UpdateHerbAsync()
    {
        try
        {
            var updateDto = new HerbInputDto
        {
            Id = HerbId,  // 更新时必填
            Name = Name.Trim(),  // Name字段虽然只读，但仍需传递
            PinYinCode = PinYinCode?.Trim(),
            Origin = Origin?.Trim(),
            Spec = Spec?.Trim(),
            Unit = Unit.Trim(),
            Price = Price,
            CostPrice = CostPrice,
            Effect = Effect?.Trim(),
            Usage = Usage?.Trim(),
            Remark = Remark?.Trim(),
            Status = Status
        };

        Logger.LogInformation("开始更新药材: HerbId={HerbId}, Name={Name}",
            HerbId, updateDto.Name);

        var result = await _herbRepository.UpdateAsync(updateDto);

            if (result != null)
            {
                Logger.LogInformation("药材更新成功: HerbId={HerbId}, Name={Name}",
                    result.Id, result.Name);

                // Issue #2166: 使用Navigation参数通知刷新，替代事件
                NavigateBack("ContentRegion", new NavigationParameters
                {
                    { "RefreshList", true }
                });
            }
            else
            {
                Logger.LogError("更新药材失败: HerbId={HerbId}", HerbId);
                await ShowErrorMessageAsync("更新药材失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "更新药材异常: HerbId={HerbId}, Name={Name}", HerbId, Name);
            await ShowErrorMessageAsync($"更新药材失败：{ex.Message}");
        }
    }

        private void Cancel()
        {
            Logger.LogDebug("用户取消操作");
            NavigateBack("ContentRegion");
        }

        /// <summary>
        /// 是否可以提交
        /// </summary>
        private bool CanSubmit()
    {
        // View模式（只读）不能提交
        if (IsReadOnly)
        {
            return false;
        }

        // Create和Edit模式都可以提交
        // 验证必填字段
        return !IsLoading &&
               !string.IsNullOrWhiteSpace(Name) &&
               !string.IsNullOrWhiteSpace(Unit) &&
               !HasErrors;
    }

    /// <summary>
    /// 切换到编辑模式
    /// </summary>
    private void SwitchToEditMode()
    {
        Logger.LogInformation("切换到编辑模式: HerbId={HerbId}", HerbId);
        IsEditMode = true;
        PageTitle = $"编辑药材 - {Name}";
    }

    /// <summary>
    /// 是否可以切换到编辑模式
    /// </summary>
    private bool CanSwitchToEdit()
    {
        // 只有在View模式下才能切换到Edit模式
        return IsEditOrViewMode && IsReadOnly && !IsLoading;
    }

    #endregion
    }
}
