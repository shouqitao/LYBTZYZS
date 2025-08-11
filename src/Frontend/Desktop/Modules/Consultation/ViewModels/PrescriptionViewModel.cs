using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Events;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Consultation.ViewModels;

using Prism.Dialogs;
using LYBT.Desktop.Core.Extensions;
using LYBT.Desktop.Core.Models.Consultation;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Consultation.Services;
using LYBT.Desktop.Consultation.Constants;
namespace LYBT.Desktop.Consultation.ViewModels
{
    /// <summary>
    /// 处方开具视图模型
    /// 支持单味药材添加、验方导入、价格计算等功能
    /// </summary>
    public class PrescriptionViewModel : BindableBase
    {
        #region 依赖服务

        private readonly IEventAggregator _eventAggregator;
        private readonly IPrescriptionService _prescriptionService;
        private readonly IHerbService _herbService;
        private readonly IFormulaService _formulaService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<PrescriptionViewModel> _logger;
        
        // TODO: 可以在这里添加新的服务类

        #endregion

        #region 属性

        private Guid _medicalCaseId;
        public Guid MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }

        // 处方编号
        private string _prescriptionNo = "";
        public string PrescriptionNo
        {
            get => _prescriptionNo;
            set => SetProperty(ref _prescriptionNo, value);
        }

        // 处方项目列表
        private ObservableCollection<PrescriptionItemViewModel> _prescriptionItems = new();
        public ObservableCollection<PrescriptionItemViewModel> PrescriptionItems
        {
            get => _prescriptionItems;
            set => SetProperty(ref _prescriptionItems, value);
        }

        // 选中的处方项
        private PrescriptionItemViewModel? _selectedItem;
        public PrescriptionItemViewModel? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        // 剂数（默认7剂）
        private int _dosageCount = PrescriptionConstants.DefaultDosageCount;
        public int DosageCount
        {
            get => _dosageCount;
            set
            {
                if (SetProperty(ref _dosageCount, value))
                {
                    CalculateTotalPrice();
                }
            }
        }

        // 用法用量
        private string _usage = PrescriptionConstants.DefaultUsage;
        public string Usage
        {
            get => _usage;
            set
            {
                if (SetProperty(ref _usage, value))
                {
                    OnDataChanged();
                }
            }
        }

        // 单剂价格
        private decimal _singleDosagePrice;
        public decimal SingleDosagePrice
        {
            get => _singleDosagePrice;
            set => SetProperty(ref _singleDosagePrice, value);
        }

        // 总价格
        private decimal _totalPrice;
        public decimal TotalPrice
        {
            get => _totalPrice;
            set => SetProperty(ref _totalPrice, value);
        }

        // 折扣（0.1-1.0，1.0表示无折扣）
        private decimal _discount = PrescriptionConstants.DefaultDiscount;
        public decimal Discount
        {
            get => _discount;
            set
            {
                if (SetProperty(ref _discount, value))
                {
                    CalculateTotalPrice();
                }
            }
        }

        // 折扣率显示（如：9折）
        public string DiscountText => Discount < 1.0m ? $"{Discount * 10:F1}折" : "无折扣";

        // 折后价格
        private decimal _discountedPrice;
        public decimal DiscountedPrice
        {
            get => _discountedPrice;
            set => SetProperty(ref _discountedPrice, value);
        }

        // 医嘱
        private string _medicalAdvice = "";
        public string MedicalAdvice
        {
            get => _medicalAdvice;
            set
            {
                if (SetProperty(ref _medicalAdvice, value))
                {
                    OnDataChanged();
                }
            }
        }

        // 备注
        private string _remark = "";
        public string Remark
        {
            get => _remark;
            set
            {
                if (SetProperty(ref _remark, value))
                {
                    OnDataChanged();
                }
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _hasChanges;
        public bool HasChanges
        {
            get => _hasChanges;
            set => SetProperty(ref _hasChanges, value);
        }

        // 输入提示
        public string UsageHint => PrescriptionConstants.UsageHint;
        public string MedicalAdviceHint => PrescriptionConstants.MedicalAdviceHint;
        public string RemarkHint => PrescriptionConstants.RemarkHint;

        // 常用选项（使用常量类）
        public ObservableCollection<int> CommonDosageCounts { get; } = 
            new ObservableCollection<int>(PrescriptionConstants.CommonDosageCounts);
        public ObservableCollection<string> CommonUsages { get; } = 
            new ObservableCollection<string>(PrescriptionConstants.CommonUsages);

        #endregion

        #region 命令

        public ICommand SaveCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand AddHerbCommand { get; }
        public ICommand RemoveHerbCommand { get; }
        public ICommand ImportFormulaCommand { get; }
        public ICommand ImportHistoryCommand { get; }
        public ICommand SetDiscountCommand { get; }
        public ICommand SetDosageCommand { get; }
        public ICommand GeneratePrescriptionNoCommand { get; }
        public ICommand PrintPreviewCommand { get; }

        #endregion

        #region 构造函数

        public PrescriptionViewModel(
            IEventAggregator eventAggregator,
            IPrescriptionService prescriptionService,
            IHerbService herbService,
            IFormulaService formulaService,
            IDialogService dialogService,
            ILogger<PrescriptionViewModel> logger)
        {
            _eventAggregator = eventAggregator;
            _prescriptionService = prescriptionService;
            _herbService = herbService;
            _formulaService = formulaService;
            _dialogService = dialogService;
            _logger = logger;
            // TODO: 初始化新的服务类

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => !IsLoading && PrescriptionItems.Count > 0);
            ClearCommand = new DelegateCommand(Clear, () => !IsLoading);
            AddHerbCommand = new DelegateCommand(async () => await AddHerbAsync());
            RemoveHerbCommand = new DelegateCommand<PrescriptionItemViewModel>(RemoveHerb);
            ImportFormulaCommand = new DelegateCommand(async () => await ImportFormulaAsync());
            ImportHistoryCommand = new DelegateCommand(async () => await ImportHistoryAsync());
            SetDiscountCommand = new DelegateCommand<string>(SetDiscount);
            SetDosageCommand = new DelegateCommand<string>(SetDosage);
            GeneratePrescriptionNoCommand = new DelegateCommand(GeneratePrescriptionNo);
            PrintPreviewCommand = new DelegateCommand(async () => await PrintPreviewAsync());

            // 订阅事件
            SubscribeEvents();
            
            // 监听处方项变化
            PrescriptionItems.CollectionChanged += (s, e) =>
            {
                CalculateTotalPrice();
                OnDataChanged();
            };
        }

        #endregion

        #region 初始化

        private void SubscribeEvents()
        {
            // 订阅保存步骤数据事件
            _eventAggregator.GetEvent<SaveStepDataEvent>().Subscribe(OnSaveStepData);
        }

        public async Task InitializeAsync(Guid medicalCaseId)
        {
            try
            {
                IsLoading = true;
                MedicalCaseId = medicalCaseId;
                
                // 生成处方编号
                GeneratePrescriptionNo();

                // 加载已有数据
                await LoadExistingDataAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化处方失败");
                await _dialogService.ShowErrorAsync("初始化失败: " + ex.Message, "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadExistingDataAsync()
        {
            if (MedicalCaseId == Guid.Empty) return;

            try
            {
                var result = await _prescriptionService.GetByMedicalCaseIdAsync(MedicalCaseId);
                if (result.IsSuccess && result.Data != null)
                {
                    var prescription = result.Data;
                    
                    // 加载处方项
                    PrescriptionItems.Clear();
                    foreach (var item in prescription.Items)
                    {
                        PrescriptionItems.Add(new PrescriptionItemViewModel(new Core.Models.Prescriptions.PrescriptionItem
                        {
                            HerbId = item.HerbId,
                            HerbName = item.HerbName,
                            Quantity = item.Quantity,
                            Unit = item.Unit,
                            UnitPrice = item.UnitPrice,
                            ImportSource = item.Remark
                        }));
                    }
                    
                    DosageCount = prescription.DosageCount;
                    Usage = prescription.Usage ?? "水煎服，一日三次，饭后服用";
                    MedicalAdvice = prescription.MedicalAdvice ?? "";
                    Discount = prescription.Discount;
                    PrescriptionNo = prescription.PrescriptionNo ?? $"CF{DateTime.Now:yyyyMMddHHmmss}";
                    
                    // 重置更改标记
                    HasChanges = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载处方数据失败");
            }
        }

        #endregion

        #region 数据操作

        private async Task SaveAsync()
        {
            try
            {
                IsLoading = true;

                // 构建处方数据
                var prescriptionData = new Core.Models.Prescriptions.PrescriptionData
                {
                    Items = PrescriptionItems.Select(item => item.GetModel()).ToList(),
                    Dosage = DosageCount,
                    Usage = Usage,
                    TotalPrice = DiscountedPrice,
                    Discount = Discount
                };

                // 发布步骤完成事件
                var stepData = new WorkflowStepData
                {
                    Step = Core.Models.Consultation.WorkflowStep.Prescription,
                    Data = prescriptionData
                };
                _eventAggregator.GetEvent<WorkflowStepCompletedEvent>().Publish(stepData);

                HasChanges = false;
                await _dialogService.ShowInformationAsync("处方保存成功", "成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存处方失败");
                await _dialogService.ShowErrorAsync("保存失败: " + ex.Message, "错误");
            }
            finally
            {
                IsLoading = false;
                (SaveCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            }
        }

        private void Clear()
        {
            var confirm = _dialogService.ShowConfirmationAsync(
                "确定要清空处方吗？",
                "清空确认").Result;
                
            if (confirm)
            {
                ResetToDefault();
            }
        }

        private async Task AddHerbAsync()
        {
            try
            {
                // 创建并显示药材选择对话框
                var dialog = new Views.SelectHerbDialog();
                var dialogViewModel = new SelectHerbDialogViewModel(_herbService, _dialogService);
                
                // 设置回调
                dialogViewModel.OnHerbsSelected = async (herbItems) =>
                {
                    dialog.DialogResult = true;
                    dialog.Close();
                    
                    // 添加选中的药材
                    await AddHerbItems(herbItems);
                };
                
                dialogViewModel.OnCancelled = () =>
                {
                    dialog.DialogResult = false;
                    dialog.Close();
                };
                
                dialog.DataContext = dialogViewModel;
                dialog.Owner = System.Windows.Application.Current.MainWindow;
                
                // 显示对话框
                var result = dialog.ShowDialog();
                
                if (result == true)
                {
                    _logger.LogInformation("药材添加成功");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加药材失败");
                await _dialogService.ShowErrorAsync("添加失败: " + ex.Message, "错误");
            }
        }

        private async Task AddHerbItems(dynamic herbItems)
        {
            try
            {
                var addedCount = 0;
                var updatedCount = 0;
                
                foreach (var herbItem in herbItems)
                {
                    // 检查是否已存在
                    var existing = PrescriptionItems.FirstOrDefault(p => p.HerbId == herbItem.HerbId);
                    if (existing != null)
                    {
                        // 增加剂量
                        existing.Quantity += herbItem.Quantity;
                        existing.Source += "、手动添加";
                        updatedCount++;
                    }
                    else
                    {
                        // 添加新药材
                        PrescriptionItems.Add(new PrescriptionItemViewModel(new Core.Models.Prescriptions.PrescriptionItem
                        {
                            HerbId = herbItem.HerbId,
                            HerbName = herbItem.HerbName,
                            Quantity = herbItem.Quantity,
                            Unit = herbItem.Unit,
                            UnitPrice = herbItem.UnitPrice,
                            ImportSource = "手动添加"
                        }));
                        addedCount++;
                    }
                }
                
                HasChanges = true;
                
                var message = "";
                if (addedCount > 0)
                    message += $"新增{addedCount}味药材";
                if (updatedCount > 0)
                    message += $"{(addedCount > 0 ? "，" : "")}更新{updatedCount}味药材";
                
                if (!string.IsNullOrEmpty(message))
                {
                    await _dialogService.ShowInformationAsync(message, "添加成功");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加药材项失败");
                await _dialogService.ShowErrorAsync("添加药材失败: " + ex.Message, "错误");
            }
        }

        private void RemoveHerb(PrescriptionItemViewModel? item)
        {
            if (item == null) return;
            
            var confirm = _dialogService.ShowConfirmationAsync(
                $"确定要删除药材\"{item.HerbName}\"吗？",
                "删除确认").Result;
                
            if (confirm)
            {
                PrescriptionItems.Remove(item);
                HasChanges = true;
            }
        }

        private async Task ImportFormulaAsync()
        {
            try
            {
                // TODO: 实现验方选择和导入功能
                await _dialogService.ShowInformationAsync(
                    "验方导入功能待实现",
                    "功能提示");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入验方失败");
                await _dialogService.ShowErrorAsync("导入失败: " + ex.Message, "错误");
            }
        }

        #region 辅助方法
        
        // 辅助转换方法已简化

        /// <summary>
        /// 重置到默认状态
        /// </summary>
        private void ResetToDefault()
        {
            PrescriptionItems.Clear();
            MedicalAdvice = "";
            Remark = "";
            Discount = PrescriptionConstants.DefaultDiscount;
            DosageCount = PrescriptionConstants.DefaultDosageCount;
            HasChanges = true;
        }

        /// <summary>
        /// 尝试解析并设置折扣
        /// </summary>
        private void TryParseAndSetDiscount(string? discountStr)
        {
            if (decimal.TryParse(discountStr, out var discount))
            {
                // 如果输入的是折数（如8、9），转换为小数
                if (discount > 1 && discount <= 10)
                {
                    Discount = discount / 10;
                }
                // 如果输入的是小数（如0.8、0.9）
                else if (discount > 0 && discount <= 1)
                {
                    Discount = discount;
                }
            }
        }

        /// <summary>
        /// 尝试解析并设置剂数
        /// </summary>
        private void TryParseAndSetDosage(string? dosageStr)
        {
            if (int.TryParse(dosageStr, out var dosage) && 
                dosage >= PrescriptionConstants.MinDosageCount && 
                dosage <= PrescriptionConstants.MaxDosageCount)
            {
                DosageCount = dosage;
            }
        }
        
        #endregion

        private async Task ImportHistoryAsync()
        {
            try
            {
                // TODO: 实现历史处方选择和导入
                await _dialogService.ShowInformationAsync(
                    "从患者历史处方中导入",
                    "功能提示");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入历史处方失败");
                await _dialogService.ShowErrorAsync("导入失败: " + ex.Message, "错误");
            }
        }

        private void SetDiscount(string? discountStr) =>
            TryParseAndSetDiscount(discountStr);

        private void SetDosage(string? dosageStr) =>
            TryParseAndSetDosage(dosageStr);

        private void GeneratePrescriptionNo()
        {
            // 使用常量中定义的格式
            var sequence = new Random().Next(1, 999);
            PrescriptionNo = string.Format(PrescriptionConstants.PrescriptionNumberFormat,
                DateTime.Now, sequence);
        }

        private async Task PrintPreviewAsync()
        {
            try
            {
                // TODO: 实现处方打印预览
                await _dialogService.ShowInformationAsync(
                    "处方打印预览功能",
                    "功能提示");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打印预览失败");
                await _dialogService.ShowErrorAsync("打印失败: " + ex.Message, "错误");
            }
        }

        #endregion

        #region 价格计算

        private void CalculateTotalPrice()
        {
            // 计算单剂价格
            SingleDosagePrice = PrescriptionItems.Sum(item => item.Subtotal);
            
            // 计算总价
            TotalPrice = SingleDosagePrice * DosageCount;
            
            // 计算折后价格
            DiscountedPrice = TotalPrice * Discount;
            
            // 更新命令状态
            (SaveCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }

        #endregion

        #region 事件处理

        private void OnDataChanged()
        {
            HasChanges = true;
            (SaveCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }

        private void OnSaveStepData(Core.Models.Consultation.WorkflowStep step)
        {
            if (step == Core.Models.Consultation.WorkflowStep.Prescription)
            {
                // 自动保存当前数据
                _ = SaveAsync();
            }
        }

        #endregion

        #region 数据导出

        /// <summary>
        /// 获取处方数据用于工作流
        /// </summary>
        public Core.Models.Prescriptions.PrescriptionData GetPrescriptionData()
        {
            return new Core.Models.Prescriptions.PrescriptionData
            {
                Items = PrescriptionItems.Select(item => item.GetModel()).ToList(),
                Dosage = DosageCount,
                Usage = Usage,
                TotalPrice = DiscountedPrice,
                Discount = Discount
            };
        }

        #endregion

        // PrescriptionItemViewModel已移动到独立文件
        // 参见：PrescriptionItemViewModel.cs
    }
}