using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LYBT.Desktop.Core.Behaviors
{
    /// <summary>
    /// 动画行为附加属性
    /// </summary>
    public static class AnimationBehaviors
    {
        #region 淡入动画

        public static readonly DependencyProperty EnableFadeInProperty =
            DependencyProperty.RegisterAttached(
                "EnableFadeIn",
                typeof(bool),
                typeof(AnimationBehaviors),
                new PropertyMetadata(false, OnEnableFadeInChanged));

        public static bool GetEnableFadeIn(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableFadeInProperty);
        }

        public static void SetEnableFadeIn(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableFadeInProperty, value);
        }

        private static void OnEnableFadeInChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element && (bool)e.NewValue)
            {
                element.Loaded += (s, args) =>
                {
                    var animation = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    element.BeginAnimation(UIElement.OpacityProperty, animation);
                };
            }
        }

        #endregion

        #region 滑入动画

        public static readonly DependencyProperty EnableSlideInProperty =
            DependencyProperty.RegisterAttached(
                "EnableSlideIn",
                typeof(bool),
                typeof(AnimationBehaviors),
                new PropertyMetadata(false, OnEnableSlideInChanged));

        public static bool GetEnableSlideIn(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableSlideInProperty);
        }

        public static void SetEnableSlideIn(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableSlideInProperty, value);
        }

        private static void OnEnableSlideInChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element && (bool)e.NewValue)
            {
                element.Loaded += (s, args) =>
                {
                    var transform = new TranslateTransform();
                    element.RenderTransform = transform;

                    var animation = new DoubleAnimation
                    {
                        From = 30,
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(400),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };

                    var opacityAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(400)
                    };

                    transform.BeginAnimation(TranslateTransform.YProperty, animation);
                    element.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
                };
            }
        }

        #endregion

        #region 鼠标悬停缩放

        public static readonly DependencyProperty EnableHoverScaleProperty =
            DependencyProperty.RegisterAttached(
                "EnableHoverScale",
                typeof(bool),
                typeof(AnimationBehaviors),
                new PropertyMetadata(false, OnEnableHoverScaleChanged));

        public static bool GetEnableHoverScale(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableHoverScaleProperty);
        }

        public static void SetEnableHoverScale(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableHoverScaleProperty, value);
        }

        private static void OnEnableHoverScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element && (bool)e.NewValue)
            {
                var transform = new ScaleTransform(1, 1);
                element.RenderTransformOrigin = new Point(0.5, 0.5);
                element.RenderTransform = transform;

                element.MouseEnter += (s, args) =>
                {
                    var scaleAnimation = new DoubleAnimation
                    {
                        To = 1.05,
                        Duration = TimeSpan.FromMilliseconds(200),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    transform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                    transform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                };

                element.MouseLeave += (s, args) =>
                {
                    var scaleAnimation = new DoubleAnimation
                    {
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(200),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    transform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                    transform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                };
            }
        }

        #endregion

        #region 点击波纹效果

        public static readonly DependencyProperty EnableRippleEffectProperty =
            DependencyProperty.RegisterAttached(
                "EnableRippleEffect",
                typeof(bool),
                typeof(AnimationBehaviors),
                new PropertyMetadata(false, OnEnableRippleEffectChanged));

        public static bool GetEnableRippleEffect(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableRippleEffectProperty);
        }

        public static void SetEnableRippleEffect(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableRippleEffectProperty, value);
        }

        private static void OnEnableRippleEffectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element && (bool)e.NewValue)
            {
                element.PreviewMouseLeftButtonDown += CreateRippleEffect;
            }
        }

        private static void CreateRippleEffect(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 波纹效果实现（简化版）
            if (sender is FrameworkElement element)
            {
                var transform = element.RenderTransform as ScaleTransform;
                if (transform == null)
                {
                    transform = new ScaleTransform(1, 1);
                    element.RenderTransformOrigin = new Point(0.5, 0.5);
                    element.RenderTransform = transform;
                }

                var animation = new DoubleAnimation
                {
                    From = 0.95,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut }
                };

                transform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
            }
        }

        #endregion

        #region 延迟加载动画

        public static readonly DependencyProperty LoadDelayProperty =
            DependencyProperty.RegisterAttached(
                "LoadDelay",
                typeof(int),
                typeof(AnimationBehaviors),
                new PropertyMetadata(0));

        public static int GetLoadDelay(DependencyObject obj)
        {
            return (int)obj.GetValue(LoadDelayProperty);
        }

        public static void SetLoadDelay(DependencyObject obj, int value)
        {
            obj.SetValue(LoadDelayProperty, value);
        }

        public static readonly DependencyProperty EnableStaggeredLoadProperty =
            DependencyProperty.RegisterAttached(
                "EnableStaggeredLoad",
                typeof(bool),
                typeof(AnimationBehaviors),
                new PropertyMetadata(false, OnEnableStaggeredLoadChanged));

        public static bool GetEnableStaggeredLoad(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableStaggeredLoadProperty);
        }

        public static void SetEnableStaggeredLoad(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableStaggeredLoadProperty, value);
        }

        private static void OnEnableStaggeredLoadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element && (bool)e.NewValue)
            {
                element.Opacity = 0;
                element.Loaded += async (s, args) =>
                {
                    var delay = GetLoadDelay(element);
                    if (delay > 0)
                    {
                        await System.Threading.Tasks.Task.Delay(delay);
                    }

                    var transform = new TranslateTransform();
                    element.RenderTransform = transform;

                    var slideAnimation = new DoubleAnimation
                    {
                        From = 20,
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };

                    var fadeAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(300)
                    };

                    transform.BeginAnimation(TranslateTransform.YProperty, slideAnimation);
                    element.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
                };
            }
        }

        #endregion
    }
}