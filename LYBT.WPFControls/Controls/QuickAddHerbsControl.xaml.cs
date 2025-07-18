using LYBT.Module.Herbs.Dtos;
using LYBT.Module.Prescriptions.Dtos;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
namespace LYBT.WPFControls {
    /// <summary>
    /// Herb selection control with search support.
    /// </summary>
    public partial class QuickAddHerbsControl : UserControl {
        private IEnumerable<HerbDto> _herbs = Array.Empty<HerbDto>();

        public QuickAddHerbsControl() {
            InitializeComponent();
            AddHerbCommand = new DelegateCommand<HerbDto?>(AddHerb);
        }

        public IEnumerable<HerbDto>? Herbs {
            get => (IEnumerable<HerbDto>?)GetValue(HerbsProperty);
            set => SetValue(HerbsProperty, value);
        }

        public static readonly DependencyProperty HerbsProperty =
            DependencyProperty.Register(
                nameof(Herbs), typeof(IEnumerable<HerbDto>), typeof(QuickAddHerbsControl),
                new PropertyMetadata(null, OnHerbsChanged));

        public ObservableCollection<PrescriptionItemDto>? TargetItems {
            get => (ObservableCollection<PrescriptionItemDto>?)GetValue(TargetItemsProperty);
            set => SetValue(TargetItemsProperty, value);
        }

        public static readonly DependencyProperty TargetItemsProperty =
            DependencyProperty.Register(nameof(TargetItems), typeof(ObservableCollection<PrescriptionItemDto>), typeof(QuickAddHerbsControl));

        public string SearchText {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(QuickAddHerbsControl),
                new PropertyMetadata(string.Empty, OnSearchTextChanged));

        public ObservableCollection<HerbDto> FilteredHerbs { get; } = new();

        public DelegateCommand<HerbDto?> AddHerbCommand { get; }

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is QuickAddHerbsControl c)
                c.UpdateFilter();
        }

        private static void OnHerbsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is QuickAddHerbsControl c) {
                c._herbs = e.NewValue as IEnumerable<HerbDto> ?? Array.Empty<HerbDto>();
                c.UpdateFilter();
            }
        }

        private void UpdateFilter() {
            FilteredHerbs.Clear();
            IEnumerable<HerbDto> query = _herbs;
            if (!string.IsNullOrWhiteSpace(SearchText)) {
                query = query.Where(h => h.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrEmpty(h.Pinyin) && h.Pinyin.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
            }
            foreach (var h in query)
                FilteredHerbs.Add(h);
        }

        private void AddHerb(HerbDto? herb) {
            if (herb == null || TargetItems == null)
                return;
            if (TargetItems.Any(t => t.HerbId == herb.Id))
                return;
            TargetItems.Add(new PrescriptionItemDto { HerbId = herb.Id, HerbName = herb.Name });
        }

    }
}
