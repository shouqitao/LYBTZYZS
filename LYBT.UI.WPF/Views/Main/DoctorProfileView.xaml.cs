using LYBT.UI.WPF.ViewModels.Main;
using LYBT.UI.WPF.ViewModels.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LYBT.UI.WPF.Views.Main {
    /// <summary>
    /// 医生档案视图的交互逻辑 - 优化版
    /// </summary>
    public partial class DoctorProfileView : UserControl {
        public DoctorProfileView() {
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
                if (DataContext is DoctorProfileViewModel viewModel) {
                    viewModel.PropertyChanged += OnViewModelPropertyChanged;

                    // 初始化焦点设置
                    SetInitialFocus(viewModel);
                }

                System.Diagnostics.Debug.WriteLine("DoctorProfileView loaded successfully");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"DoctorProfileView load error: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置初始焦点
        /// </summary>
        private void SetInitialFocus(DoctorProfileViewModel viewModel) {
            try {
                if (viewModel.IsEditable) {
                    // 编辑模式：聚焦到第一个可编辑的控件
                    Dispatcher.BeginInvoke(new Action(() => {
                        var firstEditableControl = FindFirstEditableControl();
                        firstEditableControl?.Focus();
                    }));
                } else {
                    // 只读模式：聚焦到返回按钮或滚动区域
                    Dispatcher.BeginInvoke(new Action(() => {
                        var scrollViewer = FindVisualChild<ScrollViewer>(this);
                        scrollViewer?.Focus();
                    }));
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"SetInitialFocus error: {ex.Message}");
            }
        }

        /// <summary>
        /// 查找第一个可编辑的控件
        /// </summary>
        private Control FindFirstEditableControl() {
            try {
                // 按照界面布局顺序查找可编辑控件
                foreach (var textBox in FindVisualChildren<TextBox>(this)) {
                    if (!textBox.IsReadOnly && textBox.IsEnabled) {
                        return textBox;
                    }
                }

                foreach (var comboBox in FindVisualChildren<ComboBox>(this)) {
                    if (comboBox.IsEnabled) {
                        return comboBox;
                    }
                }

                foreach (var datePicker in FindVisualChildren<DatePicker>(this)) {
                    if (datePicker.IsEnabled) {
                        return datePicker;
                    }
                }

                return null;
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"FindFirstEditableControl error: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region 键盘事件处理

        /// <summary>
        /// 键盘按键事件
        /// </summary>
        private void OnKeyDown(object sender, KeyEventArgs e) {
            try {
                if (DataContext is not DoctorProfileViewModel viewModel)
                    return;

                switch (e.Key) {
                    case Key.Enter:
                        // 回车键处理
                        HandleEnterKey(viewModel, e);
                        break;

                    case Key.Escape:
                        // ESC键取消或返回
                        if (viewModel.CancelCommand?.CanExecute() == true) {
                            viewModel.CancelCommand.Execute();
                            e.Handled = true;
                        }
                        break;

                    case Key.F2:
                        // F2键切换编辑模式（如果支持）
                        ToggleEditMode(viewModel);
                        e.Handled = true;
                        break;

                    case Key.F5:
                        // F5键刷新数据
                        RefreshData(viewModel);
                        e.Handled = true;
                        break;

                    case Key.Tab:
                        // Tab键优化焦点切换
                        HandleTabNavigation(e);
                        break;
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OnKeyDown error: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理回车键
        /// </summary>
        private void HandleEnterKey(DoctorProfileViewModel viewModel, KeyEventArgs e) {
            try {
                if (viewModel.IsEditable) {
                    // 编辑模式：回车保存
                    if (viewModel.SaveCommand?.CanExecute() == true) {
                        viewModel.SaveCommand.Execute();
                        e.Handled = true;
                    }
                } else {
                    // 只读模式：回车返回
                    if (viewModel.CancelCommand?.CanExecute() == true) {
                        viewModel.CancelCommand.Execute();
                        e.Handled = true;
                    }
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"HandleEnterKey error: {ex.Message}");
            }
        }

        /// <summary>
        /// 切换编辑模式
        /// </summary>
        private void ToggleEditMode(DoctorProfileViewModel viewModel) {
            try {
                // 这里可以添加切换编辑模式的逻辑
                // 如果ViewModel支持动态切换编辑模式
                var toggleCommand = viewModel.GetType().GetProperty("ToggleEditModeCommand")?.GetValue(viewModel);
                if (toggleCommand is ICommand command && command.CanExecute(null)) {
                    command.Execute(null);
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ToggleEditMode error: {ex.Message}");
            }
        }

        /// <summary>
        /// 刷新数据
        /// </summary>
        private void RefreshData(DoctorProfileViewModel viewModel) {
            try {
                // 刷新医生数据
                var refreshCommand = viewModel.GetType().GetProperty("RefreshCommand")?.GetValue(viewModel);
                if (refreshCommand is ICommand command && command.CanExecute(null)) {
                    command.Execute(null);
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"RefreshData error: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理Tab键导航
        /// </summary>
        private void HandleTabNavigation(KeyEventArgs e) {
            try {
                var focusedElement = Keyboard.FocusedElement as FrameworkElement;

                // 在这里可以自定义Tab键的焦点切换顺序
                // 确保按照逻辑顺序在控件间切换
                if (focusedElement != null) {
                    var isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                    if (isShiftPressed) {
                        // Shift+Tab: 反向切换
                        focusedElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
                    } else {
                        // Tab: 正向切换
                        focusedElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    }

                    e.Handled = true;
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"HandleTabNavigation error: {ex.Message}");
            }
        }

        #endregion

        #region 视图模型事件处理

        #region 视图模型事件处理

        /// <summary>
        /// 视图模型属性改变事件
        /// </summary>
        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            try {
                if (sender is not DoctorProfileViewModel viewModel)
                    return;

                switch (e.PropertyName) {
                    case nameof(DoctorProfileViewModel.IsEditable):
                        OnEditModeChanged(viewModel.IsEditable);
                        break;

                    case "Doctor":
                        OnDoctorDataChanged();
                        break;

                    default:
                        // 其他属性改变时的处理
                        break;
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OnViewModelPropertyChanged error: {ex.Message}");
            }
        }

        /// <summary>
        /// 编辑模式改变时的处理
        /// </summary>
        private void OnEditModeChanged(bool isEditable) {
            try {
                if (isEditable) {
                    // 进入编辑模式
                    Dispatcher.BeginInvoke(new Action(() => {
                        var firstEditableControl = FindFirstEditableControl();
                        firstEditableControl?.Focus();
                    }));
                } else {
                    // 退出编辑模式
                    Dispatcher.BeginInvoke(new Action(() => {
                        var scrollViewer = FindVisualChild<ScrollViewer>(this);
                        scrollViewer?.Focus();
                    }));
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OnEditModeChanged error: {ex.Message}");
            }
        }

        /// <summary>
        /// 数据保存成功后的处理
        /// </summary>
        public void ShowSuccessMessage(string message = "医生信息保存成功") {
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
        public void ShowLoadingState(bool isLoading, string message = "正在处理...") {
            try {
                Dispatcher.BeginInvoke(new Action(() => {
                    LoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
                    LoadingMessageText.Text = message;

                    // 禁用/启用输入控件
                    foreach (var textBox in FindVisualChildren<TextBox>(this)) {
                        textBox.IsEnabled = !isLoading;
                    }
                    foreach (var comboBox in FindVisualChildren<ComboBox>(this)) {
                        comboBox.IsEnabled = !isLoading;
                    }
                    foreach (var datePicker in FindVisualChildren<DatePicker>(this)) {
                        datePicker.IsEnabled = !isLoading;
                    }
                }));
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ShowLoadingState error: {ex.Message}");
            }
        }

        /// <summary>
        /// 医生数据改变时的处理
        /// </summary>
        private void OnDoctorDataChanged() {
            try {
                // 这里可以添加数据改变时的UI更新逻辑
                // 例如：重新验证数据、更新相关显示等
                System.Diagnostics.Debug.WriteLine("Doctor data changed");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OnDoctorDataChanged error: {ex.Message}");
            }
        }

        #endregion

        #region 辅助方法

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
            // catch块已移除，异常由调用方处理
        }

        /// <summary>
        /// 验证输入数据
        /// </summary>
        private bool ValidateInputData() {
            try {
                if (DataContext is not DoctorProfileViewModel viewModel)
                    return false;

                // 基本数据验证
                if (viewModel.Doctor == null)
                    return false;

                // 这里可以添加更多的数据验证逻辑
                // 例如：检查必填字段、格式验证等

                return true;
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ValidateInputData error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        private bool ShowConfirmDialog(string message, string title = "确认") {
            try {
                var result = MessageBox.Show(
                    message,
                    title,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                return result == MessageBoxResult.Yes;
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ShowConfirmDialog error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 资源清理

        /// <summary>
        /// 资源清理
        /// </summary>
        private void CleanupResources() {
            try {
                if (DataContext is DoctorProfileViewModel viewModel) {
                    viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                }

                this.Loaded -= OnViewLoaded;
                this.Unloaded -= OnUnloaded;
                this.KeyDown -= OnKeyDown;

                System.Diagnostics.Debug.WriteLine("DoctorProfileView resources cleaned up");
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

        #endregion // 资源清理

        #endregion
    }
}