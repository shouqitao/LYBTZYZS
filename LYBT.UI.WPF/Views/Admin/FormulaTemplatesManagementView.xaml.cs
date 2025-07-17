using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using LYBT.Module.Herbs.Dtos;
using LYBT.Module.Prescriptions.Dtos;
using LYBT.UI.WPF.ViewModels.Admin;
using LYBT.UI.WPF.ViewModels.Profile;

namespace LYBT.UI.WPF.Views.Admin {
    /// <summary>
    /// FormulaTemplatesManagementView code-behind
    /// </summary>
    public partial class FormulaTemplatesManagementView : UserControl {
        public FormulaTemplatesManagementView() {
            InitializeComponent();
        }

        private FormulaTemplatesProfileViewModel? ProfileVM =>
            (DataContext as FormulaTemplatesManagementViewModel)?.ProfileViewModel;

        private void HerbComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (ProfileVM == null) return;
            if (sender is ComboBox cb && cb.SelectedItem is HerbDto herb && cb.DataContext is PrescriptionItemDto item) {
                item.HerbId = herb.Id;
                item.HerbName = herb.Name;
                item.Unit = herb.Unit;
                MoveToQuantityCell(cb);
            }
        }

        private void HerbComboBox_KeyDown(object sender, KeyEventArgs e) {
            if (ProfileVM == null) return;
            if (sender is ComboBox cb && cb.IsDropDownOpen) {
                if (e.Key == Key.Tab) {
                    e.Handled = true;
                    var count = cb.Items.Count;
                    if (count > 0) {
                        var index = cb.SelectedIndex < 0 ? 0 : (cb.SelectedIndex + 1) % count;
                        cb.SelectedIndex = index;
                    }
                } else if (e.Key == Key.Enter) {
                    e.Handled = true;
                    if (cb.SelectedItem is HerbDto herb && cb.DataContext is PrescriptionItemDto item) {
                        item.HerbId = herb.Id;
                        item.HerbName = herb.Name;
                        item.Unit = herb.Unit;
                    }
                    cb.IsDropDownOpen = false;
                    MoveToQuantityCell(cb);
                }
            }
        }

        private void HerbComboBox_KeyUp(object sender, KeyEventArgs e) {
            if (ProfileVM == null) return;
            if (sender is ComboBox cb) {
                var view = CollectionViewSource.GetDefaultView(ProfileVM.AllHerbs);
                var text = cb.Text?.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(text)) {
                    view.Filter = null;
                    cb.IsDropDownOpen = false;
                    return;
                }
                view.Filter = o => {
                    if (o is HerbDto h) {
                        return h.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                               (!string.IsNullOrEmpty(h.Pinyin) && h.Pinyin.Contains(text, StringComparison.OrdinalIgnoreCase)) ||
                               (!string.IsNullOrEmpty(h.Pinyin) && GetInitials(h.Pinyin).StartsWith(text));
                    }
                    return false;
                };
                view.Refresh();
                cb.IsDropDownOpen = view.Cast<object>().Any();
            }
        }

        private void EditItemsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) {
            if (ProfileVM == null) return;
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (e.Row.Item is PrescriptionItemDto item) {
                if (ProfileVM.Items.LastOrDefault() == item && item.HerbId != Guid.Empty && item.Quantity > 0) {
                    ProfileVM.Items.Add(new PrescriptionItemDto());
                }
            }
        }

        private void MoveToQuantityCell(ComboBox cb) {
            if (EditItemsGrid.ItemContainerGenerator.ContainerFromItem(cb.DataContext) is DataGridRow row) {
                EditItemsGrid.CommitEdit();
                EditItemsGrid.CurrentCell = new DataGridCellInfo(row.Item, EditItemsGrid.Columns[1]);
                EditItemsGrid.BeginEdit();
                if (EditItemsGrid.CurrentCell.Column?.GetCellContent(row) is FrameworkElement el)
                {
                    el.Focus();
                    if (el is TextBox tb)
                        tb.SelectAll();
                }
            }
        }

        private static string GetInitials(string pinyin) {
            return new string(pinyin.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.Length > 0)
                .Select(s => char.ToLowerInvariant(s[0])).ToArray());
        }
    }
}
