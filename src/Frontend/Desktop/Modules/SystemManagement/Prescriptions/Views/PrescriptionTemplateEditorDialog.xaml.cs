using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Admin.Prescriptions.ViewModels;
using Prism.Commands;

namespace LYBT.Desktop.Admin.Prescriptions.Views
{
    /// <summary>
    /// 处方模板编辑器对话框
    /// </summary>
    public partial class PrescriptionTemplateEditorDialog : Window, INotifyPropertyChanged
    {
        #region 字段

        private string _windowTitle = "新建处方模板";
        private string _templateName = string.Empty;
        private string _selectedCategory = "其他";
        private string _syndrome = string.Empty;
        private string _treatmentPrinciple = string.Empty;
        private string _diagnosis = string.Empty;
        private string _usage = "水煎服，每日一剂，分两次服用";
        private int _dosageCount = 7;
        private string _remark = string.Empty;
        private bool _isPublic;
        private ObservableCollection<PrescriptionTemplateItem> _herbItems = new();

        #endregion

        #region 属性

        /// <summary>
        /// 编辑的模板
        /// </summary>
        public PrescriptionTemplate? Template { get; private set; }

        /// <summary>
        /// 窗口标题
        /// </summary>
        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        /// <summary>
        /// 模板名称
        /// </summary>
        public string TemplateName
        {
            get => _templateName;
            set => SetProperty(ref _templateName, value);
        }

        /// <summary>
        /// 选中的分类
        /// </summary>
        public string SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        /// <summary>
        /// 分类列表
        /// </summary>
        public ObservableCollection<string> Categories { get; } = new(TemplateCategories.DefaultCategories);

        /// <summary>
        /// 证型
        /// </summary>
        public string Syndrome
        {
            get => _syndrome;
            set => SetProperty(ref _syndrome, value);
        }

        /// <summary>
        /// 治则治法
        /// </summary>
        public string TreatmentPrinciple
        {
            get => _treatmentPrinciple;
            set => SetProperty(ref _treatmentPrinciple, value);
        }

        /// <summary>
        /// 诊断
        /// </summary>
        public string Diagnosis
        {
            get => _diagnosis;
            set => SetProperty(ref _diagnosis, value);
        }

        /// <summary>
        /// 用法用量
        /// </summary>
        public string Usage
        {
            get => _usage;
            set => SetProperty(ref _usage, value);
        }

        /// <summary>
        /// 剂数
        /// </summary>
        public int DosageCount
        {
            get => _dosageCount;
            set => SetProperty(ref _dosageCount, value);
        }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        /// <summary>
        /// 是否公开
        /// </summary>
        public bool IsPublic
        {
            get => _isPublic;
            set => SetProperty(ref _isPublic, value);
        }

        /// <summary>
        /// 药材项目
        /// </summary>
        public ObservableCollection<PrescriptionTemplateItem> HerbItems
        {
            get => _herbItems;
            set => SetProperty(ref _herbItems, value);
        }

        /// <summary>
        /// 药材数量
        /// </summary>
        public int HerbCount => HerbItems.Count;

        /// <summary>
        /// 预估价格
        /// </summary>
        public decimal EstimatedPrice => HerbItems.Sum(h => h.Quantity * h.EstimatedPrice);

        #endregion

        #region 命令

        public ICommand RemoveHerbCommand { get; }

        #endregion

        #region 构造函数

        public PrescriptionTemplateEditorDialog()
        {
            InitializeComponent();
            DataContext = this;
            
            RemoveHerbCommand = new DelegateCommand<PrescriptionTemplateItem>(RemoveHerb);
            
            HerbItems.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(HerbCount));
                OnPropertyChanged(nameof(EstimatedPrice));
            };
        }

        /// <summary>
        /// 编辑现有模板
        /// </summary>
        public PrescriptionTemplateEditorDialog(PrescriptionTemplate template) : this()
        {
            WindowTitle = "编辑处方模板";
            LoadTemplate(template);
        }

        #endregion

        #region 事件处理

        private void AddHerb_Click(object sender, RoutedEventArgs e)
        {
            // 打开药材选择对话框
            var dialog = new HerbSelectionDialog
            {
                Owner = this
            };
            
            if (dialog.ShowDialog() == true)
            {
                var vm = dialog.DataContext as HerbSelectionDialogViewModel;
                if (vm?.SelectedHerb != null)
                {
                    // 检查是否已经添加了该药材
                    if (HerbItems.Any(h => h.HerbId == vm.SelectedHerb.Id))
                    {
                        MessageBox.Show($"药材【{vm.SelectedHerb.Name}】已经在配方中", "提示", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    
                    var newItem = new PrescriptionTemplateItem
                    {
                        HerbId = vm.SelectedHerb.Id,
                        HerbName = vm.SelectedHerb.Name,
                        Quantity = 10, // 默认数量
                        Unit = vm.SelectedHerb.Unit,
                        EstimatedPrice = vm.SelectedHerb.Price,
                        SortOrder = HerbItems.Count + 1
                    };
                    
                    HerbItems.Add(newItem);
                    
                    OnPropertyChanged(nameof(HerbCount));
                    OnPropertyChanged(nameof(EstimatedPrice));
                }
            }
        }

        private void ClearHerbs_Click(object sender, RoutedEventArgs e)
        {
            if (HerbItems.Any())
            {
                var result = MessageBox.Show("确定要清空所有药材吗？", "确认", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    HerbItems.Clear();
                    OnPropertyChanged(nameof(HerbCount));
                    OnPropertyChanged(nameof(EstimatedPrice));
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // 验证必填字段
            if (string.IsNullOrWhiteSpace(TemplateName))
            {
                MessageBox.Show("请输入模板名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!HerbItems.Any())
            {
                MessageBox.Show("请至少添加一味药材", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 创建模板对象
            Template = new PrescriptionTemplate
            {
                Id = Guid.NewGuid(),
                Name = TemplateName,
                Category = SelectedCategory,
                Syndrome = Syndrome,
                TreatmentPrinciple = TreatmentPrinciple,
                Diagnosis = Diagnosis,
                Usage = Usage,
                DosageCount = DosageCount,
                Remark = Remark,
                IsPublic = IsPublic,
                IsActive = true,
                Items = HerbItems.ToList()
            };

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载模板数据
        /// </summary>
        private void LoadTemplate(PrescriptionTemplate template)
        {
            TemplateName = template.Name;
            SelectedCategory = template.Category;
            Syndrome = template.Syndrome;
            TreatmentPrinciple = template.TreatmentPrinciple;
            Diagnosis = template.Diagnosis;
            Usage = template.Usage;
            DosageCount = template.DosageCount;
            Remark = template.Remark;
            IsPublic = template.IsPublic;
            
            HerbItems.Clear();
            foreach (var item in template.Items)
            {
                HerbItems.Add(item);
            }
        }

        /// <summary>
        /// 移除药材
        /// </summary>
        private void RemoveHerb(PrescriptionTemplateItem? item)
        {
            if (item != null)
            {
                HerbItems.Remove(item);
                OnPropertyChanged(nameof(HerbCount));
                OnPropertyChanged(nameof(EstimatedPrice));
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null!)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}