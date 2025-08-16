using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Workbench.Admin.Views.Management.Herbs.Dialogs
{
    /// <summary>
    /// HerbEditDialog.xaml 的交互逻辑
    /// </summary>
    public partial class HerbEditDialog : Window
    {
        public HerbEditDialog(HerbDto? existingHerb, List<string> unitOptions)
        {
            InitializeComponent();
            
            // 加载单位选项
            if (unitOptions != null)
            {
                foreach (var unit in unitOptions)
                {
                    CmbUnit.Items.Add(unit);
                }
            }
            
            if (existingHerb != null)
            {
                // 编辑模式
                Title = "编辑药材";
                LoadHerbData(existingHerb);
            }
            else
            {
                // 新增模式
                Title = "添加药材";
                // 设置默认值
                TxtStock.Text = "0";
                TxtPrice.Text = "0.00";
                if (CmbUnit.Items.Count > 0)
                {
                    CmbUnit.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// 药材数据属性 - SystemWorkbench期望的接口
        /// </summary>
        public HerbEditData? HerbData { get; private set; }

        private void LoadHerbData(HerbDto herb)
        {
            TxtName.Text = herb.Name;
            TxtPinYinCode.Text = herb.PinYinCode ?? string.Empty;
            TxtWuBiCode.Text = herb.WuBiCode ?? string.Empty;
            TxtOrigin.Text = herb.Origin ?? string.Empty;
            TxtSpec.Text = herb.Spec ?? string.Empty;
            TxtPrice.Text = herb.Price.ToString("F2");
            TxtStock.Text = herb.Stock.ToString();
            TxtEffect.Text = herb.Effect ?? string.Empty;
            TxtUsage.Text = herb.Usage ?? string.Empty;
            
            // 设置单位
            if (!string.IsNullOrEmpty(herb.Unit))
            {
                CmbUnit.SelectedItem = herb.Unit;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                HerbData = new HerbEditData
                {
                    Name = TxtName.Text.Trim(),
                    PinYinCode = TxtPinYinCode.Text.Trim(),
                    WuBiCode = TxtWuBiCode.Text.Trim(),
                    Origin = TxtOrigin.Text.Trim(),
                    Spec = TxtSpec.Text.Trim(),
                    Unit = CmbUnit.SelectedItem?.ToString() ?? "g",
                    Price = decimal.Parse(TxtPrice.Text),
                    Stock = int.Parse(TxtStock.Text),
                    Effect = TxtEffect.Text.Trim(),
                    Usage = TxtUsage.Text.Trim(),
                    BatchNo = string.Empty, // 批次号暂时为空
                    ExpireDate = null // 过期日期暂时为空
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
                MessageBox.Show("请输入药材名称", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtName.Focus();
                return false;
            }

            if (!decimal.TryParse(TxtPrice.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("请输入有效的单价", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtPrice.Focus();
                return false;
            }

            if (!int.TryParse(TxtStock.Text, out int stock) || stock < 0)
            {
                MessageBox.Show("请输入有效的库存数量", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtStock.Focus();
                return false;
            }

            if (CmbUnit.SelectedItem == null)
            {
                MessageBox.Show("请选择单位", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                CmbUnit.Focus();
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 药材编辑数据传输对象
    /// </summary>
    public class HerbEditData
    {
        public string Name { get; set; } = string.Empty;
        public string PinYinCode { get; set; } = string.Empty;
        public string WuBiCode { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Spec { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Effect { get; set; } = string.Empty;
        public string Usage { get; set; } = string.Empty;
        public string BatchNo { get; set; } = string.Empty;
        public DateTime? ExpireDate { get; set; }
    }
}