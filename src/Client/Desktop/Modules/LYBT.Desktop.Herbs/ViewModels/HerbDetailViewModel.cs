using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Herbs.Components; // Epic #1773: 添加Component命名空间
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
// Epic #1773: 已移除LYBT.Desktop.Herbs.Interfaces using（不再需要IHerbRepository）
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
    /// 药材详情视图模型 - UltraThink架构重构版本
    /// 基于简化架构实现药材查看和编辑功能
    /// </summary>
    public class HerbDetailViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        // Epic #1773: 使用DataManager替代Repository依赖
        private readonly HerbDataManager _dataManager;
        // Issue #2147: 注入ICommonDialogService，替代MessageBox.Show直接调用
        private readonly ICommonDialogService _dialogService;

        #endregion

        #region 药材属性

        private HerbDto? _herb;
        private string _name = string.Empty;
        private string? _pinYinCode;
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
        /// 当前药材信息
        /// </summary>
        public HerbDto? Herb
        {
            get => _herb;
            set
            {
                if (SetProperty(ref _herb, value))
                {
                    // 当Herb对象改变时，通知审计字段属性变更
                    RaisePropertyChanged(nameof(CreatedAt));
                    RaisePropertyChanged(nameof(UpdatedAt));
                }
            }
        }

        /// <summary>
        /// 药材名称
        /// </summary>
        [Required(ErrorMessage = "药材名称不能为空")]
        [StringLength(100, ErrorMessage = "药材名称长度不能超过100个字符")]
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    ValidateProperty();
                    // 自动更新拼音码（仅当名称发生变化时）
                    PinYinCode = PinYinHelper.GetPinYinCode(value);
                }
            }
        }

        /// <summary>
        /// 拼音码
        /// </summary>
        [StringLength(50, ErrorMessage = "拼音码长度不能超过50个字符")]
        public string? PinYinCode
        {
            get => _pinYinCode;
            set
            {
                if (SetProperty(ref _pinYinCode, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 产地
        /// </summary>
        [StringLength(100, ErrorMessage = "产地长度不能超过100个字符")]
        public string? Origin
        {
            get => _origin;
            set => SetProperty(ref _origin, value);
        }

        /// <summary>
        /// 规格
        /// </summary>
        [StringLength(50, ErrorMessage = "规格长度不能超过50个字符")]
        public string? Spec
        {
            get => _spec;
            set => SetProperty(ref _spec, value);
        }

        /// <summary>
        /// 单位
        /// </summary>
        [Required(ErrorMessage = "单位不能为空")]
        [StringLength(20, ErrorMessage = "单位长度不能超过20个字符")]
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        /// <summary>
        /// 单价
        /// </summary>
        [Required(ErrorMessage = "单价不能为空")]
        [Range(0, 999999.99, ErrorMessage = "单价必须在0-999999.99之间")]
        public decimal Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        /// <summary>
        /// 成本价
        /// </summary>
        [Range(0, 999999.99, ErrorMessage = "成本价必须在0-999999.99之间")]
        public decimal? CostPrice
        {
            get => _costPrice;
            set => SetProperty(ref _costPrice, value);
        }

        /// <summary>
        /// 功效说明
        /// </summary>
        [StringLength(1000, ErrorMessage = "功效说明长度不能超过1000个字符")]
        public string? Effect
        {
            get => _effect;
            set => SetProperty(ref _effect, value);
        }

        /// <summary>
        /// 用法
        /// </summary>
        [StringLength(500, ErrorMessage = "用法长度不能超过500个字符")]
        public string? Usage
        {
            get => _usage;
            set => SetProperty(ref _usage, value);
        }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        /// <summary>
        /// 状态
        /// </summary>
        public CommonStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? CreatedAt => Herb?.CreatedAt;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdatedAt => Herb?.UpdatedAt;

        /// <summary>
        /// 是否只读模式
        /// </summary>
        private bool _isReadOnly;
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set
            {
                if (SetProperty(ref _isReadOnly, value))
                {
                    // 只读模式改变时，刷新命令状态
                    SaveCommand?.RaiseCanExecuteChanged();
                    EditCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        #endregion

        #region 选项集合

        /// <summary>
        /// 状态选项
        /// </summary>
        public CommonStatus[] StatusOptions { get; }

        #endregion

        #region 命令

        /// <summary>
        /// 保存命令
        /// </summary>
        public DelegateCommand SaveCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// 返回命令
        /// </summary>
        public DelegateCommand BackCommand { get; }

        /// <summary>
        /// 编辑命令
        /// </summary>
        public DelegateCommand EditCommand { get; }

        /// <summary>
        /// 取消编辑命令
        /// </summary>
        public DelegateCommand CancelEditCommand { get; }



        #endregion

        #region 构造函数

        public HerbDetailViewModel(
            HerbDataManager dataManager, // Epic #1773: 注入DataManager
            ICommonDialogService dialogService, // Issue #2147: 注入ICommonDialogService
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            // Epic #1773: 注入DataManager
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            // Issue #2147: 注入ICommonDialogService
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // 初始化选项
            StatusOptions = Enum.GetValues<CommonStatus>();

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await SaveHerbAsync(), CanSave);
            CancelCommand = new DelegateCommand(Cancel);
            BackCommand = new DelegateCommand(NavigateToHerbManagement);
            EditCommand = new DelegateCommand(EnableEdit, CanEdit);
            CancelEditCommand = new DelegateCommand(Cancel);

            // 属性变更时刷新命令状态
            PropertyChanged += (s, e) => SaveCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 保存药材
        /// </summary>
        private async Task SaveHerbAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "正在保存药材信息...";

                if (Herb == null)
                {
                    // 创建新药材
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
                        Status = Status
                    };

                    // Epic #1773: 使用DataManager包装Repository方法
                    var createdHerb = await _dataManager.CreateAsync(createDto);
                    StatusMessage = "药材创建成功";
                    // Issue #2147: 替换MessageBox.Show为ICommonDialogService
                    await _dialogService.ShowInfoAsync("药材创建成功", "成功");
                    NavigateToHerbManagement();
                }
                else
                {
                    // 更新现有药材
                    var updateDto = new HerbInputDto
                    {
                        Id = Herb.Id,
                        Name = Name.Trim(),
                        PinYinCode = string.IsNullOrWhiteSpace(PinYinCode) ? null : PinYinCode.Trim(),
                        Origin = string.IsNullOrWhiteSpace(Origin) ? null : Origin.Trim(),
                        Spec = string.IsNullOrWhiteSpace(Spec) ? null : Spec.Trim(),
                        Unit = Unit.Trim(),
                        Price = Price,
                        CostPrice = CostPrice,
                        Effect = string.IsNullOrWhiteSpace(Effect) ? null : Effect.Trim(),
                        Usage = string.IsNullOrWhiteSpace(Usage) ? null : Usage.Trim(),
                        Status = Status
                    };

                    // Epic #1773: 使用DataManager包装Repository方法
                    var updatedHerb = await _dataManager.UpdateAsync(updateDto);
                    StatusMessage = "药材更新成功";
                    // Issue #2147: 替换MessageBox.Show为ICommonDialogService
                    await _dialogService.ShowInfoAsync("药材更新成功", "成功");
                    NavigateToHerbManagement();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存药材信息时发生异常");
                HandleError(ex, "保存药材");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 检查是否可以保存
        /// </summary>
        private bool CanSave()
        {
            return !IsBusy &&
                   !IsReadOnly &&
                   !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(Unit) &&
                   !HasErrors;
        }

        /// <summary>
        /// 取消操作
        /// </summary>
        private void Cancel()
        {
            NavigateToHerbManagement();
        }

        /// <summary>
        /// 启用编辑模式
        /// </summary>
        private void EnableEdit()
        {
            // TODO: 实现编辑模式逻辑
            Logger.LogInformation("启用编辑模式");
        }

        /// <summary>
        /// 检查是否可以编辑
        /// </summary>
        private bool CanEdit()
        {
            return !IsBusy && Herb != null;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 加载药材信息
        /// </summary>
        public async Task LoadHerbAsync(Guid herbId)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "正在加载药材信息...";

                // Epic #1773: 使用DataManager包装Repository方法
                var herb = await _dataManager.GetByIdAsync(herbId);
                if (herb == null)
                {
                    StatusMessage = "未找到药材信息";
                    return;
                }
                Herb = herb;
                LoadFromDto(herb);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载药材信息时发生异常：{HerbId}", herbId);
                HandleError(ex, "加载药材信息");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 从DTO加载数据
        /// </summary>
        public void LoadFromDto(HerbDto dto)
        {
            if (dto == null) return;

            Name = dto.Name ?? string.Empty;
            PinYinCode = dto.PinYinCode;
            Origin = dto.Origin;
            Spec = dto.Spec;
            Unit = dto.Unit ?? "克";
            Price = dto.Price;
            CostPrice = dto.CostPrice;
            Effect = dto.Effect;
            Usage = dto.Usage;
            Remark = dto.Remark;
            Status = dto.Status;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 导航到药材管理页面
        /// </summary>
        private void NavigateToHerbManagement()
        {
            NavigateTo("ContentRegion", "HerbManagementView");
        }

        #endregion

        #region 导航参数处理

        /// <summary>
        /// 处理导航参数（同步）
        /// </summary>
        protected override void ProcessNavigationParameters(NavigationParameters parameters)
        {
            base.ProcessNavigationParameters(parameters);

            // 检查是否是只读模式
            if (parameters.TryGetValue("ReadOnly", out bool readOnly))
            {
                IsReadOnly = readOnly;
            }
            else
            {
                IsReadOnly = false; // 默认编辑模式
            }
        }

        /// <summary>
        /// 异步初始化（加载数据）
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);

            // 检查导航参数
            if (parameters.TryGetValue("HerbId", out Guid herbId))
            {
                // 编辑/查看模式：加载现有数据
                await LoadHerbAsync(herbId);
            }
            else if (parameters.TryGetValue("SourceHerbId", out Guid sourceHerbId))
            {
                // 复制模式：加载源数据并清空ID
                await LoadHerbAsync(sourceHerbId);
                Herb = null; // 清空ID，标记为新建
            }
            else
            {
                // 新建模式：使用默认值
                Herb = null;
                Name = string.Empty;
                PinYinCode = null;
                Origin = null;
                Spec = null;
                Unit = "克";
                Price = 0;
                CostPrice = null;
                Effect = null;
                Usage = null;
                Remark = null;
                Status = CommonStatus.Enabled;
            }
        }

        #endregion
    }
}
