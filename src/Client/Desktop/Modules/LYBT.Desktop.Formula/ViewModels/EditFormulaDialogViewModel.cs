using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 配方编辑对话框视图模型 - UltraThink简化版本
    /// 基于UnifiedViewModelBase实现配方编辑功能
    /// </summary>
    public class EditFormulaDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        #region 服务依赖

        private readonly IFormulaService _formulaService;

        #endregion

        #region 配方属性

        private Guid? _formulaId;
        private string _formulaName = string.Empty;
        private string _description = string.Empty;
        private CommonStatus _status = CommonStatus.Enabled;

        /// <summary>
        /// 配方ID
        /// </summary>
        public Guid? FormulaId
        {
            get => _formulaId;
            set => SetProperty(ref _formulaId, value);
        }

        /// <summary>
        /// 配方名称
        /// </summary>
        [Required(ErrorMessage = "配方名称不能为空")]
        [StringLength(100, ErrorMessage = "配方名称长度不能超过100个字符")]
        public string FormulaName
        {
            get => _formulaName;
            set
            {
                if (SetProperty(ref _formulaName, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 配方描述
        /// </summary>
        [StringLength(500, ErrorMessage = "配方描述长度不能超过500个字符")]
        public string Description
        {
            get => _description;
            set
            {
                if (SetProperty(ref _description, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 配方状态
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

        /// <summary>
        /// 添加药材命令 - Phase 4B 骨架
        /// </summary>
        public DelegateCommand AddHerbCommand { get; }

        /// <summary>
        /// 编辑药材命令 - Phase 4B 骨架
        /// </summary>
        public DelegateCommand EditHerbCommand { get; }

        /// <summary>
        /// 移除药材命令 - Phase 4B 骨架
        /// </summary>
        public DelegateCommand RemoveHerbCommand { get; }

        #endregion

        #region 构造函数

        public EditFormulaDialogViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IFormulaService formulaService,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));

            // 初始化选项
            StatusOptions = Enum.GetValues<CommonStatus>();

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await SaveFormulaAsync(), CanSave);
            CancelCommand = new DelegateCommand(Cancel);

            // Phase 4B 骨架命令
            AddHerbCommand = new DelegateCommand(() => Logger.LogInformation("EditFormulaDialog - 添加药材命令（骨架实现）"));
            EditHerbCommand = new DelegateCommand(() => Logger.LogInformation("EditFormulaDialog - 编辑药材命令（骨架实现）"));
            RemoveHerbCommand = new DelegateCommand(() => Logger.LogInformation("EditFormulaDialog - 移除药材命令（骨架实现）"));

            // 属性变更时刷新命令状态
            PropertyChanged += (s, e) => SaveCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 保存配方
        /// </summary>
        private async Task SaveFormulaAsync()
        {
            try
            {
                SetIsBusy(true, "正在保存配方...");

                if (FormulaId.HasValue)
                {
                    // 更新现有配方
                    var updateDto = new FormulaUpdateDto
                    {
                        Name = FormulaName.Trim(),
                        Remark = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim()
                    };

                    var result = await _formulaService.UpdateAsync(FormulaId.Value, updateDto);
                    if (result.IsSuccess)
                    {
                        var parameters = new DialogParameters
                        {
                            { "Formula", result.Data }
                        };
                        RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
                        Logger.LogInformation("配方更新成功: {FormulaId}", FormulaId);
                    }
                    else
                    {
                        await ShowErrorMessageAsync($"更新配方失败: {result.ErrorMessage}");
                    }
                }
                else
                {
                    // 创建新配方
                    var createDto = new FormulaCreateDto
                    {
                        Name = FormulaName.Trim(),
                        Remark = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim()
                    };

                    var result = await _formulaService.CreateAsync(createDto);
                    if (result.IsSuccess)
                    {
                        var parameters = new DialogParameters
                        {
                            { "Formula", result.Data }
                        };
                        RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
                        Logger.LogInformation("配方创建成功");
                    }
                    else
                    {
                        await ShowErrorMessageAsync($"创建配方失败: {result.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存配方时发生异常");
                await ShowErrorMessageAsync("保存配方时发生系统错误，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 检查是否可以保存
        /// </summary>
        private bool CanSave()
        {
            return !IsBusy &&
                   !string.IsNullOrWhiteSpace(FormulaName) &&
                   !HasErrors;
        }

        /// <summary>
        /// 取消操作
        /// </summary>
        private void Cancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        #endregion

        #region IDialogAware 实现

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title => FormulaId.HasValue ? "编辑验方模板" : "新建验方模板";

        /// <summary>
        /// 对话框关闭事件
        /// </summary>
        public event Action<IDialogResult>? RequestClose;

        /// <summary>
        /// 是否可以关闭对话框
        /// </summary>
        public bool CanCloseDialog() => true;

        /// <summary>
        /// 对话框关闭时调用
        /// </summary>
        public void OnDialogClosed() { }

        /// <summary>
        /// 对话框打开时调用
        /// </summary>
        public void OnDialogOpened(IDialogParameters parameters)
        {
            try
            {
                // 从参数中获取配方ID
                if (parameters.TryGetValue("FormulaId", out Guid formulaId))
                {
                    _ = InitializeAsync(formulaId);
                }
                else
                {
                    _ = InitializeAsync(null);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开对话框时发生异常");
            }
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化编辑配方数据
        /// </summary>
        public async Task InitializeAsync(Guid? formulaId = null)
        {
            try
            {
                FormulaId = formulaId;

                if (formulaId.HasValue)
                {
                    SetIsBusy(true, "正在加载配方信息...");

                    var result = await _formulaService.GetByIdAsync(formulaId.Value);
                    if (result.IsSuccess && result.Data != null)
                    {
                        var formula = result.Data;
                        FormulaName = formula.Name ?? string.Empty;
                        Description = formula.Remark ?? string.Empty;
                        Status = formula.Status;
                    }
                    else
                    {
                        await ShowErrorMessageAsync("加载配方信息失败");
                    }
                }
                else
                {
                    // 新建配方，重置为默认值
                    FormulaName = string.Empty;
                    Description = string.Empty;
                    Status = CommonStatus.Enabled;
                }

                // 清除验证错误
                ClearAllErrors();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "初始化配方编辑数据时发生异常");
                await ShowErrorMessageAsync("初始化配方数据时发生系统错误");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion
    }
}
