using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Controls.Controls.FormulaView
{
    /// <summary>
    /// 验方预览控件</summary>
    public partial class FormulaViewControl : UserControl
    {
        public FormulaViewControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 验方详情数据
        /// 使用object类型支持FormulaDetailModel和FormulaDetailDto（属性名一致，WPF按名绑定）
        /// </summary>
        public static readonly DependencyProperty FormulaProperty =
            DependencyProperty.Register(
                nameof(Formula),
                typeof(object),
                typeof(FormulaViewControl),
                new PropertyMetadata(null));

        public object? Formula
        {
            get => GetValue(FormulaProperty);
            set => SetValue(FormulaProperty, value);
        }

        /// <summary>
        /// 是否显示系统信息（创建时间、更新时间）
        /// </summary>
        public static readonly DependencyProperty ShowSystemInfoProperty =
            DependencyProperty.Register(
                nameof(ShowSystemInfo),
                typeof(bool),
                typeof(FormulaViewControl),
                new PropertyMetadata(true));

        public bool ShowSystemInfo
        {
            get => (bool)GetValue(ShowSystemInfoProperty);
            set => SetValue(ShowSystemInfoProperty, value);
        }
    }
}
