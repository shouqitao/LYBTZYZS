using System.Windows.Controls;

namespace LYBT.Desktop.MedicalCase.Views;

/// <summary>
/// 医案Master-Detail视图
/// 包装MedicalCaseMasterDetailControl，供导航使用
/// 注意：医案不支持新建，新建通过挂号入口创建
/// </summary>
public partial class MedicalCaseMasterDetailView : UserControl
{
    public MedicalCaseMasterDetailView()
    {
        InitializeComponent();
    }
}
