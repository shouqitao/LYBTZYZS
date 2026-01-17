using System.Windows.Controls;
using System.Windows.Input;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Controls
{
    /// <summary>
    /// 患者选择控件
    /// OpenSpec: fix-elementname-binding-architecture
    ///
    /// 可复用的患者选择控件，使用Master-Detail布局
    /// - 左侧：患者列表（工具栏+搜索+列表）
    /// - 右侧：患者详情（使用PatientViewControl）
    ///
    /// 预期 DataContext 类型: PatientSelectionViewModel
    /// - Patients: ObservableCollection&lt;PatientListDto&gt;
    /// - SelectedPatient: PatientListDto?
    /// - PatientDetail: PatientDetailDto?
    /// - HasSelection: bool
    /// - SearchKeyword: string
    /// - NewPatientCommand: ICommand
    /// - RefreshCommand: ICommand
    /// - SearchCommand: ICommand
    /// - StartMedicalCaseCommand: ICommand
    /// - IsBusy: bool
    /// </summary>
    public partial class PatientSelectionControl : UserControl
    {
        public PatientSelectionControl()
        {
            InitializeComponent();
        }

        #region 事件

        /// <summary>
        /// 患者双击事件（用于执行主操作，如开始看诊）
        /// </summary>
        public event EventHandler<PatientListDto>? PatientDoubleClicked;

        /// <summary>
        /// 双击处理 - 从DataContext获取SelectedPatient和Command
        /// OpenSpec: fix-elementname-binding-architecture - 使用DataContext替代DependencyProperty
        /// </summary>
        private void PatientDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 从DataContext获取SelectedPatient
            var selectedPatient = GetPropertyValue<PatientListDto?>("SelectedPatient");
            if (selectedPatient == null) return;

            // 触发事件
            PatientDoubleClicked?.Invoke(this, selectedPatient);

            // 执行StartMedicalCaseCommand（原SelectCommand）
            var command = GetPropertyValue<ICommand?>("StartMedicalCaseCommand");
            if (command?.CanExecute(selectedPatient) == true)
            {
                command.Execute(selectedPatient);
            }
        }

        /// <summary>
        /// 从DataContext获取属性值的辅助方法
        /// </summary>
        private T? GetPropertyValue<T>(string propertyName)
        {
            if (DataContext == null) return default;

            var property = DataContext.GetType().GetProperty(propertyName);
            if (property == null) return default;

            var value = property.GetValue(DataContext);
            return value is T typed ? typed : default;
        }

        #endregion
    }
}
