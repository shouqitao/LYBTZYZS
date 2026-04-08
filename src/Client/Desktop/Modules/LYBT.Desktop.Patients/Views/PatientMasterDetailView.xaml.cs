using System.Windows.Controls;

namespace LYBT.Desktop.Patients.Views;

/// <summary>
/// 患者Master-Detail视图
/// 包装PatientMasterDetailControl，供导航使用
/// </summary>
public partial class PatientMasterDetailView : UserControl
{
    public PatientMasterDetailView()
    {
        InitializeComponent();
    }
}