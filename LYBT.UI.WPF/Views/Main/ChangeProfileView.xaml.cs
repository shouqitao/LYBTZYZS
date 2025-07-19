using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using LYBT.UI.WPF.ViewModels.Main;

namespace LYBT.UI.WPF.Views.Main {
    /// <summary>
    /// 修改个人信息视图的交互逻辑 - 优化版
    /// </summary>
    public partial class ChangeProfileView : UserControl {
        // 验证正则表达式
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PhoneRegex = new Regex(
            @"^1[3-9]\d{9}$|^0\d{2,3}-?\d{7,8}$|^400-?\d{3}-?\d{4}$",
            RegexOptions.Compiled);

        private static readonly Regex NameRegex = new Regex(
            @"^[\u4e00-\u9fa5a-zA-Z\s]{2,20}$",
            RegexOptions.Compiled);

        public ChangeProfileView() {
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
                // 订阅视图模型事件
                if (DataContext is ChangeProfileViewModel viewModel) {
                    viewModel.PropertyChanged += OnViewModelPropertyChanged;

                    // 初始化输入验证
                    InitializeValidation(viewModel);
                }

                System.Diagnostics.Debug.WriteLine("ChangeProfileView loaded successfully");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ChangeProfileView load error: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化验证
        /// </summary>
        private void InitializeValidation(ChangeProfileViewModel viewModel) {
            try {
                // 绑定输入验证事件
                BindTextBoxValidation();

                // 执行初始验证
                ValidateAllFields(viewModel);
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"InitializeValidation error: {ex.Message}");
            }
        }

        /// <summary>
        /// 绑定文本框验证事件
        /// </summary>
        private void BindTextBoxValidation() {
            try {
                // 为每个输入框绑定失去焦点时的验证事件
                foreach (var textBox in FindVisualChildren<TextBox>(this)) {
                    textBox.LostFocus += OnTextBoxLostFocus;
                    textBox.TextChanged += OnTextBoxTextChanged;
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"BindTextBoxValidation error: {ex.Message}");
            }
        }

        #endregion

        #region 输入验证事件

        /// <summary>
        /// 文本框失去焦点时验证
        /// </summary>
        private void OnTextBoxLostFocus(object sender, RoutedEventArgs e) {
            try {
                if (sender is TextBox textBox && DataContext is ChangeProfileViewModel viewModel) {
                    var hint = MaterialDesignThemes.Wpf.HintAssist.GetHint(textBox)?.ToString();

                    switch (hint) {
                        case "真实姓名":
                            ValidateRealName(textBox.Text, viewModel);
                            break;
                        case "电子邮箱":
                            ValidateEmail(textBox.Text, viewModel);
                            break;
                        case "联系电话":
                            ValidatePhone(textBox.Text, viewModel);
                            break;
                    }
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OnTextBoxLostFocus error: {ex.Message}");
            }
        }

        /// <summary>
        /// 文本框内容改变时的实时验证
        /// </summary>
        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e) {
            try {
                if (sender is TextBox textBox && DataContext is ChangeProfileViewModel viewModel) {
                    // 延迟验证，避免输入时过于频繁的验证
                    var timer = new System.Windows.Threading.DispatcherTimer {
                        Interval = TimeSpan.FromMilliseconds(500)
                    };

                    timer.Tick += (s, args) => {
                        timer.Stop();
                        var hint = MaterialDesignThemes.Wpf.HintAssist.GetHint(textBox)?.ToString();

                        switch (hint) {
                            case "真实姓名":
                                ValidateRealName(textBox.Text, viewModel);
                                break;
                            case "电子邮箱":
                                ValidateEmail(textBox.Text, viewModel);
                                break;
                            case "联系电话":
                                ValidatePhone(textBox.Text, viewModel);
                                break;
                        }
                    };

                    timer.Start();
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OnTextBoxTextChanged error: {ex.Message}");
            }
        }

        #endregion

        #region 字段验证方法

        /// <summary>
        /// 验证真实姓名
        /// </summary>
        private void ValidateRealName(string name, ChangeProfileViewModel viewModel) {
            try {
                if (string.IsNullOrWhiteSpace(name)) {
                    SetValidationStatus("RealName", false, "未填写", "Alert");
                    return;
                }

                if (!NameRegex.IsMatch(name)) {
                    SetValidationStatus("RealName", false, "格式不正确", "AlertCircle");
                    return;
                }

                SetValidationStatus("RealName", true, "格式正确", "CheckCircle");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ValidateRealName error: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证邮箱
        /// </summary>
        private void ValidateEmail(string email, ChangeProfileViewModel viewModel) {
            try {
                if (string.IsNullOrWhiteSpace(email)) {
                    SetValidationStatus("Email", false, "未填写", "Alert");
                    return;
                }

                if (!EmailRegex.IsMatch(email)) {
                    SetValidationStatus("Email", false, "格式不正确", "AlertCircle");
                    return;
                }

                SetValidationStatus("Email", true, "格式正确", "CheckCircle");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ValidateEmail error: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证电话号码
        /// </summary>
        private void ValidatePhone(string phone, ChangeProfileViewModel viewModel) {
            try {
                if (string.IsNullOrWhiteSpace(phone)) {
                    SetValidationStatus("Phone", false, "未填写", "Alert");
                    return;
                }

                if (!PhoneRegex.IsMatch(phone)) {
                    SetValidationStatus("Phone", false, "格式不正确", "AlertCircle");
                    return;
                }

                SetValidationStatus("Phone", true, "格式正确", "CheckCircle");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ValidatePhone error: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证所有字段
        /// </summary>
        private void ValidateAllFields(ChangeProfileViewModel viewModel) {
            try {
                ValidateRealName(viewModel.RealName, viewModel);
                ValidateEmail(viewModel.Email, viewModel);
                ValidatePhone(viewModel.PhoneNumber, viewModel);
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ValidateAllFields error: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置验证状态
        /// </summary>
        private void SetValidationStatus(string field, bool isValid, string message, string iconKind) {
            try {
                Dispatcher.BeginInvoke(new Action(() => {
                    ValidationStatusExpander.Visibility = Visibility.Visible;

                    var color = isValid ? Brushes.Green : Brushes.Red;

                    switch (field) {
                        case "RealName":
                            SetValidationIconKind(RealNameValidationIcon, iconKind);
                            RealNameValidationIcon.Foreground = color;
                            RealNameValidationMessage.Text = message;
                            RealNameValidationMessage.Foreground = color;
                            break;

                        case "Email":
                            SetValidationIconKind(EmailValidationIcon, iconKind);
                            EmailValidationIcon.Foreground = color;
                            EmailValidationMessage.Text = message;
                            EmailValidationMessage.Foreground = color;
                            break;

                        case "Phone":
                            SetValidationIconKind(PhoneValidationIcon, iconKind);
                            PhoneValidationIcon.Foreground = color;
                            PhoneValidationMessage.Text = message;
                            PhoneValidationMessage.Foreground = color;
                            break;
                    }
                }));
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"SetValidationStatus error: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置验证图标
        /// </summary>
        private void SetValidationIconKind(PackIcon icon, string kindName) {
            try {
                if (Enum.TryParse<PackIconKind>(kindName, out var kind)) {
                    icon.Kind = kind;
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"SetValidationIconKind error: {ex.Message}");
            }
        }

        #endregion

        #region 键盘事件处理

        /// <summary>
        /// 键盘按键事件
        /// </summary>
        private void OnKeyDown(object sender, KeyEventArgs e) {
            try {
                if (DataContext is not ChangeProfileViewModel viewModel)
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

                    case Key.F5:
                        // F5键重新验证所有字段
                        ValidateAllFields(viewModel);
                        e.Handled = true;
                        break;
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OnKeyDown error: {ex.Message}");
            }
        }

        #endregion

        #region 视图模型事件处理

        /// <summary>
        /// 视图模型属性改变事件
        /// </summary>
        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            try {
                if (sender is not ChangeProfileViewModel viewModel)
                    return;

                switch (e.PropertyName) {
                    case nameof(ChangeProfileViewModel.RealName):
                        ValidateRealName(viewModel.RealName, viewModel);
                        break;

                    case nameof(ChangeProfileViewModel.Email):
                        ValidateEmail(viewModel.Email, viewModel);
                        break;

                    case nameof(ChangeProfileViewModel.PhoneNumber):
                        ValidatePhone(viewModel.PhoneNumber, viewModel);
                        break;

                    case nameof(ChangeProfileViewModel.ErrorMessage):
                        // 错误信息改变时的处理
                        break;
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OnViewModelPropertyChanged error: {ex.Message}");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 显示成功通知
        /// </summary>
        public void ShowSuccessMessage(string message = "个人信息保存成功") {
            try {
                Dispatcher.BeginInvoke(new Action(() => {
                    SuccessMessageText.Text = message;
                    SuccessMessageBorder.Visibility = Visibility.Visible;

                    // 3秒后自动隐藏
                    var timer = new System.Windows.Threading.DispatcherTimer {
                        Interval = TimeSpan.FromSeconds(3)
                    };
                    timer.Tick += (s, e) => {
                        timer.Stop();
                        SuccessMessageBorder.Visibility = Visibility.Collapsed;
                    };
                    timer.Start();
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
                    LoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;

                    // 禁用/启用输入控件
                    foreach (var textBox in FindVisualChildren<TextBox>(this)) {
                        textBox.IsEnabled = !isLoading;
                    }
                }));
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ShowLoadingState error: {ex.Message}");
            }
        }

        /// <summary>
        /// 查找子控件
        /// </summary>
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject {
            try {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    if (child is T result) {
                        return result;
                    }

                    var descendant = FindVisualChild<T>(child);
                    if (descendant != null) {
                        return descendant;
                    }
                }
                return null;
            } catch {
                return null;
            }
        }

        /// <summary>
        /// 查找所有子控件
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result) {
                    yield return result;
                }

                foreach (var descendant in FindVisualChildren<T>(child)) {
                    yield return descendant;
                }
            }
        }

        #endregion

        #region 资源清理

        /// <summary>
        /// 资源清理
        /// </summary>
        private void CleanupResources() {
            try {
                if (DataContext is ChangeProfileViewModel viewModel) {
                    viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                }

                // 清理事件绑定
                foreach (var textBox in FindVisualChildren<TextBox>(this)) {
                    textBox.LostFocus -= OnTextBoxLostFocus;
                    textBox.TextChanged -= OnTextBoxTextChanged;
                }

                this.Loaded -= OnViewLoaded;
                this.Unloaded -= OnUnloaded;
                this.KeyDown -= OnKeyDown;

                System.Diagnostics.Debug.WriteLine("ChangeProfileView resources cleaned up");
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