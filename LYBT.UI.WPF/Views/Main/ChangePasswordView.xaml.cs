using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LYBT.UI.WPF.ViewModels.Main;

namespace LYBT.UI.WPF.Views.Main {
    /// <summary>
    /// 修改密码视图的交互逻辑 - 优化版
    /// </summary>
    public partial class ChangePasswordView : UserControl {
        public ChangePasswordView() {
            InitializeComponent();
            InitializeView();
        }

        #region 初始化

        /// <summary>
        /// 初始化视图
        /// </summary>
        private void InitializeView() {
            this.Loaded += OnViewLoaded;
            this.Unloaded += OnUnloaded;
            this.KeyDown += OnKeyDown;
        }

        /// <summary>
        /// 视图加载完成
        /// </summary>
        private void OnViewLoaded(object sender, RoutedEventArgs e) {
            try {
                // 设置初始焦点
                oldPasswordBox.Focus();

                // 订阅视图模型事件
                if (DataContext is ChangePasswordViewModel viewModel) {
                    viewModel.PropertyChanged += OnViewModelPropertyChanged;
                }

                System.Diagnostics.Debug.WriteLine("ChangePasswordView loaded successfully");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ChangePasswordView load error: {ex.Message}");
            }
        }

        #endregion

        #region 密码框事件处理

        /// <summary>
        /// 旧密码改变事件
        /// </summary>
        private void OldPasswordBox_PasswordChanged(object sender, RoutedEventArgs e) {
            try {
                if (DataContext is ChangePasswordViewModel vm && sender is PasswordBox passwordBox) {
                    vm.OldPassword = passwordBox.Password;

                    // 清除错误信息
                    if (!string.IsNullOrEmpty(vm.ErrorMessage) && !string.IsNullOrEmpty(passwordBox.Password)) {
                        vm.ErrorMessage = string.Empty;
                    }
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OldPasswordBox_PasswordChanged error: {ex.Message}");
            }
        }

        /// <summary>
        /// 新密码改变事件
        /// </summary>
        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e) {
            try {
                if (DataContext is ChangePasswordViewModel vm && sender is PasswordBox passwordBox) {
                    vm.NewPassword = passwordBox.Password;

                    // 实时密码强度验证
                    ValidatePasswordStrength(passwordBox.Password);

                    // 如果确认密码已输入，重新验证匹配性
                    if (!string.IsNullOrEmpty(confirmPasswordBox.Password)) {
                        ValidatePasswordMatch();
                    }
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"NewPasswordBox_PasswordChanged error: {ex.Message}");
            }
        }

        /// <summary>
        /// 确认密码改变事件
        /// </summary>
        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e) {
            try {
                if (DataContext is ChangePasswordViewModel vm && sender is PasswordBox passwordBox) {
                    vm.ConfirmPassword = passwordBox.Password;

                    // 实时验证密码匹配
                    ValidatePasswordMatch();
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ConfirmPasswordBox_PasswordChanged error: {ex.Message}");
            }
        }

        #endregion

        #region 键盘事件处理

        /// <summary>
        /// 键盘按键事件
        /// </summary>
        private void OnKeyDown(object sender, KeyEventArgs e) {
            try {
                if (DataContext is not ChangePasswordViewModel viewModel)
                    return;

                switch (e.Key) {
                    case Key.Enter:
                        // 回车键执行保存
                        if (viewModel.SaveCommand?.CanExecute() == true) {
                            viewModel.SaveCommand.Execute();
                            e.Handled = true;
                        }
                        break;

                    case Key.Escape:
                        // ESC键取消
                        if (viewModel.CancelCommand?.CanExecute() == true) {
                            viewModel.CancelCommand.Execute();
                            e.Handled = true;
                        }
                        break;

                    case Key.Tab:
                        // Tab键焦点切换优化
                        HandleTabNavigation(e);
                        break;
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OnKeyDown error: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理Tab键导航
        /// </summary>
        private void HandleTabNavigation(KeyEventArgs e) {
            try {
                var focusedElement = Keyboard.FocusedElement as FrameworkElement;

                if (focusedElement == oldPasswordBox && !Keyboard.IsKeyDown(Key.LeftShift)) {
                    newPasswordBox.Focus();
                    e.Handled = true;
                } else if (focusedElement == newPasswordBox && !Keyboard.IsKeyDown(Key.LeftShift)) {
                    confirmPasswordBox.Focus();
                    e.Handled = true;
                } else if (focusedElement == confirmPasswordBox && Keyboard.IsKeyDown(Key.LeftShift)) {
                    newPasswordBox.Focus();
                    e.Handled = true;
                } else if (focusedElement == newPasswordBox && Keyboard.IsKeyDown(Key.LeftShift)) {
                    oldPasswordBox.Focus();
                    e.Handled = true;
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"HandleTabNavigation error: {ex.Message}");
            }
        }

        #endregion

        #region 视图模型事件处理

        /// <summary>
        /// 视图模型属性改变事件
        /// </summary>
        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            try {
                if (sender is not ChangePasswordViewModel viewModel)
                    return;

                switch (e.PropertyName) {
                    case nameof(ChangePasswordViewModel.ErrorMessage):
                        // 错误信息改变时的处理
                        if (!string.IsNullOrEmpty(viewModel.ErrorMessage)) {
                            ShowErrorMessage(viewModel.ErrorMessage);
                        }
                        break;

                    default:
                        // 其他属性改变时的处理
                        break;
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OnViewModelPropertyChanged error: {ex.Message}");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 验证密码强度
        /// </summary>
        private void ValidatePasswordStrength(string password) {
            try {
                if (DataContext is not ChangePasswordViewModel vm)
                    return;

                if (string.IsNullOrEmpty(password)) {
                    return;
                }

                // 基本长度检查
                if (password.Length < 6) {
                    // 可以在这里设置密码强度提示
                    return;
                }

                // 这里可以添加更复杂的密码强度验证逻辑
                // 例如：检查是否包含大小写字母、数字、特殊字符等
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ValidatePasswordStrength error: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证密码匹配
        /// </summary>
        private void ValidatePasswordMatch() {
            try {
                if (DataContext is not ChangePasswordViewModel vm)
                    return;

                if (!string.IsNullOrEmpty(vm.NewPassword) && !string.IsNullOrEmpty(vm.ConfirmPassword)) {
                    if (vm.NewPassword != vm.ConfirmPassword) {
                        // 可以在这里设置不匹配的提示
                        // vm.ErrorMessage = "两次输入的密码不一致";
                    } else {
                        // 密码匹配，清除错误信息
                        if (vm.ErrorMessage == "两次输入的密码不一致") {
                            vm.ErrorMessage = string.Empty;
                        }
                    }
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ValidatePasswordMatch error: {ex.Message}");
            }
        }

        /// <summary>
        /// 清空密码框
        /// </summary>
        private void ClearPasswordBoxes() {
            try {
                Dispatcher.BeginInvoke(new Action(() => {
                    oldPasswordBox.Clear();
                    newPasswordBox.Clear();
                    confirmPasswordBox.Clear();
                    oldPasswordBox.Focus();
                }));
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ClearPasswordBoxes error: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示错误信息
        /// </summary>
        private void ShowErrorMessage(string message) {
            try {
                // 这里可以添加错误信息的UI显示逻辑
                System.Diagnostics.Debug.WriteLine($"Error: {message}");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ShowErrorMessage error: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示成功信息
        /// </summary>
        public void ShowSuccessMessage(string message = "密码修改成功") {
            try {
                Dispatcher.BeginInvoke(new Action(() => {
                    var successBorder = FindVisualChild<Border>(this, "SuccessMessageBorder");
                    var successText = FindVisualChild<TextBlock>(this, "SuccessMessageText");

                    if (successBorder != null && successText != null) {
                        successText.Text = message;
                        successBorder.Visibility = Visibility.Visible;

                        // 3秒后自动隐藏
                        var timer = new System.Windows.Threading.DispatcherTimer {
                            Interval = TimeSpan.FromSeconds(3)
                        };
                        timer.Tick += (s, e) => {
                            timer.Stop();
                            successBorder.Visibility = Visibility.Collapsed;
                            ClearPasswordBoxes();
                        };
                        timer.Start();
                    }
                }));
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ShowSuccessMessage error: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示加载状态
        /// </summary>
        public void ShowLoadingState(bool isLoading) {
            try {
                Dispatcher.BeginInvoke(new Action(() => {
                    var loadingOverlay = FindVisualChild<Border>(this, "LoadingOverlay");

                    if (loadingOverlay != null) {
                        loadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
                    }

                    // 禁用/启用输入控件
                    oldPasswordBox.IsEnabled = !isLoading;
                    newPasswordBox.IsEnabled = !isLoading;
                    confirmPasswordBox.IsEnabled = !isLoading;
                }));
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ShowLoadingState error: {ex.Message}");
            }
        }

        /// <summary>
        /// 查找具有特定名称的子控件
        /// </summary>
        private static T FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement {
            try {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
                    var child = VisualTreeHelper.GetChild(parent, i);

                    if (child is T element && element.Name == name) {
                        return element;
                    }

                    var descendant = FindVisualChild<T>(child, name);
                    if (descendant != null) {
                        return descendant;
                    }
                }
                return null;
            } catch {
                return null;
            }
        }

        #endregion

        #region 资源清理

        /// <summary>
        /// 资源清理
        /// </summary>
        private void CleanupResources() {
            try {
                if (DataContext is ChangePasswordViewModel viewModel) {
                    viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                }

                this.Loaded -= OnViewLoaded;
                this.Unloaded -= OnUnloaded;
                this.KeyDown -= OnKeyDown;

                System.Diagnostics.Debug.WriteLine("ChangePasswordView resources cleaned up");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"CleanupResources error: {ex.Message}");
            }
        }

        /// <summary>
        /// 视图卸载时清理资源
        /// </summary>
        private void OnUnloaded(object sender, RoutedEventArgs e) {
            CleanupResources();
        }

        #endregion
    }
}