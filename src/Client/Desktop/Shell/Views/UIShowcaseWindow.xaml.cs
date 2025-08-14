using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace LYBT.Desktop.Shell.Views
{
    /// <summary>
    /// UIShowcaseWindow.xaml 的交互逻辑
    /// </summary>
    public partial class UIShowcaseWindow : Window
    {
        public UIShowcaseWindow()
        {
            InitializeComponent();
            
            // 窗口加载动画
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // 淡入动画
            var fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut }
            };
            
            BeginAnimation(OpacityProperty, fadeInAnimation);
        }
    }
}