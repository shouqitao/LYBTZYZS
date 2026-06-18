using System.Windows.Controls;
using LYBT.Desktop.Clinical.ViewModels;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Clinical.Views;

/// <summary>
/// 临床工作台 - 一体化布局
/// 左侧：患者选择列表（嵌入 PatientSelectionControl）
/// 右侧：看诊工作区（信息条 + 空状态 + 历史面板 + 操作按钮）
/// </summary>
public partial class ClinicalWorkspaceView : UserControl
{
    public ClinicalWorkspaceView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 双击患者：等同于点击「开始看诊」按钮
    /// </summary>
    private void PatientSelectionControl_PatientDoubleClicked(object? sender, PatientListDto e)
    {
        if (DataContext is ClinicalWorkspaceViewModel vm
            && vm.StartConsultationCommand.CanExecute(null))
        {
            vm.StartConsultationCommand.Execute(null);
        }
    }
}
