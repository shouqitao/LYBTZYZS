using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.Desktop.Consultation.Controls
{
    /// <summary>
    /// WorkflowNavigator.xaml 的交互逻辑
    /// 增强型工作流导航控件，支持可视化步骤导航和快捷键
    /// </summary>
    public partial class WorkflowNavigator : UserControl
    {
        public WorkflowNavigator()
        {
            InitializeComponent();
            
            // 注册快捷键
            RegisterKeyboardShortcuts();
        }

        private void RegisterKeyboardShortcuts()
        {
            // Alt+1~4 快速切换步骤
            this.PreviewKeyDown += (s, e) =>
            {
                if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt)
                {
                    switch (e.Key)
                    {
                        case Key.D1:
                        case Key.NumPad1:
                            NavigateToStep(0);
                            e.Handled = true;
                            break;
                        case Key.D2:
                        case Key.NumPad2:
                            NavigateToStep(1);
                            e.Handled = true;
                            break;
                        case Key.D3:
                        case Key.NumPad3:
                            NavigateToStep(2);
                            e.Handled = true;
                            break;
                        case Key.D4:
                        case Key.NumPad4:
                            NavigateToStep(3);
                            e.Handled = true;
                            break;
                    }
                }
                // Enter 下一步
                else if (e.Key == Key.Enter && e.KeyboardDevice.Modifiers == ModifierKeys.None)
                {
                    NavigateNext();
                    e.Handled = true;
                }
                // Esc 上一步
                else if (e.Key == Key.Escape && e.KeyboardDevice.Modifiers == ModifierKeys.None)
                {
                    NavigatePrevious();
                    e.Handled = true;
                }
            };
        }

        private void NavigateToStep(int stepIndex)
        {
            if (DataContext is ViewModels.WorkflowNavigatorViewModel viewModel)
            {
                if (stepIndex >= 0 && stepIndex < viewModel.Steps.Count)
                {
                    var step = viewModel.Steps[stepIndex];
                    if (viewModel.NavigateToStepCommand.CanExecute(step))
                    {
                        viewModel.NavigateToStepCommand.Execute(step);
                    }
                }
            }
        }

        private void NavigateNext()
        {
            if (DataContext is ViewModels.WorkflowNavigatorViewModel viewModel)
            {
                if (viewModel.NavigateNextCommand.CanExecute())
                {
                    viewModel.NavigateNextCommand.Execute();
                }
            }
        }

        private void NavigatePrevious()
        {
            if (DataContext is ViewModels.WorkflowNavigatorViewModel viewModel)
            {
                if (viewModel.NavigatePreviousCommand.CanExecute())
                {
                    viewModel.NavigatePreviousCommand.Execute();
                }
            }
        }
    }
}