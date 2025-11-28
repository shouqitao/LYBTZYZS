using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.MedicalCase.Dialogs
{
    /// <summary>
    /// OpenSpec: medicalcase-management-ui-refactor (EDITMODE-011)
    /// 审计理由对话框ViewModel - 修改医案时填写修改原因
    /// </summary>
    public class AuditReasonDialogViewModel : BindableBase, IDialogAware
    {
        #region 常用原因常量

        private const string Reason1 = "补充遗漏信息";
        private const string Reason2 = "更正录入错误";
        private const string Reason3 = "患者要求修改";
        private const string Reason4 = "医嘱调整";

        #endregion

        #region 属性

        private string _reason = string.Empty;
        /// <summary>
        /// 修改原因
        /// </summary>
        public string Reason
        {
            get => _reason;
            set
            {
                if (SetProperty(ref _reason, value))
                {
                    RaisePropertyChanged(nameof(ReasonLength));
                    RaisePropertyChanged(nameof(CanConfirm));
                }
            }
        }

        /// <summary>
        /// 原因字符数
        /// </summary>
        public int ReasonLength => Reason?.Length ?? 0;

        /// <summary>
        /// 是否可以确认（原因非空）
        /// </summary>
        public bool CanConfirm => !string.IsNullOrWhiteSpace(Reason);

        #region 常用原因选择

        private bool _isReason1Selected;
        public bool IsReason1Selected
        {
            get => _isReason1Selected;
            set
            {
                if (SetProperty(ref _isReason1Selected, value) && value)
                {
                    Reason = Reason1;
                }
            }
        }

        private bool _isReason2Selected;
        public bool IsReason2Selected
        {
            get => _isReason2Selected;
            set
            {
                if (SetProperty(ref _isReason2Selected, value) && value)
                {
                    Reason = Reason2;
                }
            }
        }

        private bool _isReason3Selected;
        public bool IsReason3Selected
        {
            get => _isReason3Selected;
            set
            {
                if (SetProperty(ref _isReason3Selected, value) && value)
                {
                    Reason = Reason3;
                }
            }
        }

        private bool _isReason4Selected;
        public bool IsReason4Selected
        {
            get => _isReason4Selected;
            set
            {
                if (SetProperty(ref _isReason4Selected, value) && value)
                {
                    Reason = Reason4;
                }
            }
        }

        #endregion

        #endregion

        #region IDialogAware

        public string Title => "修改原因";

        public event Action<IDialogResult>? RequestClose;

        public bool CanCloseDialog() => true;

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            // 可选：从参数获取预填内容
            if (parameters.TryGetValue("Reason", out string? reason) && !string.IsNullOrEmpty(reason))
            {
                Reason = reason;
            }
        }

        #endregion

        #region 命令

        /// <summary>
        /// 确认命令 - 返回修改原因
        /// </summary>
        public DelegateCommand ConfirmCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 构造函数

        public AuditReasonDialogViewModel()
        {
            ConfirmCommand = new DelegateCommand(ExecuteConfirm, () => CanConfirm)
                .ObservesProperty(() => CanConfirm);
            CancelCommand = new DelegateCommand(ExecuteCancel);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 确认保存 - 返回修改原因
        /// </summary>
        private void ExecuteConfirm()
        {
            var parameters = new DialogParameters
            {
                { "Reason", Reason.Trim() }
            };
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
        }

        /// <summary>
        /// 取消
        /// </summary>
        private void ExecuteCancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        #endregion
    }
}
