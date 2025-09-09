using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.WPF.Client.Controls.Formulas
{

    /// <summary>
    /// FormulaListItemControl.xaml 的交互逻辑
    /// 验方模板列表项控件
    /// </summary>
    public partial class FormulaListItemControl : UserControl
    {

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(FormulaDto),
                typeof(FormulaListItemControl),
                new PropertyMetadata(null));

        public FormulaDto Data
        {
            get => (FormulaDto)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public FormulaListItemControl()
        {
            InitializeComponent();
        }
    }
}
