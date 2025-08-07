using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.WPF.Client.Core.Models.Formulas;
using LYBT.WPF.Client.Core.Models.Prescriptions;

namespace LYBT.WPF.Client.Modules.Consultation.Views
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
                if (sender is ComboBoxItem item && item.DataContext is HerbInfo herb)
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
                if (sender is ComboBoxItem item && item.DataContext is FormulaInfo formula)
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