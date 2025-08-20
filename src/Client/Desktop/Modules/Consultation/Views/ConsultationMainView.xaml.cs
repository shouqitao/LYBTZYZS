using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
// UltraThink v2.0: 直接使用DTOs，移除Info模型引用
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Desktop.Core.Models.Prescriptions;

namespace LYBT.Desktop.Consultation.Views
{
    /// <summary>
    /// ConsultationMainView.xaml 的交互逻辑
    /// </summary>
    public partial class ConsultationMainView : UserControl
    {
        public ConsultationMainView()
        {
            InitializeComponent();
        }

        private void OnHerbSelected(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is ComboBoxItem item && item.DataContext is HerbDto herb)
                {
                    if (DataContext is ViewModels.ConsultationMainViewModel viewModel && viewModel.AddHerbCommand != null)
                    {
                        if (viewModel.AddHerbCommand.CanExecute(herb))
                        {
                            viewModel.AddHerbCommand.Execute(herb);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误日志，但不中断用户操作
                System.Diagnostics.Debug.WriteLine($"添加药材时发生错误: {ex.Message}");
            }
        }

        private void OnFormulaSelected(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is ComboBoxItem item && item.DataContext is FormulaDto formula)
                {
                    if (DataContext is ViewModels.ConsultationMainViewModel viewModel && viewModel.ApplyFormulaCommand != null)
                    {
                        if (viewModel.ApplyFormulaCommand.CanExecute(formula))
                        {
                            viewModel.ApplyFormulaCommand.Execute(formula);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误日志，但不中断用户操作
                System.Diagnostics.Debug.WriteLine($"应用验方时发生错误: {ex.Message}");
            }
        }


    }
}