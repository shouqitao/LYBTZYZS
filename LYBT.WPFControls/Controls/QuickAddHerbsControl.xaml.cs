using LYBT.Module.Herbs.Dtos;
using LYBT.Module.Prescriptions.Dtos;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LYBT.WPFControls {
    /// <summary>
    /// Control providing fuzzy search and batch import of herbs.
    /// </summary>
    public partial class QuickAddHerbsControl : UserControl {
        private IEnumerable<HerbDto> _herbs = Array.Empty<HerbDto>();

        public QuickAddHerbsControl() {
            InitializeComponent();
            ImportCommand = new DelegateCommand(Import);
            RemovePendingCommand = new DelegateCommand<PrescriptionItemDto?>(RemovePending);
        }

        public ObservableCollection<HerbDto> Suggestions { get; } = new();

        public ObservableCollection<PrescriptionItemDto> Pending { get; } = new();

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

        public DelegateCommand ImportCommand { get; }
        public DelegateCommand<PrescriptionItemDto?> RemovePendingCommand { get; }

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is QuickAddHerbsControl c)
                c.UpdateSuggestions();
        }

        private static void OnHerbsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is QuickAddHerbsControl c) {
                c._herbs = e.NewValue as IEnumerable<HerbDto> ?? Array.Empty<HerbDto>();
                c.UpdateSuggestions();
            }
        }

        private void UpdateSuggestions() {
            Suggestions.Clear();
            if (string.IsNullOrWhiteSpace(SearchText))
                return;
            foreach (var h in _herbs.Where(h =>
                h.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(h.Pinyin) && h.Pinyin.Contains(SearchText, StringComparison.OrdinalIgnoreCase))))
                Suggestions.Add(h);
        }

        private void SearchBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is HerbDto herb)
                AddPending(herb);
            ((ComboBox)sender).Text = string.Empty;
            Suggestions.Clear();
        }

        private void AddPending(HerbDto herb) {
            if (Pending.Any(p => p.HerbId == herb.Id))
                return;
            Pending.Add(new PrescriptionItemDto { HerbId = herb.Id, HerbName = herb.Name });
        }

        private void RemovePending(PrescriptionItemDto? item) {
            if (item != null)
                Pending.Remove(item);
        }

        private void Import() {
            if (TargetItems == null)
                return;
            foreach (var p in Pending) {
                if (TargetItems.Any(t => t.HerbId == p.HerbId))
                    continue;
                TargetItems.Add(new PrescriptionItemDto { HerbId = p.HerbId, HerbName = p.HerbName });
            }
            Pending.Clear();
        }
    }
}
