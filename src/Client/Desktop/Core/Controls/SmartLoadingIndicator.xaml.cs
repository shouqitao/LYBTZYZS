using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LYBT.Desktop.Core.Services;

namespace LYBT.Desktop.Core.Controls
{

    /// <summary>
    /// 智能加载指示器控件
    ///
    /// 功能特性：
    /// 1. 自动绑定SmartLoadingManager状态
    /// 2. 支持分层加载显示
    /// 3. 现代化UI设计
    /// 4. 进度跟踪和取消支持
    /// </summary>
    public partial class SmartLoadingIndicator : UserControl, INotifyPropertyChanged
    {

        #region 依赖属性

        /// <summary>
        /// 关联的SmartLoadingManager
        /// </summary>
        public static readonly DependencyProperty LoadingManagerProperty =
            DependencyProperty.Register(nameof(LoadingManager), typeof(ISmartLoadingManager),
                typeof(SmartLoadingIndicator), new PropertyMetadata(null, OnLoadingManagerChanged));

        /// <summary>
        /// 监听的加载层级
        /// </summary>
        public static readonly DependencyProperty LayerProperty =
            DependencyProperty.Register(nameof(Layer), typeof(int),
                typeof(SmartLoadingIndicator), new PropertyMetadata(1, OnLayerChanged));

        /// <summary>
        /// 是否显示取消按钮
        /// </summary>
        public static readonly DependencyProperty ShowCancelButtonProperty =
            DependencyProperty.Register(nameof(ShowCancelButton), typeof(bool),
                typeof(SmartLoadingIndicator), new PropertyMetadata(false));

        /// <summary>
        /// 自定义加载消息
        /// </summary>
        public static readonly DependencyProperty CustomMessageProperty =
            DependencyProperty.Register(nameof(CustomMessage), typeof(string),
                typeof(SmartLoadingIndicator), new PropertyMetadata(null, OnCustomMessageChanged));

        /// <summary>
        /// 取消命令 - UltraThink Command绑定优化
        /// </summary>
        public static readonly DependencyProperty CancelCommandProperty =
            DependencyProperty.Register(nameof(CancelCommand), typeof(ICommand),
                typeof(SmartLoadingIndicator), new PropertyMetadata(null));

        #endregion 依赖属性

        #region 私有字段

        private bool _isVisible;
        private string _loadingMessage = "加载中...";
        private int _progressValue;
        private bool _showProgress;
        private string _progressText = string.Empty;

        #endregion 私有字段

        #region 构造函数

        public SmartLoadingIndicator()
        {
            InitializeComponent();
            DataContext = this;
        }

        #endregion 构造函数

        #region 公共属性

        public ISmartLoadingManager? LoadingManager
        {
            get => (ISmartLoadingManager)GetValue(LoadingManagerProperty);
            set => SetValue(LoadingManagerProperty, value);
        }

        public int Layer
        {
            get => (int)GetValue(LayerProperty);
            set => SetValue(LayerProperty, value);
        }

        public bool ShowCancelButton
        {
            get => (bool)GetValue(ShowCancelButtonProperty);
            set => SetValue(ShowCancelButtonProperty, value);
        }

        public string? CustomMessage
        {
            get => (string?)GetValue(CustomMessageProperty);
            set => SetValue(CustomMessageProperty, value);
        }

        /// <summary>
        /// 取消命令 - UltraThink Command绑定优化
        /// </summary>
        public ICommand? CancelCommand
        {
            get => (ICommand?)GetValue(CancelCommandProperty);
            set => SetValue(CancelCommandProperty, value);
        }

        // 绑定属性
        public bool IsLoadingVisible
        {
            get => _isVisible;
            private set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LoadingMessage
        {
            get => _loadingMessage;
            private set
            {
                if (_loadingMessage != value)
                {
                    _loadingMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ProgressValue
        {
            get => _progressValue;
            private set
            {
                if (_progressValue != value)
                {
                    _progressValue = value;
                    OnPropertyChanged();
                    ProgressText = $"{value}%";
                }
            }
        }

        public bool ShowProgress
        {
            get => _showProgress;
            private set
            {
                if (_showProgress != value)
                {
                    _showProgress = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ProgressText
        {
            get => _progressText;
            private set
            {
                if (_progressText != value)
                {
                    _progressText = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion 公共属性

        #region 私有方法

        private static void OnLoadingManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SmartLoadingIndicator indicator)
            {
                if (e.OldValue is ISmartLoadingManager oldManager)
                {
                    oldManager.PropertyChanged -= indicator.LoadingManager_PropertyChanged;
                }

                if (e.NewValue is ISmartLoadingManager newManager)
                {
                    newManager.PropertyChanged += indicator.LoadingManager_PropertyChanged;
                    indicator.UpdateLoadingState();
                }
            }
        }

        private static void OnLayerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SmartLoadingIndicator indicator)
            {
                indicator.UpdateLoadingState();
            }
        }

        private static void OnCustomMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SmartLoadingIndicator indicator)
            {
                indicator.UpdateLoadingMessage();
            }
        }

        private void LoadingManager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISmartLoadingManager.IsGlobalLoading) ||
                e.PropertyName == nameof(ISmartLoadingManager.ActiveLoadingCount))
            {
                Dispatcher.BeginInvoke(new Action(UpdateLoadingState));
            }
        }

        private void UpdateLoadingState()
        {
            if (LoadingManager == null)
            {
                IsLoadingVisible = false;
                return;
            }

            var isLayerLoading = LoadingManager.IsLoadingAtLayer(Layer);
            IsLoadingVisible = isLayerLoading;

            if (isLayerLoading)
            {
                UpdateLoadingMessage();
                // 这里可以扩展，获取当前操作的进度信息
                // ShowProgress = currentOperation?.SupportsProgress ?? false;
                // ProgressValue = currentOperation?.Progress ?? 0;
            }
        }

        private void UpdateLoadingMessage()
        {
            if (LoadingManager == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(CustomMessage))
            {
                LoadingMessage = CustomMessage;
            }
            else
            {
                var message = LoadingManager.GetCurrentLoadingMessage(Layer);
                LoadingMessage = string.IsNullOrEmpty(message) ? "加载中..." : message;
            }
        }

        #endregion 私有方法

        #region 事件处理

        /// <summary>
        /// 取消按钮点击事件 - UltraThink Command绑定优化
        /// 优先使用Command绑定，如果没有则执行默认逻辑
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // 优先使用Command绑定
            if (CancelCommand?.CanExecute(null) == true)
            {
                CancelCommand.Execute(null);
            }
            else
            {
                // 如果没有Command绑定，执行默认逻辑（保持向后兼容）
                LoadingManager?.CancelAllOperations();
            }
        }

        #endregion 事件处理

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}

namespace LYBT.Desktop.Core.Controls
{

    /// <summary>
    /// 布尔值到可见性转换器
    /// </summary>
    public class BooleanToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public static readonly BooleanToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is Visibility.Visible;
        }
    }

    /// <summary>
    /// 进度值到缩放比例转换器
    /// </summary>
    public class ProgressToScaleConverter : System.Windows.Data.IValueConverter
    {
        public static readonly ProgressToScaleConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                return Math.Max(0, Math.Min(1, doubleValue / 100.0));
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
