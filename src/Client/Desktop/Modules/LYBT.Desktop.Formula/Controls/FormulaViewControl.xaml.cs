using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.Controls
{
    /// <summary>
    /// 验方预览控件 - OpenSpec: extract-detail-controls Task 1.1
    /// 独立的验方预览控件，可在FormulaDetailView和FormulaImportDialog中复用
    /// </summary>
    public partial class FormulaViewControl : UserControl
    {
        public FormulaViewControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 验方详情数据（接受FormulaDetailDto或其子类FormulaDetailDto）
        /// OpenSpec: extract-detail-controls - 使用基类FormulaDetailDto支持更广泛复用
        /// </summary>
        public static readonly DependencyProperty FormulaProperty =
            DependencyProperty.Register(
                nameof(Formula),
                typeof(FormulaDetailDto),
                typeof(FormulaViewControl),
                new PropertyMetadata(null));

        public FormulaDetailDto? Formula
        {
            get => (FormulaDetailDto?)GetValue(FormulaProperty);
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
