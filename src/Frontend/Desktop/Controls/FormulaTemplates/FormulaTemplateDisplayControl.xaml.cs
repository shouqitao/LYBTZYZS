using LYBT.Shared.Models.Contracts.FormulaTemplates;
using LYBT.WPF.Client.Controls.Base;

namespace LYBT.WPF.Client.Controls.FormulaTemplates
{
    /// <summary>
    /// FormulaTemplateDisplayControl.xaml 的交互逻辑
    /// 用于展示 FormulaTemplateDto 的用户控件
    /// </summary>
    public partial class FormulaTemplateDisplayControl : BaseDisplayControl<FormulaTemplateDto>
    {
        public FormulaTemplateDisplayControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 重写数据变更处理
        /// </summary>
        protected override void OnDataChanged(FormulaTemplateDto oldValue, FormulaTemplateDto newValue)
        {
            base.OnDataChanged(oldValue, newValue);
            
            // 可以在这里添加验方模板特定的逻辑
            // 例如：计算预估价格
            if (newValue != null && newValue.Herbs != null)
            {
                decimal estimatedPrice = 0;
                foreach (var herb in newValue.Herbs)
                {
                    estimatedPrice += herb.Price * herb.Dosage;
                }
                // 如果DTO有EstimatedPrice属性，在这里设置
            }
        }
    }
}