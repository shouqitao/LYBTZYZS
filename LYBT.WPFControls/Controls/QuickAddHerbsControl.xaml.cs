using LYBT.Module.Herbs.Dtos;
using LYBT.Module.Prescriptions.Dtos;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace LYBT.WPFControls {
    /// <summary>
    /// Herb selection control with search and category filter.
    /// </summary>
    public partial class QuickAddHerbsControl : UserControl {
        private IEnumerable<HerbDto> _herbs = Array.Empty<HerbDto>();
        private readonly Dictionary<Guid, string> _categoryCache = new();

        public QuickAddHerbsControl() {
            InitializeComponent();
            AddHerbCommand = new DelegateCommand<HerbDto?>(AddHerb);
            Categories.Add("全部");
            Categories.Add("解表");
            Categories.Add("清热");
            Categories.Add("其他");
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

        public ObservableCollection<string> Categories { get; } = new();
        public ObservableCollection<HerbDto> FilteredHerbs { get; } = new();

        private string _selectedCategory = "全部";
        public string SelectedCategory {
            get => _selectedCategory;
            set {
                if (_selectedCategory != value) {
                    _selectedCategory = value;
                    UpdateFilter();
                }
            }
        }

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
            if (SelectedCategory != "全部")
                query = query.Where(h => GetCategory(h) == SelectedCategory);
            foreach (var h in query)
                FilteredHerbs.Add(h);
        }

        private string GetCategory(HerbDto herb) {
            if (_categoryCache.TryGetValue(herb.Id, out var cat))
                return cat;
            if (!string.IsNullOrWhiteSpace(herb.Effect)) {
                if (herb.Effect.Contains("解表"))
                    cat = "解表";
                else if (herb.Effect.Contains("清热"))
                    cat = "清热";
                else
                    cat = "其他";
            } else {
                cat = "其他";
            }
            _categoryCache[herb.Id] = cat;
            return cat;
        }

        private void AddHerb(HerbDto? herb) {
            if (herb == null || TargetItems == null)
                return;
            if (TargetItems.Any(t => t.HerbId == herb.Id))
                return;
            TargetItems.Add(new PrescriptionItemDto { HerbId = herb.Id, HerbName = herb.Name });
        }

        private void CategoryButton_Click(object sender, RoutedEventArgs e) {
            if (sender is ToggleButton btn && btn.Content is string cat)
                SelectedCategory = cat;
        }
    }
}
