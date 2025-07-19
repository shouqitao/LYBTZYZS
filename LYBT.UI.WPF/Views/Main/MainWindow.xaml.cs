using System;
using System.ComponentModel;
using System.Windows;
using LYBT.UI.WPF.ViewModels.Main;

namespace LYBT.UI.WPF.Views.Main {
    /// <summary>
    /// 简化后的主窗口交互逻辑 - 移除自定义标题栏相关功能
    /// </summary>
    public partial class MainWindow : Window {

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

            // 设置窗口属性
            this.MinWidth = 800;
            this.MinHeight = 600;

            System.Diagnostics.Debug.WriteLine("MainWindow initialized (simplified version without custom title bar)");
        }

        #endregion

        #region Window Events

        /// <summary>
        /// 窗口加载完成事件
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            try {
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

        #endregion

        #region Private Methods

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