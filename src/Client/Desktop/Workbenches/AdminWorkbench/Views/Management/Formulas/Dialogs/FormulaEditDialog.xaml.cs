using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Workbench.Admin.Views.Management.Formulas.Dialogs
{
    /// <summary>
    /// FormulaEditDialog.xaml 的交互逻辑
    /// </summary>
    public partial class FormulaEditDialog : Window
    {
        private readonly List<HerbDto> _availableHerbs;
        private readonly ObservableCollection<FormulaHerbItem> _herbItems;

        public FormulaEditDialog(FormulaDto? existingFormula, List<HerbDto> availableHerbs)
        {
            InitializeComponent();
            
            _availableHerbs = availableHerbs ?? new List<HerbDto>();
            _herbItems = new ObservableCollection<FormulaHerbItem>();
            DgHerbs.ItemsSource = _herbItems;
            
            if (existingFormula != null)
            {
                // 编辑模式
                Title = "编辑验方";
                LoadFormulaData(existingFormula);
            }
            else
            {
                // 新增模式
                Title = "添加验方";
            }
        }

        /// <summary>
        /// 验方数据属性 - SystemWorkbench期望的接口
        /// </summary>
        public FormulaEditData? FormulaData { get; private set; }

        private void LoadFormulaData(FormulaDto formula)
        {
            TxtName.Text = formula.Name;
            TxtEffect.Text = formula.Effect ?? string.Empty;
            TxtUsage.Text = formula.Usage ?? string.Empty;
            TxtIndications.Text = formula.Indications ?? string.Empty;
            TxtContraindications.Text = formula.Contraindications ?? string.Empty;
            TxtPreparation.Text = formula.Preparation ?? string.Empty;
            ChkIsShared.IsChecked = formula.IsShared;
            
            // 加载药材组成
            if (formula.Herbs != null)
            {
                foreach (var herb in formula.Herbs)
                {
                    var herbInfo = _availableHerbs.FirstOrDefault(h => h.Id == herb.HerbId);
                    _herbItems.Add(new FormulaHerbItem
                    {
                        Id = herb.Id,
                        HerbId = herb.HerbId,
                        HerbName = herbInfo?.Name ?? "未知药材",
                        Quantity = herb.Quantity,
                        Preparation = herb.Preparation ?? string.Empty,
                        Usage = herb.Usage ?? string.Empty,
                        SortOrder = herb.SortOrder
                    });
                }
            }
        }

        private void BtnAddHerb_Click(object sender, RoutedEventArgs e)
        {
            var selectDialog = new HerbSelectionDialog(_availableHerbs);
            if (selectDialog.ShowDialog() == true && selectDialog.SelectedHerb != null)
            {
                var herb = selectDialog.SelectedHerb;
                _herbItems.Add(new FormulaHerbItem
                {
                    HerbId = herb.Id,
                    HerbName = herb.Name,
                    Quantity = 10, // 默认用量
                    Preparation = "常规",
                    Usage = "煎服",
                    SortOrder = _herbItems.Count + 1
                });
            }
        }

        private void BtnRemoveHerb_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is FormulaHerbItem herbItem)
            {
                _herbItems.Remove(herbItem);
                
                // 重新排序
                for (int i = 0; i < _herbItems.Count; i++)
                {
                    _herbItems[i].SortOrder = i + 1;
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                FormulaData = new FormulaEditData
                {
                    Name = TxtName.Text.Trim(),
                    Effect = TxtEffect.Text.Trim(),
                    Usage = TxtUsage.Text.Trim(),
                    Indications = TxtIndications.Text.Trim(),
                    Contraindications = TxtContraindications.Text.Trim(),
                    Preparation = TxtPreparation.Text.Trim(),
                    IsShared = ChkIsShared.IsChecked ?? false,
                    Instructions = string.Empty, // 暂时为空
                    Remark = string.Empty, // 暂时为空
                    Herbs = _herbItems.ToList()
                };

                DialogResult = true;
                Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("请输入验方名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtEffect.Text))
            {
                MessageBox.Show("请输入功效", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtEffect.Focus();
                return false;
            }

            if (_herbItems.Count == 0)
            {
                MessageBox.Show("请至少添加一味药材", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 验方编辑数据传输对象
    /// </summary>
    public class FormulaEditData
    {
        public string Name { get; set; } = string.Empty;
        public string Effect { get; set; } = string.Empty;
        public string Usage { get; set; } = string.Empty;
        public string Indications { get; set; } = string.Empty;
        public string Contraindications { get; set; } = string.Empty;
        public string Preparation { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public bool IsShared { get; set; }
        public List<FormulaHerbItem> Herbs { get; set; } = new List<FormulaHerbItem>();
    }

    /// <summary>
    /// 验方药材项
    /// </summary>
    public class FormulaHerbItem
    {
        public Guid Id { get; set; }
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Preparation { get; set; } = string.Empty;
        public string Usage { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}