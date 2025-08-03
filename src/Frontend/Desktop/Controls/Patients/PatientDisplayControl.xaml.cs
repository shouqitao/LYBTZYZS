using LYBT.Shared.Models.Contracts.Patients;
using LYBT.WPF.Client.Controls.Base;

namespace LYBT.WPF.Client.Controls.Patients
{
    /// <summary>
    /// PatientDisplayControl.xaml 的交互逻辑
    /// 用于展示 PatientDetailDto 的用户控件
    /// </summary>
    public partial class PatientDisplayControl : BaseDisplayControl<PatientDetailDto>
    {
        public PatientDisplayControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 重写数据变更处理
        /// </summary>
        protected override void OnDataChanged(PatientDetailDto oldValue, PatientDetailDto newValue)
        {
            base.OnDataChanged(oldValue, newValue);
            
            // 可以在这里添加患者特定的逻辑
            // 例如：根据患者状态改变显示样式
        }
    }
}