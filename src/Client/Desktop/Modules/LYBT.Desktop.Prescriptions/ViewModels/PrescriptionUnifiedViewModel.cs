using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace LYBT.Desktop.Prescriptions.ViewModels
{
    /// <summary>
    /// 统一处方ViewModel：整合8列快速输入和列表详细编辑两种模式
    /// Epic #1701: PrescriptionView + PrescriptionEditorDialog合并
    /// </summary>
    public class PrescriptionUnifiedViewModel : BindableBase, INavigationAware
    {
        #region 字段
        private readonly IRegionManager _regionManager;
        private bool _isDetailedListMode;
        private bool _isViewMode;
        private string _patientInfo;
        private string _prescriptionNumber;
        private string _diagnosis;
        private int _dosageCount = 7;
        private string _usage = "水煎服，日一剂，分早晚服";
        private string _advice;
        private decimal _totalPrice;
        private int _status;
        #endregion

        #region 基础属性
        public string PatientInfo
        {
            get => _patientInfo;
            set => SetProperty(ref _patientInfo, value);
        }

        public string PrescriptionNumber
        {
            get => _prescriptionNumber;
            set => SetProperty(ref _prescriptionNumber, value);
        }

        public string Diagnosis
        {
            get => _diagnosis;
            set => SetProperty(ref _diagnosis, value);
        }

        public int DosageCount
        {
            get => _dosageCount;
            set => SetProperty(ref _dosageCount, value);
        }

        public string Usage
        {
            get => _usage;
            set => SetProperty(ref _usage, value);
        }

        public string Advice
        {
            get => _advice;
            set => SetProperty(ref _advice, value);
        }

        public decimal TotalPrice
        {
            get => _totalPrice;
            set => SetProperty(ref _totalPrice, value);
        }

        public int Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
        #endregion

        #region 8列模式属性
        public ObservableCollection<PrescriptionItemRow> ItemRows { get; set; } = new ObservableCollection<PrescriptionItemRow>();
        public ObservableCollection<HerbDto> FilteredHerbs { get; set; } = new ObservableCollection<HerbDto>();
        #endregion

        #region 列表模式属性
        public ObservableCollection<PrescriptionItemDto> PrescriptionItems { get; set; } = new ObservableCollection<PrescriptionItemDto>();
        #endregion

        #region 布局切换属性
        public bool IsDetailedListMode
        {
            get => _isDetailedListMode;
            set
            {
                if (SetProperty(ref _isDetailedListMode, value))
                {
                    OnLayoutModeChanged();
                }
            }
        }

        public string LayoutModeIcon => IsDetailedListMode ? "📋" : "⚡";
        public string LayoutModeText => IsDetailedListMode ? "列表" : "快速";
        #endregion

        #region 模式控制属性
        public bool IsViewMode
        {
            get => _isViewMode;
            set
            {
                if (SetProperty(ref _isViewMode, value))
                {
                    UpdateCommandStates();
                }
            }
        }

        public bool ShowStatusSelector => !IsViewMode;
        public bool ShowDraftButton => !IsViewMode;
        public bool ShowSaveButton => !IsViewMode;
        public bool ShowPreviewButton => !IsViewMode;
        #endregion

        #region 命令
        public DelegateCommand SaveDraftCommand { get; }
        public DelegateCommand SavePrescriptionCommand { get; }
        public DelegateCommand PreviewCommand { get; }
        public DelegateCommand CloseCommand { get; }
        public DelegateCommand AddHerbCommand { get; }
        public DelegateCommand<PrescriptionItemDto> DeleteHerbCommand { get; }
        public DelegateCommand LoadFormulaTemplateCommand { get; }
        #endregion

        #region 构造函数
        public PrescriptionUnifiedViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;

            // 初始化命令
            SaveDraftCommand = new DelegateCommand(OnSaveDraft, CanSave);
            SavePrescriptionCommand = new DelegateCommand(OnSavePrescription, CanSave);
            PreviewCommand = new DelegateCommand(OnPreview);
            CloseCommand = new DelegateCommand(OnClose);
            AddHerbCommand = new DelegateCommand(OnAddHerb);
            DeleteHerbCommand = new DelegateCommand<PrescriptionItemDto>(OnDeleteHerb);
            LoadFormulaTemplateCommand = new DelegateCommand(OnLoadFormulaTemplate);

            // 初始化数据
            InitializeData();
        }
        #endregion

        #region 初始化方法
        private void InitializeData()
        {
            // 初始化8列模式数据
            for (int i = 0; i < 10; i++)
            {
                ItemRows.Add(new PrescriptionItemRow());
            }

            // 默认使用8列快速输入模式
            IsDetailedListMode = false;
        }
        #endregion

        #region 布局切换逻辑
        private void OnLayoutModeChanged()
        {
            // 切换前同步数据
            if (IsDetailedListMode)
            {
                // 从8列模式切换到列表模式
                SyncQuickEntryToList();
            }
            else
            {
                // 从列表模式切换到8列模式
                SyncListToQuickEntry();
            }

            // 更新UI提示
            RaisePropertyChanged(nameof(LayoutModeIcon));
            RaisePropertyChanged(nameof(LayoutModeText));
        }

        private void SyncQuickEntryToList()
        {
            PrescriptionItems.Clear();
            foreach (var row in ItemRows)
            {
                if (row.Item1 != null && !string.IsNullOrWhiteSpace(row.Item1.HerbName))
                    PrescriptionItems.Add(ConvertToItemDto(row.Item1));
                if (row.Item2 != null && !string.IsNullOrWhiteSpace(row.Item2.HerbName))
                    PrescriptionItems.Add(ConvertToItemDto(row.Item2));
                if (row.Item3 != null && !string.IsNullOrWhiteSpace(row.Item3.HerbName))
                    PrescriptionItems.Add(ConvertToItemDto(row.Item3));
                if (row.Item4 != null && !string.IsNullOrWhiteSpace(row.Item4.HerbName))
                    PrescriptionItems.Add(ConvertToItemDto(row.Item4));
            }
        }

        private void SyncListToQuickEntry()
        {
            ItemRows.Clear();
            var items = PrescriptionItems.ToList();
            for (int i = 0; i < items.Count; i += 4)
            {
                var row = new PrescriptionItemRow
                {
                    Item1 = i < items.Count ? ConvertToQuickItem(items[i]) : new QuickEntryItem(),
                    Item2 = i + 1 < items.Count ? ConvertToQuickItem(items[i + 1]) : new QuickEntryItem(),
                    Item3 = i + 2 < items.Count ? ConvertToQuickItem(items[i + 2]) : new QuickEntryItem(),
                    Item4 = i + 3 < items.Count ? ConvertToQuickItem(items[i + 3]) : new QuickEntryItem()
                };
                ItemRows.Add(row);
            }
        }

        private PrescriptionItemDto ConvertToItemDto(QuickEntryItem quickItem)
        {
            return new PrescriptionItemDto
            {
                HerbName = quickItem.HerbName,
                Quantity = quickItem.Quantity,
                Unit = "克",
                Specification = "统货",
                UnitPrice = 0, // 需要从Herb库查询
                Amount = 0,
                Usage = Usage
            };
        }

        private QuickEntryItem ConvertToQuickItem(PrescriptionItemDto itemDto)
        {
            return new QuickEntryItem
            {
                HerbName = itemDto.HerbName,
                Quantity = itemDto.Quantity
            };
        }
        #endregion

        #region 命令实现
        private void OnSaveDraft()
        {
            // 同步数据
            if (!IsDetailedListMode)
                SyncQuickEntryToList();

            // TODO: 保存草稿逻辑
        }

        private void OnSavePrescription()
        {
            // 同步数据
            if (!IsDetailedListMode)
                SyncQuickEntryToList();

            // TODO: 保存处方逻辑
        }

        private void OnPreview()
        {
            // TODO: 预览逻辑
        }

        private void OnClose()
        {
            _regionManager.RequestNavigate("PrescriptionRegion", "PrescriptionsMainView");
        }

        private void OnAddHerb()
        {
            PrescriptionItems.Add(new PrescriptionItemDto());
        }

        private void OnDeleteHerb(PrescriptionItemDto item)
        {
            PrescriptionItems.Remove(item);
        }

        private void OnLoadFormulaTemplate()
        {
            // TODO: 导入验方模板逻辑
        }

        private bool CanSave()
        {
            return !IsViewMode && PrescriptionItems.Count > 0;
        }

        private void UpdateCommandStates()
        {
            SaveDraftCommand.RaiseCanExecuteChanged();
            SavePrescriptionCommand.RaiseCanExecuteChanged();
        }
        #endregion

        #region INavigationAware实现
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 获取导航参数
            var mode = navigationContext.Parameters.GetValue<string>("Mode");
            var layoutMode = navigationContext.Parameters.GetValue<string>("LayoutMode");

            // 设置模式
            if (mode == "View")
                IsViewMode = true;
            else if (mode == "Edit")
                IsViewMode = false;

            // 设置布局
            if (layoutMode == "DetailedList")
                IsDetailedListMode = true;
            else
                IsDetailedListMode = false;

            // TODO: 加载处方数据
        }
        #endregion
    }

    #region 辅助类
    public class PrescriptionItemRow : BindableBase
    {
        public QuickEntryItem Item1 { get; set; } = new QuickEntryItem();
        public QuickEntryItem Item2 { get; set; } = new QuickEntryItem();
        public QuickEntryItem Item3 { get; set; } = new QuickEntryItem();
        public QuickEntryItem Item4 { get; set; } = new QuickEntryItem();
    }

    public class QuickEntryItem : BindableBase
    {
        private string _herbName;
        private decimal _quantity;

        public string HerbName
        {
            get => _herbName;
            set => SetProperty(ref _herbName, value);
        }

        public decimal Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }
    }

    public class PrescriptionItemDto : BindableBase
    {
        public string HerbName { get; set; }
        public string Specification { get; set; }
        public string Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public string Usage { get; set; }
    }

    public class HerbDto
    {
        public string Name { get; set; }
        public string PinyinCode { get; set; }
    }
    #endregion
}
