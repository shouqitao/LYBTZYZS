using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
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

        private readonly IHerbService _herbService;

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
            set => SetProperty(ref _herb, value);
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

        #endregion

        #region 构造函数

        public HerbDetailViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IHerbService herbService,
            ISessionManager? sessionManager = null,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, errorHandlingService)
        {
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));

            // 初始化选项
            StatusOptions = Enum.GetValues<CommonStatus>();

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await SaveHerbAsync(), CanSave);
            CancelCommand = new DelegateCommand(Cancel);

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
                    var createDto = new HerbCreateDto
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

                    var result = await _herbService.CreateAsync(createDto);
                    if (result.IsSuccess)
                    {
                        StatusMessage = "药材创建成功";
                        System.Windows.MessageBox.Show("药材创建成功", "成功", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        NavigateToHerbManagement();
                    }
                    else
                    {
                        ErrorMessage = $"创建药材失败: {result.ErrorMessage}";
                        System.Windows.MessageBox.Show(ErrorMessage, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
                else
                {
                    // 更新现有药材
                    var updateDto = new HerbUpdateDto
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

                    var result = await _herbService.UpdateAsync(Herb.Id, updateDto);
                    if (result.IsSuccess)
                    {
                        StatusMessage = "药材更新成功";
                        System.Windows.MessageBox.Show("药材更新成功", "成功", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        NavigateToHerbManagement();
                    }
                    else
                    {
                        ErrorMessage = $"更新药材失败: {result.ErrorMessage}";
                        System.Windows.MessageBox.Show(ErrorMessage, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
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

                var result = await _herbService.GetByIdAsync(herbId);
                if (result.IsSuccess && result.Data != null)
                {
                    Herb = result.Data;
                    LoadFromDto(result.Data);
                }
                else
                {
                    ErrorMessage = $"加载药材信息失败: {result.ErrorMessage}";
                    System.Windows.MessageBox.Show(ErrorMessage, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
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
            NavigateTo("MainRegion", "HerbManagementView");
        }

        #endregion
    }
}