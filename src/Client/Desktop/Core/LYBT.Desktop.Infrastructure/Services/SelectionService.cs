using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 选择服务实现
    /// OpenSpec: refactor-viewmodel-composition
    /// </summary>
    /// <typeparam name="T">列表项类型</typeparam>
    public partial class SelectionService<T> : ObservableObject, ISelectionService<T> where T : class
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelection))]
        [NotifyPropertyChangedFor(nameof(SelectionCount))]
        private T? _selectedItem;

        [ObservableProperty]
        private bool _isMultiSelectMode;

        /// <inheritdoc/>
        public ObservableCollection<T> SelectedItems { get; } = new();

        /// <inheritdoc/>
        public bool HasSelection => SelectedItem != null || SelectedItems.Count > 0;

        /// <inheritdoc/>
        public int SelectionCount => IsMultiSelectMode ? SelectedItems.Count : (SelectedItem != null ? 1 : 0);

        /// <inheritdoc/>
        public event EventHandler<SelectionChangedEventArgs<T>>? SelectionChanged;

        public SelectionService()
        {
            SelectedItems.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(SelectionCount));
            };
        }

        /// <inheritdoc/>
        public void Select(T? item)
        {
            var oldSelection = SelectedItem;
            SelectedItem = item;

            if (!IsMultiSelectMode)
            {
                SelectedItems.Clear();
                if (item != null)
                {
                    SelectedItems.Add(item);
                }
            }

            RaiseSelectionChanged(item, oldSelection);
        }

        /// <inheritdoc/>
        public void SelectMultiple(IEnumerable<T> items)
        {
            var oldSelection = SelectedItem;
            SelectedItems.Clear();

            foreach (var item in items)
            {
                SelectedItems.Add(item);
            }

            SelectedItem = SelectedItems.FirstOrDefault();
            RaiseSelectionChanged(SelectedItem, oldSelection);
        }

        /// <inheritdoc/>
        public void ToggleSelection(T item)
        {
            if (IsMultiSelectMode)
            {
                if (SelectedItems.Contains(item))
                {
                    SelectedItems.Remove(item);
                    if (SelectedItem == item)
                    {
                        SelectedItem = SelectedItems.FirstOrDefault();
                    }
                }
                else
                {
                    SelectedItems.Add(item);
                    SelectedItem ??= item;
                }
            }
            else
            {
                Select(SelectedItem == item ? null : item);
            }
        }

        /// <inheritdoc/>
        public void ClearSelection()
        {
            var oldSelection = SelectedItem;
            SelectedItem = null;
            SelectedItems.Clear();
            RaiseSelectionChanged(null, oldSelection);
        }

        private void RaiseSelectionChanged(T? newSelection, T? oldSelection)
        {
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs<T>(
                newSelection,
                oldSelection,
                SelectedItems.ToList().AsReadOnly()));
        }

        partial void OnSelectedItemChanged(T? oldValue, T? newValue)
        {
            if (oldValue != newValue)
            {
                RaiseSelectionChanged(newValue, oldValue);
            }
        }
    }
}
