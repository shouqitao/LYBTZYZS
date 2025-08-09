using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.Shared.Models.Contracts.Formula;

using Prism.Dialogs;
using LYBT.WPF.Client.Core.Extensions;
namespace LYBT.WPF.Client.Modules.SystemManagement.Formulas.Views
{
    public partial class AddFormulaDialog : Window
    {
        private readonly IDialogService _commonDialogService;

        private readonly IHerbService _herbService;
        private readonly IFormulaService _formulaService;
        private List<HerbInfo> _availableHerbs = new List<HerbInfo>();
        private Dictionary<string, HerbInfo> _herbDict = new Dictionary<string, HerbInfo>(StringComparer.OrdinalIgnoreCase);
        private List<SelectedHerbItem> _selectedHerbs = new List<SelectedHerbItem>();
        private bool _isLoadingHerbs = false;

        public AddFormulaDialog(IHerbService herbService, IFormulaService formulaService,
            IDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            InitializeComponent();

            _herbService = herbService;
            _formulaService = formulaService;

            // 设置焦点到验方名称
            Loaded += async (s, e) =>
            {
                txtFormulaName.Focus();

                if (_herbService != null)
                {
                    await LoadHerbs();
                }
                else
                {
                    // 如果没有服务，使用预定义的药材列表
                    LoadPredefinedHerbs();
                }
            };

            // 设置ComboBox的文本变化事件用于过滤
            cboHerbName.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(OnHerbNameTextChanged));
        }

        private void LoadPredefinedHerbs()
        {
            // 预定义的常用药材列表（演示用）
            var predefinedHerbs = new[]
            {
                new { Name = "白芍", PinYin = "bs", Unit = "g" },
                new { Name = "白芷子", PinYin = "bzz", Unit = "g" },
                new { Name = "党参", PinYin = "dc", Unit = "g" },
                new { Name = "竹茹", PinYin = "zr", Unit = "g" },
                new { Name = "川椒子", PinYin = "cjz", Unit = "g" },
                new { Name = "酒芍", PinYin = "js", Unit = "g" },
                new { Name = "桔梗", PinYin = "jg", Unit = "g" },
                new { Name = "陈皮", PinYin = "cp", Unit = "g" },
                new { Name = "川芎", PinYin = "cx", Unit = "g" },
                new { Name = "香附", PinYin = "xf", Unit = "g" },
                new { Name = "生姜", PinYin = "sj", Unit = "g" },
                new { Name = "炒谷芽", PinYin = "cgy", Unit = "g" },
                new { Name = "茯苓", PinYin = "fl", Unit = "g" },
                new { Name = "藿香", PinYin = "hx", Unit = "g" },
                new { Name = "白术", PinYin = "bz", Unit = "g" },
                new { Name = "翘白草", PinYin = "qbc", Unit = "g" },
                new { Name = "郁李仁", PinYin = "ylr", Unit = "g" },
                new { Name = "甘草", PinYin = "gc", Unit = "g" },
                new { Name = "大枣", PinYin = "dz", Unit = "个" },
                new { Name = "半夏", PinYin = "bx", Unit = "g" },
                new { Name = "台乌", PinYin = "tw", Unit = "g" },
                new { Name = "苏叶", PinYin = "sy", Unit = "g" },
                new { Name = "厚朴", PinYin = "hp", Unit = "g" },
                new { Name = "柴胡", PinYin = "ch", Unit = "g" },
                new { Name = "吴茱", PinYin = "wz", Unit = "g" },
                new { Name = "炒麦芽", PinYin = "cmy", Unit = "g" },
                new { Name = "旋覆花", PinYin = "xfh", Unit = "g" },
                new { Name = "黄苓", PinYin = "hq", Unit = "g" },
                new { Name = "白芷", PinYin = "bz", Unit = "g" },
                new { Name = "腹皮", PinYin = "fp", Unit = "g" }
            };

            _availableHerbs.Clear();
            _herbDict.Clear();

            foreach (var herb in predefinedHerbs)
            {
                var herbInfo = new HerbInfo
                {
                    Id = Guid.NewGuid(),
                    Name = herb.Name,
                    PinYinCode = herb.PinYin,
                    Unit = herb.Unit
                };
                _availableHerbs.Add(herbInfo);
                _herbDict[herb.Name] = herbInfo;
            }

            // 填充ComboBox
            cboHerbName.Items.Clear();
            foreach (var herb in _availableHerbs.OrderBy(h => h.Name))
            {
                cboHerbName.Items.Add(herb.Name);
            }
        }

        private async Task LoadHerbs()
        {
            try
            {
                _isLoadingHerbs = true;

                // 从药材服务加载可用药材
                _availableHerbs = await _herbService.GetAvailableHerbsAsync();

                // 建立名称和拼音索引
                _herbDict.Clear();
                foreach (var herb in _availableHerbs)
                {
                    _herbDict[herb.Name] = herb;
                }

                // 填充ComboBox
                cboHerbName.Items.Clear();
                foreach (var herb in _availableHerbs.OrderBy(h => h.Name))
                {
                    cboHerbName.Items.Add(herb.Name);
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"加载药材列表失败：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
            finally
            {
                _isLoadingHerbs = false;
            }
        }

        private void OnHerbNameTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoadingHerbs) return;

            var comboBox = sender as ComboBox;
            if (comboBox == null || !comboBox.IsDropDownOpen) return;

            var searchText = comboBox.Text?.ToLower() ?? "";
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // 显示所有药材
                comboBox.Items.Clear();
                foreach (var herb in _availableHerbs.OrderBy(h => h.Name))
                {
                    comboBox.Items.Add(herb.Name);
                }
                return;
            }

            // 过滤药材（支持名称和拼音首字母匹配）
            var filteredHerbs = _availableHerbs.Where(h =>
                h.Name.Contains(searchText) ||
                (h.PinYinCode?.ToLower().StartsWith(searchText) ?? false)
            ).OrderBy(h => h.Name).ToList();

            comboBox.Items.Clear();
            foreach (var herb in filteredHerbs)
            {
                comboBox.Items.Add(herb.Name);
            }

            // 如果没有匹配项，保持用户输入
            if (comboBox.Items.Count == 0)
            {
                comboBox.Text = searchText;
            }
        }

        private void cboHerbName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboHerbName.SelectedItem != null)
            {
                var herbName = cboHerbName.SelectedItem.ToString();
                if (!string.IsNullOrEmpty(herbName) && _herbDict.TryGetValue(herbName, out var herb))
                {
                    // 自动填充单位
                    txtUnit.Text = herb.Unit ?? "g";
                }
            }
        }

        private void cboHerbName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                e.Handled = true;

                // 验证药材是否存在
                var herbName = cboHerbName.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(herbName))
                {
                    if (!_herbDict.ContainsKey(herbName))
                    {
                        _commonDialogService.ShowWarningAsync("无此药材", "提示").GetAwaiter().GetResult();
                        cboHerbName.Focus();
                        if (cboHerbName.SelectedItem == null)
                        {
                            cboHerbName.Text = "";
                        }
                        return;
                    }

                    // 选中药材并更新单位
                    if (_herbDict.TryGetValue(herbName, out var herb))
                    {
                        cboHerbName.SelectedItem = herb.Name;
                        txtUnit.Text = herb.Unit ?? "g";
                    }
                }

                txtQuantity.Focus();
                txtQuantity.SelectAll();
            }
        }

        private void txtQuantity_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                AddHerb();
            }
        }

        private void btnAddHerb_Click(object sender, RoutedEventArgs e)
        {
            AddHerb();
        }

        private void AddHerb()
        {
            var herbName = cboHerbName.Text?.Trim();
            var quantityText = txtQuantity.Text?.Trim();

            if (string.IsNullOrWhiteSpace(herbName))
            {
                _commonDialogService.ShowWarningAsync("请输入药材名称", "提示").GetAwaiter().GetResult();
                cboHerbName.Focus();
                return;
            }

            // 验证药材是否存在
            if (!_herbDict.TryGetValue(herbName, out var herb))
            {
                _commonDialogService.ShowWarningAsync("无此药材", "提示").GetAwaiter().GetResult();
                cboHerbName.Focus();
                if (cboHerbName.SelectedItem == null)
                {
                    cboHerbName.Text = "";
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(quantityText) || !decimal.TryParse(quantityText, out decimal quantity) || quantity <= 0)
            {
                _commonDialogService.ShowWarningAsync("请输入有效的剂量", "提示").GetAwaiter().GetResult();
                txtQuantity.Focus();
                txtQuantity.SelectAll();
                return;
            }

            // 添加药材
            var selectedHerb = new SelectedHerbItem
            {
                HerbId = herb.Id,
                HerbName = herb.Name,
                Quantity = quantity,
                Unit = herb.Unit ?? "g"
            };
            _selectedHerbs.Add(selectedHerb);

            // 创建显示控件
            var border = CreateHerbDisplay(selectedHerb);
            wpHerbs.Children.Add(border);

            // 清空输入
            cboHerbName.Text = "";
            txtQuantity.Text = "";
            txtUnit.Text = "g";
            cboHerbName.Focus();
        }

        private Border CreateHerbDisplay(SelectedHerbItem herb)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                Margin = new Thickness(5),
                Padding = new Thickness(10, 5, 10, 5),
                CornerRadius = new CornerRadius(3),
                MinWidth = 120  // 确保每个药材项有最小宽度
            };

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var textBlock = new TextBlock
            {
                Text = $"{herb.HerbName} {herb.Quantity}{herb.Unit}",
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var deleteButton = new Button
            {
                Content = "×",
                Width = 20,
                Height = 20,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.Red,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                Tag = herb
            };

            deleteButton.Click += (s, e) =>
            {
                var btn = s as Button;
                var item = btn?.Tag as SelectedHerbItem;
                if (item != null)
                {
                    _selectedHerbs.Remove(item);
                }
                wpHerbs.Children.Remove(border);
            };

            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(deleteButton);
            border.Child = stackPanel;

            return border;
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            await HandleSaveAsync();
        }

        private async Task HandleSaveAsync()
        {
            // 验证
            if (string.IsNullOrWhiteSpace(txtFormulaName.Text))
            {
                await _commonDialogService.ShowWarningAsync("请输入验方名称", "提示");
                txtFormulaName.Focus();
                return;
            }

            if (_selectedHerbs.Count == 0)
            {
                await _commonDialogService.ShowWarningAsync("请至少添加一味药材", "提示");
                cboHerbName.Focus();
                return;
            }

            var category = (cboCategory.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "内科方";

            try
            {
                // 创建DTO
                var createDto = new FormulaCreateDto
                {
                    Name = txtFormulaName.Text.Trim(),
                    Indications = txtEffect.Text?.Trim() ?? "",  // 适应症，使用txtEffect控件
                    Effect = txtEffect.Text?.Trim() ?? "",           // 功效
                    Usage = txtUsage.Text?.Trim() ?? "",
                    Remark = txtRemark.Text?.Trim(),
                    Herbs = _selectedHerbs.Select((h, index) => new FormulaHerbItemCreateDto
                    {
                        HerbId = h.HerbId,
                        Quantity = h.Quantity,
                        SortOrder = index
                    }).ToList()
                };

                // 调用服务保存
                if (_formulaService != null)
                {
                    var result = await _formulaService.CreateAsync(createDto);

                    if (result.IsSuccess)
                    {
                        _commonDialogService.ShowInformationAsync("验方模板保存成功", "成功").GetAwaiter().GetResult();
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        _commonDialogService.ShowErrorAsync($"保存失败：{result.ErrorMessage}", "错误").GetAwaiter().GetResult();
                    }
                }
                else
                {
                    // 演示模式：只显示要保存的数据
                    var herbList = string.Join("、", _selectedHerbs.Select(h => $"{h.HerbName}{h.Quantity}{h.Unit}"));
                    _commonDialogService.ShowInformationAsync($"验方模板数据（演示模式）：\n\n" +
                                  $"名称：{createDto.Name}\n" +
                                  $"药材组成：{herbList}\n" +
                                  $"功效说明：{createDto.Effect}\n" +
                                  $"用法用量：{createDto.Usage}", "保存成功（演示）").GetAwaiter().GetResult();

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"保存验方时发生错误：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // 内部类：选中的药材项
        private class SelectedHerbItem
        {
            public Guid HerbId { get; set; }
            public string HerbName { get; set; } = string.Empty;
            public decimal Quantity { get; set; }
            public string Unit { get; set; } = "g";
        }

        // 处理粘贴事件（支持批量输入）
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (cboHerbName.IsFocused || txtQuantity.IsFocused)
                {
                    e.Handled = true;
                    HandlePaste();
                }
            }
            base.OnPreviewKeyDown(e);
        }

        private void HandlePaste()
        {
            var text = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text)) return;

            // 尝试解析粘贴的文本
            // 支持格式：白芍25 川芎50 或 白芍 25 川芎 50
            var pattern = @"(\S+)\s*(\d+(?:\.\d+)?)";
            var matches = Regex.Matches(text, pattern);

            if (matches.Count > 0)
            {
                var validHerbs = new List<(string name, decimal quantity, HerbInfo herb)>();
                var invalidHerbs = new List<string>();

                foreach (Match match in matches)
                {
                    var herbName = match.Groups[1].Value;
                    if (decimal.TryParse(match.Groups[2].Value, out decimal quantity))
                    {
                        if (_herbDict.TryGetValue(herbName, out var herb))
                        {
                            validHerbs.Add((herbName, quantity, herb));
                        }
                        else
                        {
                            invalidHerbs.Add(herbName);
                        }
                    }
                }

                if (invalidHerbs.Count > 0)
                {
                    _commonDialogService.ShowWarningAsync($"以下药材不存在：{string.Join("、", invalidHerbs)}", "提示").GetAwaiter().GetResult();
                }

                if (validHerbs.Count > 0)
                {
                    var result = _commonDialogService.ShowConfirmationAsync($"检测到{validHerbs.Count}味药材，是否批量添加？", "批量添加").GetAwaiter().GetResult();

                    if (result)
                    {
                        foreach (var (name, quantity, herb) in validHerbs)
                        {
                            var selectedHerb = new SelectedHerbItem
                            {
                                HerbId = herb.Id,
                                HerbName = name,
                                Quantity = quantity,
                                Unit = herb.Unit ?? "g"
                            };
                            _selectedHerbs.Add(selectedHerb);
                            var border = CreateHerbDisplay(selectedHerb);
                            wpHerbs.Children.Add(border);
                        }

                        // 清空输入框
                        cboHerbName.Text = "";
                        txtQuantity.Text = "";
                        txtUnit.Text = "g";
                    }
                }
            }
        }
    }
}