using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Infrastructure.Controls.FormulaTemplates
{

    /// <summary>
    /// FormulaTemplateListItemControl.xaml 的交互逻辑
    /// 验方模板列表项控件
    /// </summary>
    public partial class FormulaTemplateListItemControl : UserControl
    {

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(FormulaDto),
                typeof(FormulaTemplateListItemControl),
                new PropertyMetadata(null));

        public FormulaDto Data
        {
            get => (FormulaDto)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public FormulaTemplateListItemControl()
        {
            InitializeComponent();
        }
    }
}
