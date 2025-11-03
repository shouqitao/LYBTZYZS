using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Modules.Prescriptions.ViewModels.Components; // Issue #1786: 添加Component命名空间
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels
{
    /// <summary>
    /// 处方编辑对话框视图模型 - UltraThink精简架构
    /// 提供处方信息的快速编辑功能
    /// </summary>
    public class PrescriptionEditorDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        #region 服务依赖

        // Issue #1786: 使用DataManager替代直接Api和Repository访问
        private readonly PrescriptionDataManager _dataManager;

        #endregion

        #region 数据属性

        private Guid _prescriptionId;
        private PrescriptionDto? _originalPrescription;
        private string _prescriptionNo = string.Empty;
        private int _dosageCount = 7;
        private string _usage = "水煎服，一日三次，饭后服用";
        private string _medicalAdvice = string.Empty;
        private string _remark = string.Empty;
        private decimal _discount = 1.0m;
        private decimal _totalAmount;

        /// <summary>
        /// 处方ID
        /// </summary>
        public Guid PrescriptionId
        {
            get => _prescriptionId;
            set => SetProperty(ref _prescriptionId, value);
        }

        /// <summary>
        /// 原始处方数据
        /// </summary>
        public PrescriptionDto? OriginalPrescription
        {
            get => _originalPrescription;
            set => SetProperty(ref _originalPrescription, value);
        }

        /// <summary>
        /// 处方编号
        /// </summary>
        [Required(ErrorMessage = "处方编号不能为空")]
        [StringLength(50, ErrorMessage = "处方编号长度不能超过50个字符")]
        public string PrescriptionNo
        {
            get => _prescriptionNo;
            set
            {
                if (SetProperty(ref _prescriptionNo, value))
                {
                    ValidateProperty();
                    MarkAsChanged();
                }
            }
        }

        /// <summary>
        /// 剂数
        /// </summary>
        [Required(ErrorMessage = "剂数不能为空")]
        [Range(1, 100, ErrorMessage = "剂数必须在1-100之间")]
        public int DosageCount
        {
            get => _dosageCount;
            set
            {
                if (SetProperty(ref _dosageCount, value))
                {
                    ValidateProperty();
                    MarkAsChanged();
                }
            }
        }

        /// <summary>
        /// 用法
        /// </summary>
        [Required(ErrorMessage = "用法不能为空")]
        [StringLength(200, ErrorMessage = "用法长度不能超过200个字符")]
        public string Usage
        {
            get => _usage;
            set
            {
                if (SetProperty(ref _usage, value))
                {
                    ValidateProperty();
                    MarkAsChanged();
                }
            }
        }

        /// <summary>
        /// 医嘱
        /// </summary>
        [StringLength(500, ErrorMessage = "医嘱长度不能超过500个字符")]
        public string MedicalAdvice
        {
            get => _medicalAdvice;
            set
            {
                if (SetProperty(ref _medicalAdvice, value))
                {
                    ValidateProperty();
                    MarkAsChanged();
                }
            }
        }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string Remark
        {
            get => _remark;
            set
            {
                if (SetProperty(ref _remark, value))
                {
                    ValidateProperty();
                    MarkAsChanged();
                }
            }
        }

        /// <summary>
        /// 折扣
        /// </summary>
        [Required(ErrorMessage = "折扣不能为空")]
        [Range(0.1, 1.0, ErrorMessage = "折扣必须在0.1-1.0之间")]
        public decimal Discount
        {
            get => _discount;
            set
            {
                if (SetProperty(ref _discount, value))
                {
                    ValidateProperty();
                    MarkAsChanged();
                }
            }
        }

        /// <summary>
        /// 总金额
        /// </summary>
        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        #endregion

        #region 状态属性

        private bool _hasChanges;
        private bool _isValidationEnabled = true;
        private bool _isReadOnly;
        private string _readOnlyReason = string.Empty;

        /// <summary>
        /// 是否有更改
        /// </summary>
        public bool HasChanges
        {
            get => _hasChanges;
            set => SetProperty(ref _hasChanges, value);
        }

        /// <summary>
        /// 是否启用验证
        /// </summary>
        public bool IsValidationEnabled
        {
            get => _isValidationEnabled;
            set => SetProperty(ref _isValidationEnabled, value);
        }

        /// <summary>
        /// 是否为只读模式（Issue #1423 RULE-4）
        /// </summary>
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set
            {
                if (SetProperty(ref _isReadOnly, value))
                {
                    UpdateCommandStates();
                    RaisePropertyChanged(nameof(CanEdit));
                }
            }
        }

        /// <summary>
        /// 只读原因说明（Issue #1423 RULE-4）
        /// </summary>
        public string ReadOnlyReason
        {
            get => _readOnlyReason;
            set => SetProperty(ref _readOnlyReason, value);
        }

        /// <summary>
        /// 是否可以编辑（Issue #1423 RULE-4）
        /// </summary>
        public bool CanEdit => !IsReadOnly && !IsBusy;

        /// <summary>
        /// 变更信息
        /// </summary>
        public string ChangeInfo => IsReadOnly ? ReadOnlyReason : (HasChanges ? "有未保存的更改" : "无更改");

        #endregion

        #region 对话框属性

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title { get; set; } = "编辑处方信息";

        /// <summary>
        /// 对话框关闭事件
        /// </summary>
        public event Action<IDialogResult>? RequestClose;

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
        /// 重置命令
        /// </summary>
        public DelegateCommand ResetCommand { get; }

        /// <summary>
        /// 验证命令
        /// </summary>
        public DelegateCommand ValidateCommand { get; }

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

        /// <summary>
        /// 加载验方模板命令 - Phase 4B 骨架
        /// </summary>
        public DelegateCommand LoadFormulaTemplateCommand { get; }

        /// <summary>
        /// 预览命令 - Phase 4B 骨架
        /// </summary>
        public DelegateCommand PreviewCommand { get; }

        #endregion

        #region 构造函数

        public PrescriptionEditorDialogViewModel(
            PrescriptionDataManager dataManager, // Issue #1786: 注入DataManager
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            // Issue #1786: 注入DataManager
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave);
            CancelCommand = new DelegateCommand(Cancel);
            ResetCommand = new DelegateCommand(Reset, CanReset);
            ValidateCommand = new DelegateCommand(ValidateAllWrapper);

            // Phase 4B 骨架命令
            AddHerbCommand = new DelegateCommand(() => Logger.LogInformation("PrescriptionEditorDialog - 添加药材命令（骨架实现）"));
            EditHerbCommand = new DelegateCommand(() => Logger.LogInformation("PrescriptionEditorDialog - 编辑药材命令（骨架实现）"));
            RemoveHerbCommand = new DelegateCommand(() => Logger.LogInformation("PrescriptionEditorDialog - 移除药材命令（骨架实现）"));
            LoadFormulaTemplateCommand = new DelegateCommand(() => Logger.LogInformation("PrescriptionEditorDialog - 加载验方模板命令（骨架实现）"));
            PreviewCommand = new DelegateCommand(() => Logger.LogInformation("PrescriptionEditorDialog - 预览命令（骨架实现）"));

            // 属性变更时刷新命令状态
            PropertyChanged += (s, e) =>
            {
                UpdateCommandStates();
                if (e.PropertyName == nameof(HasChanges))
                {
                    RaisePropertyChanged(nameof(CanSave));
                }
            };
        }

        #endregion

        #region IDialogAware 实现

        /// <summary>
        /// 是否可以关闭对话框
        /// </summary>
        public bool CanCloseDialog()
        {
            if (HasChanges)
            {
                // 如果有未保存的更改，询问用户
                return ShowConfirmMessage("有未保存的更改，确定要关闭吗？");
            }
            return true;
        }

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
                // 获取参数
                if (parameters.ContainsKey("Title"))
                {
                    Title = parameters.GetValue<string>("Title");
                }

                if (parameters.ContainsKey("PrescriptionId"))
                {
                    PrescriptionId = parameters.GetValue<Guid>("PrescriptionId");
                }

                if (parameters.ContainsKey("Prescription"))
                {
                    OriginalPrescription = parameters.GetValue<PrescriptionDto>("Prescription");
                    LoadFromPrescription(OriginalPrescription);
                }
                else if (PrescriptionId != Guid.Empty)
                {
                    // 加载处方数据
                    Task.Run(async () => await LoadPrescriptionAsync());
                }

                // 重置变更状态
                HasChanges = false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开处方编辑对话框时发生异常");
                ShowErrorMessage("初始化失败，请稍后重试");
            }
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载处方数据
        /// </summary>
        private async Task LoadPrescriptionAsync()
        {
            try
            {
                SetIsBusy(true, "正在加载处方信息...");

                // Issue #1786: 使用DataManager包装Api方法
                var response = await _dataManager.GetPrescriptionByIdAsync(PrescriptionId);
                var prescription = response.Data;
                if (prescription == null)
                {
                    await ShowErrorMessageAsync("处方不存在");
                    return;
                }

                OriginalPrescription = prescription;
                LoadFromPrescription(OriginalPrescription);
                Logger.LogInformation("处方信息加载完成");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载处方信息时发生异常");
                await ShowErrorMessageAsync("加载处方信息时发生系统错误");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 从处方对象加载数据
        /// Issue #1423 RULE-4: 添加只读模式检测
        /// </summary>
        private void LoadFromPrescription(PrescriptionDto prescription)
        {
            if (prescription == null) return;

            try
            {
                IsValidationEnabled = false;

                // RULE-4: 检查是否为创建当天，隔日后进入只读模式
                if (prescription.CreatedAt.Date != DateTime.Today)
                {
                    IsReadOnly = true;
                    ReadOnlyReason = $"只读模式：该处方创建于 {prescription.CreatedAt:yyyy-MM-dd}，已超过可修改期限（仅限创建当天可修改）";
                    Logger.LogInformation("处方 {PrescriptionId} 进入只读模式，创建日期：{CreatedDate}",
                        prescription.Id, prescription.CreatedAt.Date);
                }
                else
                {
                    IsReadOnly = false;
                    ReadOnlyReason = string.Empty;
                }

                // PrescriptionNo字段已删除
                // PrescriptionNo = prescription.PrescriptionNo ?? string.Empty;
                PrescriptionNo = $"CF{DateTime.Now:yyyyMMddHHmmss}"; // 使用默认生成规则
                DosageCount = prescription.DosageCount;
                Usage = prescription.Usage ?? "水煎服，一日三次，饭后服用";
                MedicalAdvice = prescription.Advice ?? string.Empty;
                Remark = prescription.Remark ?? string.Empty;
                Discount = prescription.Discount;
                TotalAmount = prescription.TotalPrice;

                HasChanges = false;
            }
            finally
            {
                IsValidationEnabled = true;
            }
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 保存
        /// Issue #1423 RULE-4: 只读模式禁止保存
        /// </summary>
        private async Task SaveAsync()
        {
            try
            {
                // RULE-4: 只读模式检查
                if (IsReadOnly)
                {
                    await ShowWarningMessageAsync(ReadOnlyReason);
                    return;
                }

                if (!ValidateAll())
                {
                    await ShowWarningMessageAsync("请修正输入错误后再保存");
                    return;
                }

                SetIsBusy(true, "正在保存处方信息...");

                var updateDto = new PrescriptionUpdateDto
                {
                    // PrescriptionNo字段已删除，使用其他字段
                    DosageCount = DosageCount,
                    Usage = Usage,
                    Advice = MedicalAdvice,
                    Remark = Remark,
                    Discount = Discount
                };

                // Issue #1786: 使用DataManager包装Repository方法，并修复bug - 使用MedicalCaseId
                if (OriginalPrescription == null)
                {
                    await ShowErrorMessageAsync("无法保存：缺少医案信息");
                    return;
                }
                var updatedPrescription = await _dataManager.UpdatePrescriptionAsync(OriginalPrescription.MedicalCaseId, updateDto);
                if (updatedPrescription == null)
                {
                    await ShowErrorMessageAsync("保存处方失败");
                    return;
                }
                await ShowSuccessMessageAsync("处方信息保存成功");

                var parameters = new DialogParameters
                {
                    { "UpdatedPrescription", updatedPrescription },
                    { "HasChanges", true }
                };

                RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存处方信息时发生异常");
                await ShowErrorMessageAsync("保存处方信息时发生系统错误");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 取消
        /// </summary>
        private void Cancel()
        {
            if (HasChanges)
            {
                var confirmed = ShowConfirmMessage("有未保存的更改，确定要取消吗？");
                if (!confirmed) return;
            }

            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        /// <summary>
        /// 重置
        /// </summary>
        private void Reset()
        {
            if (OriginalPrescription != null)
            {
                var confirmed = ShowConfirmMessage("确定要重置所有更改吗？");
                if (confirmed)
                {
                    LoadFromPrescription(OriginalPrescription);
                    ClearAllErrors();
                }
            }
        }

        /// <summary>
        /// 验证所有字段
        /// </summary>
        private bool ValidateAll()
        {
            if (!IsValidationEnabled) return true;

            ClearAllErrors();

            var isValid = true;

            // 验证处方编号
            if (string.IsNullOrWhiteSpace(PrescriptionNo))
            {
                AddError(nameof(PrescriptionNo), "处方编号不能为空");
                isValid = false;
            }

            // 验证剂数
            if (DosageCount < 1 || DosageCount > 100)
            {
                AddError(nameof(DosageCount), "剂数必须在1-100之间");
                isValid = false;
            }

            // 验证用法
            if (string.IsNullOrWhiteSpace(Usage))
            {
                AddError(nameof(Usage), "用法不能为空");
                isValid = false;
            }

            // 验证折扣
            if (Discount < 0.1m || Discount > 1.0m)
            {
                AddError(nameof(Discount), "折扣必须在0.1-1.0之间");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// 验证命令包装器 - 用于DelegateCommand
        /// </summary>
        private void ValidateAllWrapper()
        {
            ValidateAll();
        }

        #endregion

        #region 命令状态检查

        /// <summary>
        /// 是否可以保存（Issue #1423 RULE-4: 只读模式禁止保存）
        /// </summary>
        private bool CanSave() => !IsReadOnly && HasChanges && !IsBusy && !HasErrors;

        /// <summary>
        /// 是否可以重置
        /// </summary>
        private bool CanReset() => !IsReadOnly && HasChanges && OriginalPrescription != null;

        private void UpdateCommandStates()
        {
            SaveCommand.RaiseCanExecuteChanged();
            ResetCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 标记为已更改
        /// </summary>
        private void MarkAsChanged()
        {
            if (IsValidationEnabled)
            {
                HasChanges = true;
            }
        }

        /// <summary>
        /// 检查是否有实际更改
        /// </summary>
        private bool HasActualChanges()
        {
            if (OriginalPrescription == null) return HasChanges;

            return DosageCount != OriginalPrescription.DosageCount ||
                   Usage != (OriginalPrescription.Usage ?? "水煎服，一日三次，饭后服用") ||
                   MedicalAdvice != (OriginalPrescription.Advice ?? string.Empty) ||
                   Remark != (OriginalPrescription.Remark ?? string.Empty) ||
                   Math.Abs(Discount - OriginalPrescription.Discount) > 0.001m;
        }

        #endregion
    }
}
