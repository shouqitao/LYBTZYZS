using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.UI.WPF.Views.Admin {
    /// <summary>
    /// 用户管理视图的交互逻辑
    /// </summary>
    public partial class UserManagementView : UserControl {
        public UserManagementView() {
            InitializeComponent();

            // 设置键盘快捷键
            SetupKeyboardShortcuts();
        }

        /// <summary>
        /// 设置键盘快捷键
        /// </summary>
        private void SetupKeyboardShortcuts() {
            // Ctrl+N: 新增用户
            var addUserGesture = new KeyGesture(Key.N, ModifierKeys.Control);
            var addUserBinding = new KeyBinding(
                new RelayCommand(() => ExecuteCommand("AddUserCommand")),
                addUserGesture);
            InputBindings.Add(addUserBinding);

            // Ctrl+E: 编辑用户
            var editUserGesture = new KeyGesture(Key.E, ModifierKeys.Control);
            var editUserBinding = new KeyBinding(
                new RelayCommand(() => ExecuteCommand("EditUserCommand")),
                editUserGesture);
            InputBindings.Add(editUserBinding);

            // F5: 刷新列表
            var refreshGesture = new KeyGesture(Key.F5);
            var refreshBinding = new KeyBinding(
                new RelayCommand(() => ExecuteCommand("RefreshCommand")),
                refreshGesture);
            InputBindings.Add(refreshBinding);

            // Delete: 禁用用户
            var disableGesture = new KeyGesture(Key.Delete);
            var disableBinding = new KeyBinding(
                new RelayCommand(() => ExecuteCommand("DisableUserCommand")),
                disableGesture);
            InputBindings.Add(disableBinding);

            // Ctrl+S: 保存
            var saveGesture = new KeyGesture(Key.S, ModifierKeys.Control);
            var saveBinding = new KeyBinding(
                new RelayCommand(() => ExecuteCommand("SaveUserCommand")),
                saveGesture);
            InputBindings.Add(saveBinding);

            // Escape: 取消
            var cancelGesture = new KeyGesture(Key.Escape);
            var cancelBinding = new KeyBinding(
                new RelayCommand(() => ExecuteCommand("CancelCommand")),
                cancelGesture);
            InputBindings.Add(cancelBinding);
        }

        /// <summary>
        /// 执行ViewModel中的命令
        /// </summary>
        /// <param name="commandName">命令名称</param>
        private void ExecuteCommand(string commandName) {
            if (DataContext == null)
                return;

            var command = DataContext.GetType().GetProperty(commandName)?.GetValue(DataContext) as ICommand;
            if (command?.CanExecute(null) == true) {
                command.Execute(null);
            }
        }

        /// <summary>
        /// 视图加载完成事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserManagementView_Loaded(object sender, RoutedEventArgs e) {
            // 设置焦点到搜索框（如果有的话）
            // 可以在这里添加初始化逻辑
        }
    }

    /// <summary>
    /// 简单的RelayCommand实现，用于快捷键绑定
    /// </summary>
    public class RelayCommand : ICommand {
        private readonly System.Action _execute;
        private readonly System.Func<bool> _canExecute;

        public RelayCommand(System.Action execute, System.Func<bool> canExecute = null) {
            _execute = execute ?? throw new System.ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event System.EventHandler CanExecuteChanged {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object parameter) {
            _execute();
        }
    }
}