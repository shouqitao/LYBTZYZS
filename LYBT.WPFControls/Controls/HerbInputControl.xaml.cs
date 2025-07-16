using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LYBT.Module.Herbs.Dtos;
using LYBT.Models.Prescriptions.Dtos;

namespace LYBT.WPFControls {
    /// <summary>
    /// 药材智能录入控件，支持拼音搜索与快捷键添加
    /// </summary>
    public partial class HerbInputControl : UserControl, INotifyPropertyChanged {
        public HerbInputControl() {
            ItemsSource = new ObservableCollection<PrescriptionItemDto>();
            FilteredHerbs = new ObservableCollection<HerbDto>();
            AddCommand = new RelayCommand(_ => AddCurrent(), _ => CanAdd());
            RemoveCommand = new RelayCommand(p => RemoveItem(p as PrescriptionItemDto));
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #region Dependency Properties
        public ObservableCollection<PrescriptionItemDto> ItemsSource {
            get => (ObservableCollection<PrescriptionItemDto>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(ObservableCollection<PrescriptionItemDto>), typeof(HerbInputControl), new PropertyMetadata(new ObservableCollection<PrescriptionItemDto>()));

        public ObservableCollection<HerbDto>? Herbs {
            get => (ObservableCollection<HerbDto>?)GetValue(HerbsProperty);
            set => SetValue(HerbsProperty, value);
        }

        public static readonly DependencyProperty HerbsProperty =
            DependencyProperty.Register(nameof(Herbs), typeof(ObservableCollection<HerbDto>), typeof(HerbInputControl), new PropertyMetadata(null));
        #endregion

        public ObservableCollection<HerbDto> FilteredHerbs { get; }

        public HerbDto? SelectedSuggestion { get; set; }

        private string _herbText = string.Empty;
        public string HerbText {
            get => _herbText;
            set {
                if (_herbText != value) {
                    _herbText = value;
                    OnPropertyChanged(nameof(HerbText));
                    UpdateFilter();
                }
            }
        }

        private string _amountText = string.Empty;
        public string AmountText {
            get => _amountText;
            set {
                if (_amountText != value) {
                    _amountText = value;
                    OnPropertyChanged(nameof(AmountText));
                }
            }
        }

        private bool _isSuggestionVisible;
        public bool IsSuggestionVisible {
            get => _isSuggestionVisible;
            set {
                if (_isSuggestionVisible != value) {
                    _isSuggestionVisible = value;
                    OnPropertyChanged(nameof(IsSuggestionVisible));
                }
            }
        }

        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }

        private void UpdateFilter() {
            FilteredHerbs.Clear();
            if (string.IsNullOrWhiteSpace(HerbText) || Herbs == null) {
                IsSuggestionVisible = false;
                return;
            }
            var text = HerbText.Trim().ToLowerInvariant();
            var items = Herbs.Where(h => !ItemsSource.Any(i => i.HerbId == h.Id) &&
                (h.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                 (!string.IsNullOrEmpty(h.Pinyin) && h.Pinyin.Contains(text, StringComparison.OrdinalIgnoreCase)) ||
                 (!string.IsNullOrEmpty(h.Pinyin) && GetInitials(h.Pinyin).StartsWith(text))));
            foreach (var h in items)
                FilteredHerbs.Add(h);
            IsSuggestionVisible = FilteredHerbs.Count > 0;
            if (FilteredHerbs.Count > 0)
                SelectedSuggestion = FilteredHerbs[0];
        }

        private static string GetInitials(string pinyin) {
            return new string(pinyin.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.Length > 0)
                .Select(s => char.ToLowerInvariant(s[0])).ToArray());
        }

        private bool CanAdd() => SelectedSuggestion != null && decimal.TryParse(AmountText, out var a) && a > 0;

        private void AddCurrent() {
            if (SelectedSuggestion == null) return;
            if (!decimal.TryParse(AmountText, out var amount)) return;
            var dto = new PrescriptionItemDto {
                Id = Guid.NewGuid(),
                HerbId = SelectedSuggestion.Id,
                HerbName = SelectedSuggestion.Name,
                Quantity = amount,
                Unit = SelectedSuggestion.Unit
            };
            ItemsSource.Add(dto);
            HerbText = string.Empty;
            AmountText = string.Empty;
            IsSuggestionVisible = false;
            PART_HerbBox.Focus();
        }

        private void RemoveItem(PrescriptionItemDto? item) {
            if (item != null)
                ItemsSource.Remove(item);
        }

        private void PART_HerbBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Down) {
                if (FilteredHerbs.Count > 0) {
                    PART_SuggestionList.Focus();
                    PART_SuggestionList.SelectedIndex = 0;
                }
            } else if (e.Key == Key.Enter) {
                if (FilteredHerbs.Count == 1) {
                    SelectedSuggestion = FilteredHerbs[0];
                    e.Handled = true;
                    PART_AmountBox.Focus();
                }
            }
        }

        private void PART_SuggestionList_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                if (SelectedSuggestion != null) {
                    PART_AmountBox.Focus();
                }
            }
        }

        private void PART_AmountBox_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                if (AddCommand.CanExecute(null)) {
                    AddCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }

    internal class RelayCommand : ICommand {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _can;
        public RelayCommand(Action<object?> execute, Func<object?, bool>? can = null) {
            _execute = execute; _can = can;
        }
        public event EventHandler? CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
        public bool CanExecute(object? parameter) => _can?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
    }
}
