using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Contracts.FormulaTemplates;

namespace LYBT.WPF.Client.Controls.FormulaTemplates
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
                typeof(FormulaTemplateDto),
                typeof(FormulaTemplateListItemControl),
                new PropertyMetadata(null));

        public FormulaTemplateDto Data
        {
            get => (FormulaTemplateDto)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public FormulaTemplateListItemControl()
        {
            InitializeComponent();
        }
    }
}