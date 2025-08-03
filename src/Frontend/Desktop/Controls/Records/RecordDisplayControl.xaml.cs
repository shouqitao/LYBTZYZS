using System.Windows;
using LYBT.Shared.Models.Contracts.Records;
using LYBT.WPF.Client.Controls.Base;

namespace LYBT.WPF.Client.Controls.Records
{
    /// <summary>
    /// RecordDisplayControl.xaml 的交互逻辑
    /// 用于展示 RecordDto 的用户控件
    /// </summary>
    public partial class RecordDisplayControl : BaseDisplayControl<RecordDto>
    {
        public RecordDisplayControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 是否有处方依赖属性（RecordDto没有此属性，需要扩展）
        /// </summary>
        public static readonly DependencyProperty HasPrescriptionProperty =
            DependencyProperty.Register(
                nameof(HasPrescription),
                typeof(bool),
                typeof(RecordDisplayControl),
                new PropertyMetadata(false));

        /// <summary>
        /// 获取或设置是否有处方
        /// </summary>
        public bool HasPrescription
        {
            get => (bool)GetValue(HasPrescriptionProperty);
            set => SetValue(HasPrescriptionProperty, value);
        }

        /// <summary>
        /// 重写数据变更处理
        /// </summary>
        protected override void OnDataChanged(RecordDto oldValue, RecordDto newValue)
        {
            base.OnDataChanged(oldValue, newValue);
            
            // 可以在这里添加病历特定的逻辑
            // 例如：根据诊断内容判断是否有处方
            if (newValue != null)
            {
                // 这里可以根据业务逻辑设置HasPrescription
                // HasPrescription = !string.IsNullOrEmpty(newValue.PrescriptionId);
            }
        }
    }
}