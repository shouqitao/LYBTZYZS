using MaterialDesignThemes.Wpf;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using LYBT.UI.WPF.ViewModels.Main;

namespace LYBT.UI.WPF.Views.Main {
    /// <summary>
    /// 重构后的主窗口交互逻辑 - 增强窗口管理和用户体验
    /// </summary>
    public partial class MainWindow : Window {
        private bool _isMaximized = false;
        private double _restoreLeft;
        private double _restoreTop;
        private double _restoreWidth;
        private double _restoreHeight;

        public MainWindow() {
            InitializeComponent();
            InitializeWindow();
        }

        #region Window Initialization

        /// <summary>
        /// 初始化窗口
        /// </summary>
        private void InitializeWindow() {
            // 设置窗口事件
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
            this.StateChanged += MainWindow_StateChanged;
            this.KeyDown += MainWindow_KeyDown;

            // 设置窗口属性
            this.MinWidth = 800;
            this.MinHeight = 600;

            // 启用窗口拖拽
            this.MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;

            System.Diagnostics.Debug.WriteLine("MainWindow initialized with enhanced features");
        }

        #endregion

        #region Window Events

        /// <summary>
        /// 窗口加载完成事件
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            try {
                // 初始窗口状态
                if (this.WindowState == WindowState.Maximized) {
                    _isMaximized = true;
                    UpdateMaximizeIcon();
                }

                System.Diagnostics.Debug.WriteLine("MainWindow loaded successfully");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"MainWindow_Loaded error: {ex.Message}");
            }
        }

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        private void MainWindow_Closing(object sender, CancelEventArgs e) {
            try {
                // 确认退出
                var result = MessageBox.Show(
                    "确定要关闭凌隐宝堂中医诊所管理系统吗？",
                    "退出确认",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                if (result == MessageBoxResult.No) {
                    e.Cancel = true;
                    return;
                }

                // 清理资源
                CleanupResources();

                System.Diagnostics.Debug.WriteLine("MainWindow closing confirmed");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"MainWindow_Closing error: {ex.Message}");
            }
        }

        /// <summary>
        /// 窗口状态改变事件
        /// </summary>
        private void MainWindow_StateChanged(object sender, EventArgs e) {
            try {
                switch (this.WindowState) {
                    case WindowState.Maximized:
                        _isMaximized = true;
                        break;
                    case WindowState.Normal:
                        _isMaximized = false;
                        break;
                }
                UpdateMaximizeIcon();
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"MainWindow_StateChanged error: {ex.Message}");
            }
        }

        /// <summary>
        /// 窗口键盘事件
        /// </summary>
        private void MainWindow_KeyDown(object sender, KeyEventArgs e) {
            try {
                // 全局快捷键处理
                if (Keyboard.Modifiers == ModifierKeys.Alt) {
                    switch (e.Key) {
                        case Key.F4:
                            // Alt+F4 关闭窗口
                            this.Close();
                            e.Handled = true;
                            break;
                    }
                } else if (Keyboard.Modifiers == ModifierKeys.Control) {
                    switch (e.Key) {
                        case Key.M:
                            // Ctrl+M 切换导航菜单
                            if (DataContext is MainWindowViewModel viewModel) {
                                viewModel.ToggleNavDrawerCommand?.Execute();
                            }
                            e.Handled = true;
                            break;
                        case Key.T:
                            // Ctrl+T 切换主题
                            if (DataContext is MainWindowViewModel vm) {
                                vm.ToggleThemeCommand?.Execute();
                            }
                            e.Handled = true;
                            break;
                    }
                } else {
                    switch (e.Key) {
                        case Key.F11:
                            // F11 全屏切换
                            ToggleFullScreen();
                            e.Handled = true;
                            break;
                        case Key.Escape:
                            // ESC 键关闭导航菜单
                            if (DataContext is MainWindowViewModel viewModel && viewModel.IsNavDrawerOpen) {
                                viewModel.IsNavDrawerOpen = false;
                                e.Handled = true;
                            }
                            break;
                    }
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"MainWindow_KeyDown error: {ex.Message}");
            }
        }

        /// <summary>
        /// 窗口拖拽事件
        /// </summary>
        private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            try {
                // 双击标题栏最大化/还原
                if (e.ClickCount == 2) {
                    ToggleMaximize();
                    return;
                }

                // 拖拽窗口
                if (e.ButtonState == MouseButtonState.Pressed) {
                    this.DragMove();
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"MainWindow_MouseLeftButtonDown error: {ex.Message}");
            }
        }

        #endregion

        #region Title Bar Button Events

        /// <summary>
        /// 最小化按钮点击事件
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) {
            try {
                this.WindowState = WindowState.Minimized;
                System.Diagnostics.Debug.WriteLine("Window minimized");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"MinimizeButton_Click error: {ex.Message}");
            }
        }

        /// <summary>
        /// 最大化/还原按钮点击事件
        /// </summary>
        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e) {
            ToggleMaximize();
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 切换最大化状态
        /// </summary>
        private void ToggleMaximize() {
            try {
                if (_isMaximized) {
                    // 还原窗口
                    this.WindowState = WindowState.Normal;
                    if (_restoreWidth > 0 && _restoreHeight > 0) {
                        this.Left = _restoreLeft;
                        this.Top = _restoreTop;
                        this.Width = _restoreWidth;
                        this.Height = _restoreHeight;
                    }
                    _isMaximized = false;
                    System.Diagnostics.Debug.WriteLine("Window restored");
                } else {
                    // 保存当前位置和大小
                    _restoreLeft = this.Left;
                    _restoreTop = this.Top;
                    _restoreWidth = this.Width;
                    _restoreHeight = this.Height;

                    // 最大化窗口
                    this.WindowState = WindowState.Maximized;
                    _isMaximized = true;
                    System.Diagnostics.Debug.WriteLine("Window maximized");
                }

                UpdateMaximizeIcon();
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ToggleMaximize error: {ex.Message}");
            }
        }

        /// <summary>
        /// 切换全屏模式
        /// </summary>
        private void ToggleFullScreen() {
            try {
                if (this.WindowStyle == WindowStyle.None && this.WindowState == WindowState.Maximized) {
                    // 退出全屏
                    this.WindowStyle = WindowStyle.None;
                    this.WindowState = WindowState.Normal;
                    System.Diagnostics.Debug.WriteLine("Exited full screen mode");
                } else {
                    // 进入全屏
                    this.WindowStyle = WindowStyle.None;
                    this.WindowState = WindowState.Maximized;
                    System.Diagnostics.Debug.WriteLine("Entered full screen mode");
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ToggleFullScreen error: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新最大化图标
        /// </summary>
        private void UpdateMaximizeIcon() {
            try {
                if (MaximizeIcon != null) {
                    MaximizeIcon.Kind = _isMaximized ? PackIconKind.WindowRestore : PackIconKind.WindowMaximize;
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"UpdateMaximizeIcon error: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void CleanupResources() {
            try {
                if (DataContext is MainWindowViewModel viewModel) {
                    viewModel.Cleanup();
                }
                System.Diagnostics.Debug.WriteLine("MainWindow resources cleaned up");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"CleanupResources error: {ex.Message}");
            }
        }

        #endregion
    }
}