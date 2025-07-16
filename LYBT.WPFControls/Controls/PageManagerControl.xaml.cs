using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.WPFControls {
    /// <summary>
    /// Paging control supporting MVVM binding.
    /// </summary>
    public partial class PageManagerControl : UserControl {
        public PageManagerControl() {
            InitializeComponent();
            FirstPageCommand = new SimpleCommand(() => PageIndex = 1, () => PageIndex > 1);
            PrevPageCommand = new SimpleCommand(() => PageIndex -= 1, () => PageIndex > 1);
            NextPageCommand = new SimpleCommand(() => PageIndex += 1, () => PageIndex < TotalPages);
            LastPageCommand = new SimpleCommand(() => PageIndex = TotalPages, () => PageIndex < TotalPages);
        }

        public event EventHandler? PagingChanged;

        public ICommand FirstPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand LastPageCommand { get; }

        public IEnumerable<int> PageSizeOptions { get; } = new[] { 5, 10, 20, 50, 100 };

        public int PageIndex {
            get => (int)GetValue(PageIndexProperty);
            set => SetValue(PageIndexProperty, value);
        }

        public static readonly DependencyProperty PageIndexProperty =
            DependencyProperty.Register(
                nameof(PageIndex), typeof(int), typeof(PageManagerControl),
                new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPagingPropertyChanged));

        public int PageSize {
            get => (int)GetValue(PageSizeProperty);
            set => SetValue(PageSizeProperty, value);
        }

        public static readonly DependencyProperty PageSizeProperty =
            DependencyProperty.Register(
                nameof(PageSize), typeof(int), typeof(PageManagerControl),
                new FrameworkPropertyMetadata(10, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPagingPropertyChanged));

        public int TotalCount {
            get => (int)GetValue(TotalCountProperty);
            set => SetValue(TotalCountProperty, value);
        }

        public static readonly DependencyProperty TotalCountProperty =
            DependencyProperty.Register(
                nameof(TotalCount), typeof(int), typeof(PageManagerControl),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPagingPropertyChanged));

        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

        public string RangeText {
            get {
                if (TotalCount == 0) return "第 0-0 条，共 0 条";
                var start = (PageIndex - 1) * PageSize + 1;
                var end = Math.Min(PageIndex * PageSize, TotalCount);
                return $"第 {start}-{end} 条，共 {TotalCount} 条";
            }
        }

        private static void OnPagingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is PageManagerControl c) {
                c.CommandManagerInvalidate();
                c.PagingChanged?.Invoke(c, EventArgs.Empty);
            }
        }

        private void CommandManagerInvalidate() => CommandManager.InvalidateRequerySuggested();

        private void PageBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                var expr = ((TextBox)sender).GetBindingExpression(TextBox.TextProperty);
                expr?.UpdateSource();
            }
        }

        private class SimpleCommand : ICommand {
            private readonly Action _execute;
            private readonly Func<bool> _can;
            public SimpleCommand(Action execute, Func<bool> can) { _execute = execute; _can = can; }
            public bool CanExecute(object? parameter) => _can();
            public void Execute(object? parameter) => _execute();
            public event EventHandler? CanExecuteChanged {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }
        }
    }
}
