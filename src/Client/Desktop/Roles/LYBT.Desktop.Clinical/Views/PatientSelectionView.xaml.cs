using System.Windows.Controls;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Clinical.Views;

/// <summary>
/// 患者选择主界面 - Code Behind
/// OpenSpec: refactor-clinical-workflow
/// 位于Clinical模块，使用Patients模块的PatientSelectionControl
/// </summary>
public partial class PatientSelectionView : UserControl
{
    public PatientSelectionView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 处理患者双击事件
    /// 双击患者时触发开始看诊命令
    /// </summary>
    private void PatientSelectionControl_PatientDoubleClicked(object? sender, PatientListDto e)
    {
        if (DataContext is ViewModels.PatientSelectionViewModel viewModel)
        {
            // 双击等同于点击"开始看诊"按钮
            if (viewModel.StartConsultationCommand.CanExecute())
            {
                viewModel.StartConsultationCommand.Execute();
            }
        }
    }
}
