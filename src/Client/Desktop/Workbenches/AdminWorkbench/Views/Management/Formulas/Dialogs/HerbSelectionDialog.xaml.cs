using LYBT.Shared.Models.Contracts.Common;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Workbench.Admin.Views.Management.Formulas.Dialogs
{
    /// <summary>
    /// HerbSelectionDialog.xaml 的交互逻辑
    /// </summary>
    public partial class HerbSelectionDialog : Window
    {
        private readonly List<HerbDto> _allHerbs;
        private List<HerbDto> _filteredHerbs;

        public HerbSelectionDialog(List<HerbDto> availableHerbs)
        {
            InitializeComponent();
            
            _allHerbs = availableHerbs ?? new List<HerbDto>();
            _filteredHerbs = _allHerbs.ToList();
            
            DgHerbs.ItemsSource = _filteredHerbs;
            
            if (_filteredHerbs.Count > 0)
            {
                DgHerbs.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 选中的药材
        /// </summary>
        public HerbDto? SelectedHerb { get; private set; }

        private void TxtSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var searchText = TxtSearch.Text.Trim().ToLower();
            
            if (string.IsNullOrEmpty(searchText))
            {
                _filteredHerbs = _allHerbs.ToList();
            }
            else
            {
                _filteredHerbs = _allHerbs.Where(h => 
                    h.Name.ToLower().Contains(searchText) ||
                    (h.PinYinCode?.ToLower().Contains(searchText) ?? false) ||
                    (h.Effect?.ToLower().Contains(searchText) ?? false)
                ).ToList();
            }
            
            DgHerbs.ItemsSource = _filteredHerbs;
            
            if (_filteredHerbs.Count > 0)
            {
                DgHerbs.SelectedIndex = 0;
            }
        }

        private void DgHerbs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DgHerbs.SelectedItem is HerbDto herb)
            {
                SelectedHerb = herb;
                DialogResult = true;
                Close();
            }
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (DgHerbs.SelectedItem is HerbDto herb)
            {
                SelectedHerb = herb;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("请选择一个药材", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}