using System.Windows;
using LYBT.Desktop.Core.ViewModels.Dialogs;
using SharedCommon = LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.Views.Dialogs
{

    /// <summary>
    /// UltraThink Command绑定优化：严重错误对话框
    /// 消除Click事件处理器，使用Command绑定模式
    /// </summary>
    public partial class CriticalErrorDialog : Window
    {
        private CriticalErrorDialogViewModel? ViewModel => DataContext as CriticalErrorDialogViewModel;

        public SharedCommon.HandledError? ErrorInfo
        {
            get => ViewModel?.ErrorInfo;
            set
            {
                if (ViewModel != null)
                {
                    ViewModel.ErrorInfo = value;
                }
            }
        }

        public CriticalErrorDialog()
        {
            InitializeComponent();

            // 设置窗口属性
            this.WindowStyle = WindowStyle.ToolWindow;
            this.ShowActivated = true;
            this.Topmost = true;

            // 订阅ViewModel的关闭请求
            this.DataContextChanged += (s, e) =>
            {
                if (ViewModel != null)
                {
                    ViewModel.RequestClose += OnRequestClose;
                }
            };
        }

        /// <summary>
        /// 处理ViewModel的关闭请求
        /// </summary>
        private void OnRequestClose(bool? dialogResult)
        {
            this.DialogResult = dialogResult;
            this.Close();
        }

        /// <inheritdoc/>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 确保窗口显示在最前面
            this.Activate();
            this.Focus();
        }
    }
}
