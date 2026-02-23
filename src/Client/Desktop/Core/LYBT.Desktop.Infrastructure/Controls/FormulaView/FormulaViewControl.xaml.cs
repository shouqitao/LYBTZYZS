using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Infrastructure.Controls.FormulaView
{
    /// <summary>
    /// 验方预览控件 - OpenSpec: extract-detail-controls Task 1.1
    /// 独立的验方预览控件，可在FormulaDetailView和FormulaImportDialog中复用
    /// OpenSpec: unify-vm-view-binding-patterns - 重构为接受object类型（duck-typing）
    /// OpenSpec: cross-module-decoupling - 迁移到Infrastructure，解耦模块间编译依赖
    ///
    /// WPF绑定引擎按属性名解析 -- FormulaDetailModel和FormulaDetailDto属性名一致，运行时均可工作。
    /// 这是WPF控件的常见模式（DataTemplate duck-typing）。
    /// </summary>
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
        /// 是否显示审计信息（创建时间、更新时间）
        /// </summary>
        public static readonly DependencyProperty ShowAuditInfoProperty =
            DependencyProperty.Register(
                nameof(ShowAuditInfo),
                typeof(bool),
                typeof(FormulaViewControl),
                new PropertyMetadata(true));

        public bool ShowAuditInfo
        {
            get => (bool)GetValue(ShowAuditInfoProperty);
            set => SetValue(ShowAuditInfoProperty, value);
        }
    }
}
