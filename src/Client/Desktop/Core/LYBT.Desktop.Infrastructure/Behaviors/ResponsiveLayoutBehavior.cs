using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Infrastructure.Behaviors;

public static class ResponsiveLayoutBehavior
{
    public static readonly DependencyProperty BreakpointWidthProperty =
        DependencyProperty.RegisterAttached(
            "BreakpointWidth",
            typeof(double),
            typeof(ResponsiveLayoutBehavior),
            new PropertyMetadata(800.0, OnBreakpointWidthChanged));

    public static readonly DependencyProperty CollapsedElementProperty =
        DependencyProperty.RegisterAttached(
            "CollapsedElement",
            typeof(FrameworkElement),
            typeof(ResponsiveLayoutBehavior),
            new PropertyMetadata(null));

    public static double GetBreakpointWidth(DependencyObject obj) =>
        (double)obj.GetValue(BreakpointWidthProperty);

    public static void SetBreakpointWidth(DependencyObject obj, double value) =>
        obj.SetValue(BreakpointWidthProperty, value);

    public static FrameworkElement GetCollapsedElement(DependencyObject obj) =>
        (FrameworkElement)obj.GetValue(CollapsedElementProperty);

    public static void SetCollapsedElement(DependencyObject obj, FrameworkElement value) =>
        obj.SetValue(CollapsedElementProperty, value);

    private static void OnBreakpointWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            element.SizeChanged -= OnSizeChanged;
            element.SizeChanged += OnSizeChanged;
        }
    }

    private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not FrameworkElement element) return;

        var breakpoint = GetBreakpointWidth(element);
        var target = GetCollapsedElement(element);
        if (target == null) return;

        target.Visibility = e.NewSize.Width < breakpoint
            ? Visibility.Collapsed
            : Visibility.Visible;
    }
}
