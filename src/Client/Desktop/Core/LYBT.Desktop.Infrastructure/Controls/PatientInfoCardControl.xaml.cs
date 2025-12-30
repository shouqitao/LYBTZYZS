using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 患者信息卡片控件 - 用于显示患者基本信息
    /// OpenSpec: refactor-medicalcase-workspace
    /// </summary>
    public partial class PatientInfoCardControl : UserControl
    {
        public PatientInfoCardControl() => InitializeComponent();

        #region Patient - 患者数据

        /// <summary>
        /// 患者数据
        /// </summary>
        public PatientDisplayModel Patient
        {
            get => (PatientDisplayModel)GetValue(PatientProperty);
            set => SetValue(PatientProperty, value);
        }

        public static readonly DependencyProperty PatientProperty =
            DependencyProperty.Register(nameof(Patient), typeof(PatientDisplayModel), typeof(PatientInfoCardControl),
                new PropertyMetadata(null));

        #endregion

        #region DisplayMode - 显示模式

        /// <summary>
        /// 显示模式: Full/Compact/Minimal
        /// </summary>
        public PatientCardDisplayMode DisplayMode
        {
            get => (PatientCardDisplayMode)GetValue(DisplayModeProperty);
            set => SetValue(DisplayModeProperty, value);
        }

        public static readonly DependencyProperty DisplayModeProperty =
            DependencyProperty.Register(nameof(DisplayMode), typeof(PatientCardDisplayMode), typeof(PatientInfoCardControl),
                new PropertyMetadata(PatientCardDisplayMode.Full));

        #endregion

        #region ShowHistoryButton - 是否显示历史按钮

        /// <summary>
        /// 是否显示查看历史按钮
        /// </summary>
        public bool ShowHistoryButton
        {
            get => (bool)GetValue(ShowHistoryButtonProperty);
            set => SetValue(ShowHistoryButtonProperty, value);
        }

        public static readonly DependencyProperty ShowHistoryButtonProperty =
            DependencyProperty.Register(nameof(ShowHistoryButton), typeof(bool), typeof(PatientInfoCardControl),
                new PropertyMetadata(true));

        #endregion

        #region HistoryCommand - 查看历史命令

        /// <summary>
        /// 查看历史命令
        /// </summary>
        public ICommand HistoryCommand
        {
            get => (ICommand)GetValue(HistoryCommandProperty);
            set => SetValue(HistoryCommandProperty, value);
        }

        public static readonly DependencyProperty HistoryCommandProperty =
            DependencyProperty.Register(nameof(HistoryCommand), typeof(ICommand), typeof(PatientInfoCardControl),
                new PropertyMetadata(null));

        #endregion

        #region ShowVisitCount - 是否显示就诊次数

        /// <summary>
        /// 是否显示就诊次数
        /// </summary>
        public bool ShowVisitCount
        {
            get => (bool)GetValue(ShowVisitCountProperty);
            set => SetValue(ShowVisitCountProperty, value);
        }

        public static readonly DependencyProperty ShowVisitCountProperty =
            DependencyProperty.Register(nameof(ShowVisitCount), typeof(bool), typeof(PatientInfoCardControl),
                new PropertyMetadata(true));

        #endregion
    }
}
