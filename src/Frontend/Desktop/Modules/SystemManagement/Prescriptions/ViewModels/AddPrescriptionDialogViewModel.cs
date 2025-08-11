using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Prism.Commands;
using Prism.Mvvm;

using LYBT.Desktop.Core.Interfaces.Services;
using Prism.Dialogs;
using LYBT.Desktop.Core.Extensions;
namespace LYBT.Desktop.Admin.Prescriptions.ViewModels
{
    /// <summary>
    /// 新增处方对话框视图模型
    /// </summary>
    public class AddPrescriptionDialogViewModel : BindableBase
    {
        private readonly IDialogService _commonDialogService;
        private readonly IPrescriptionService _prescriptionService;
        private readonly IPrescriptionValidationService _validationService;
        private readonly IHerbService _herbService;
        private readonly IUserSessionManager _userSessionManager;
        private readonly IPatientService _patientService;

        #region 属性

        private string _patientName = string.Empty;
        private Guid? _patientId = null;
        private string _doctorName = string.Empty;
        private string _diagnosis = string.Empty;
        private int _dosageCount = 1;
        private string _usage = string.Empty;
        private string _remark = string.Empty;
        private bool _isLoading = false;
        private bool _isSaving = false;

        /// <summary>患者姓名</summary>
        public string PatientName
        {
            get => _patientName;
            set
            {
                if (SetProperty(ref _patientName, value))
                {
                    UpdateCanSaveState();
                }
            }
        }

        /// <summary>医生姓名</summary>
        public string DoctorName
        {
            get => _doctorName;
            set
            {
                if (SetProperty(ref _doctorName, value))
                {
                    UpdateCanSaveState();
                }
            }
        }

        /// <summary>诊断信息</summary>
        public string Diagnosis
        {
            get => _diagnosis;
            set
            {
                if (SetProperty(ref _diagnosis, value))
                {
                    UpdateCanSaveState();
                }
            }
        }

        /// <summary>剂数</summary>
        public int DosageCount
        {
            get => _dosageCount;
            set
            {
                if (SetProperty(ref _dosageCount, value))
                {
                    UpdateCanSaveState();
                }
            }
        }

        /// <summary>服用方法</summary>
        public string Usage
        {
            get => _usage;
            set => SetProperty(ref _usage, value);
        }

        /// <summary>备注</summary>
        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>是否正在保存</summary>
        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
        }

        /// <summary>处方项目集合</summary>
        public ObservableCollection<PrescriptionItemEditModel> Items { get; }

        /// <summary>总价</summary>
        public decimal TotalPrice => Items.Sum(item => item.Subtotal);

        /// <summary>是否可以保存</summary>
        public bool CanSave => _patientId.HasValue &&
                               !string.IsNullOrWhiteSpace(PatientName) &&
                               !string.IsNullOrWhiteSpace(DoctorName) &&
                               !string.IsNullOrWhiteSpace(Diagnosis) &&
                               Items.Any() &&
                               DosageCount > 0 &&
                               !IsSaving;

        #endregion

        #region 命令

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand SaveAndContinueCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand AddItemCommand { get; }
        public DelegateCommand UseTemplateCommand { get; }
        public DelegateCommand<PrescriptionItemEditModel> RemoveItemCommand { get; }
        public DelegateCommand<PrescriptionItemEditModel> SelectHerbCommand { get; }
        public DelegateCommand ValidatePrescriptionCommand { get; }
        public DelegateCommand SelectPatientCommand { get; }

        #endregion

        public Action? CloseDialogCallback { get; set; }
        public Action<PrescriptionDto>? SaveSuccessCallback { get; set; }

        public AddPrescriptionDialogViewModel(
            IPrescriptionService prescriptionService,
            IDialogService commonDialogService,
            IPrescriptionValidationService validationService,
            IHerbService herbService,
            IUserSessionManager userSessionManager,
            IPatientService patientService)
        {
            _commonDialogService = commonDialogService;
            _prescriptionService = prescriptionService;
            _validationService = validationService;
            _herbService = herbService;
            _userSessionManager = userSessionManager;
            _patientService = patientService;

            Items = new ObservableCollection<PrescriptionItemEditModel>();

            // 先初始化命令
            SaveCommand = new DelegateCommand(async () => await ExecuteSaveAsync(), () => CanSave);
            SaveAndContinueCommand = new DelegateCommand(async () => await ExecuteSaveAndContinueAsync(), () => CanSave);
            CancelCommand = new DelegateCommand(ExecuteCancel);
            AddItemCommand = new DelegateCommand(ExecuteAddItem);
            UseTemplateCommand = new DelegateCommand(ExecuteUseTemplate);
            RemoveItemCommand = new DelegateCommand<PrescriptionItemEditModel>(ExecuteRemoveItem);
            SelectHerbCommand = new DelegateCommand<PrescriptionItemEditModel>(ExecuteSelectHerb);
            ValidatePrescriptionCommand = new DelegateCommand(async () => await ExecuteValidatePrescriptionAsync(), () => Items.Any());
            SelectPatientCommand = new DelegateCommand(ExecuteSelectPatient);

            // 然后添加事件处理器
            Items.CollectionChanged += (s, e) =>
            {
                RaisePropertyChanged(nameof(TotalPrice));
                RaisePropertyChanged(nameof(CanSave));
                SaveCommand?.RaiseCanExecuteChanged();
                SaveAndContinueCommand?.RaiseCanExecuteChanged();
                ValidatePrescriptionCommand?.RaiseCanExecuteChanged();
            };

            // 添加默认的处方项目
            ExecuteAddItem();
        }

        private async Task ExecuteSaveAsync()
        {
            if (!await SavePrescriptionAsync()) return;
            CloseDialogCallback?.Invoke();
        }

        private async Task ExecuteSaveAndContinueAsync()
        {
            if (!await SavePrescriptionAsync()) return;

            // 清空表单，准备下一个处方
            ClearForm();
            await _commonDialogService.ShowInformationAsync("处方保存成功，可以继续添加新处方", "成功");
        }

        private async Task<bool> SavePrescriptionAsync()
        {
            if (!CanSave) return false;

            try
            {
                IsSaving = true;

                // 获取当前用户（医生）信息
                var currentUser = _userSessionManager.CurrentUser;
                if (currentUser == null)
                {
                    await _commonDialogService.ShowErrorAsync("无法获取当前用户信息", "错误");
                    return false;
                }

                // 检查是否选择了患者
                if (!_patientId.HasValue)
                {
                    await _commonDialogService.ShowWarningAsync("请先选择患者", "提示");
                    return false;
                }

                // 创建处方请求对象
                var request = new PrescriptionCreateDto
                {
                    PatientId = _patientId.Value, // 使用选中的患者ID
                    DoctorId = currentUser.Id,  // 使用当前登录用户作为医生
                    Diagnosis = Diagnosis,
                    DosageCount = DosageCount,
                    Usage = Usage,
                    Remark = Remark,
                    Items = Items.Where(item => item.IsValid).Select(item => new PrescriptionItemCreateDto
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        UnitPrice = item.Price,
                        Subtotal = item.Subtotal,
                        Usage = null, // 单项用法，可选
                        Note = null   // 单项备注，可选
                    }).ToList()
                };

                // 调用实际的API
                var response = await _prescriptionService.CreateAsync(request);

                if (response.IsSuccess && response.Data != null)
                {
                    SaveSuccessCallback?.Invoke(response.Data);
                    await _commonDialogService.ShowInformationAsync("处方保存成功", "成功");
                    return true;
                }
                else
                {
                    var errorMsg = response.ErrorMessage ?? "保存处方失败";
                    await _commonDialogService.ShowErrorAsync(errorMsg, "错误");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"保存处方失败: {ex.Message}", "错误").GetAwaiter().GetResult();
                return false;
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void ExecuteCancel()
        {
            var result = _commonDialogService.ShowConfirmationAsync("确定要取消新增处方吗？未保存的数据将丢失。", "确认取消").GetAwaiter().GetResult();

            if (result)
            {
                CloseDialogCallback?.Invoke();
            }
        }

        private void ExecuteAddItem()
        {
            var newItem = new PrescriptionItemEditModel();
            newItem.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PrescriptionItemEditModel.Quantity) ||
                    e.PropertyName == nameof(PrescriptionItemEditModel.Price))
                {
                    RaisePropertyChanged(nameof(TotalPrice));
                }
            };

            Items.Add(newItem);
        }

        private void ExecuteRemoveItem(PrescriptionItemEditModel item)
        {
            if (item == null) return;

            var result = _commonDialogService.ShowConfirmationAsync($"确定要移除药材 {item.HerbName} 吗？", "确认移除").GetAwaiter().GetResult();

            if (result)
            {
                Items.Remove(item);
            }
        }

        /// <summary>
        /// 使用模板创建处方
        /// </summary>
        private void ExecuteUseTemplate()
        {
            try
            {
                // 创建模板管理对话框
                var templateDialog = new Views.PrescriptionTemplateManagementDialog
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };

                if (templateDialog.ShowDialog() == true && templateDialog.SelectedTemplate != null)
                {
                    var template = templateDialog.SelectedTemplate;

                    // 询问是否清空现有药材
                    var clearExisting = false;
                    if (Items.Any())
                    {
                        clearExisting = _commonDialogService.ShowConfirmationAsync(
                            "应用模板会替换当前的药材配方，是否继续？", "确认应用模板").GetAwaiter().GetResult();
                        
                        if (!clearExisting)
                            return;
                    }

                    // 清空现有项目
                    if (clearExisting || !Items.Any())
                    {
                        Items.Clear();
                    }

                    // 应用模板数据
                    ApplyTemplateToForm(template);

                    // 添加药材项目
                    foreach (var templateItem in template.Items)
                    {
                        var newItem = new PrescriptionItemEditModel
                        {
                            HerbId = templateItem.HerbId,
                            HerbName = templateItem.HerbName,
                            Quantity = templateItem.Quantity,
                            Unit = templateItem.Unit,
                            Price = templateItem.EstimatedPrice
                        };

                        // 绑定属性变化事件
                        newItem.PropertyChanged += (sender, e) =>
                        {
                            if (e.PropertyName == nameof(PrescriptionItemEditModel.Quantity) ||
                                e.PropertyName == nameof(PrescriptionItemEditModel.Price))
                            {
                                RaisePropertyChanged(nameof(TotalPrice));
                            }
                        };

                        Items.Add(newItem);
                    }

                    // 增加模板使用次数（这里应该调用服务更新）
                    // TODO: 调用模板服务增加使用次数

                    _commonDialogService.ShowInformationAsync($"成功应用模板【{template.Name}】", "模板应用成功").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"应用模板失败：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// 应用模板数据到表单
        /// </summary>
        private void ApplyTemplateToForm(PrescriptionTemplate template)
        {
            // 应用诊断信息
            if (!string.IsNullOrWhiteSpace(template.Diagnosis))
            {
                Diagnosis = template.Diagnosis;
            }

            // 应用用法用量
            if (!string.IsNullOrWhiteSpace(template.Usage))
            {
                Usage = template.Usage;
            }

            // 应用剂数
            if (template.DosageCount > 0)
            {
                DosageCount = template.DosageCount;
            }

            // 应用备注
            if (!string.IsNullOrWhiteSpace(template.Remark))
            {
                Remark = template.Remark;
            }
        }

        private async void ExecuteSelectHerb(PrescriptionItemEditModel item)
        {
            if (item == null) return;

            try
            {
                // 获取可用药材列表
                var availableHerbs = await _herbService.GetAvailableHerbsAsync();
                if (availableHerbs == null || !availableHerbs.Any())
                {
                    await _commonDialogService.ShowInformationAsync("暂无可用药材", "提示");
                    return;
                }

                // 创建并显示药材选择对话框
                var dialog = new Views.HerbSelectionDialog();
                var viewModel = new HerbSelectionDialogViewModel();
                viewModel.Initialize(availableHerbs, Items.Select(i => i.HerbId).ToList());
                dialog.DataContext = viewModel;
                dialog.Owner = System.Windows.Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    var selectedHerb = viewModel.GetSelectedHerb();
                    if (selectedHerb != null)
                    {
                        // 更新当前项的药材信息
                        item.HerbId = selectedHerb.Id;
                        item.HerbName = selectedHerb.Name;
                        item.Unit = selectedHerb.Unit ?? "g";
                        item.Price = selectedHerb.Price;
                        
                        // 刷新界面
                        RaisePropertyChanged(nameof(Items));
                        RaisePropertyChanged(nameof(TotalPrice));
                    }
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"选择药材失败: {ex.Message}", "错误");
            }
        }

        /// <summary>
        /// 执行处方质量验证
        /// </summary>
        private async Task ExecuteValidatePrescriptionAsync()
        {
            if (!Items.Any())
            {
                await _commonDialogService.ShowWarningAsync("请先添加处方药材", "无法验证");
                return;
            }

            try
            {
                IsLoading = true;

                // 将处方项目转换为验证用的模型
                var prescriptionItems = Items.Where(i => i.IsValid).Select(item => new PrescriptionItemInfo
                {
                    HerbId = item.HerbId,
                    HerbName = item.HerbName,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    UnitPrice = item.Price
                }).ToList();

                // 创建患者信息（模拟数据，实际应从患者表单获取）
                var patientInfo = new PatientValidationInfo
                {
                    Age = 35, // 模拟年龄
                    Gender = "男", // 模拟性别
                    Weight = 70, // 模拟体重
                    IsPregnant = false,
                    IsLactating = false,
                    Allergies = new List<string>(),
                    MedicalHistory = new List<string>(),
                    CurrentMedications = new List<string>()
                };

                // 执行验证
                var validationResult = await _validationService.ValidatePrescriptionAsync(
                    prescriptionItems, 
                    patientInfo, 
                    Diagnosis);

                // 显示验证结果
                await DisplayValidationResult(validationResult);
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"处方验证失败: {ex.Message}", "验证错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 显示验证结果
        /// </summary>
        private async Task DisplayValidationResult(PrescriptionValidationResult result)
        {
            var resultText = $"处方质量验证结果\n\n";
            resultText += $"质量等级：{GetQualityLevelText(result.QualityLevel)}\n";
            resultText += $"质量评分：{result.QualityScore}分\n";
            resultText += $"是否可开方：{(result.CanPrescribe ? "是" : "否")}\n\n";

            if (result.Errors.Any())
            {
                resultText += $"严重错误 ({result.Errors.Count}个)：\n";
                foreach (var error in result.Errors.Take(3)) // 只显示前3个
                {
                    resultText += $"• {error.Message}\n";
                }
                if (result.Errors.Count > 3)
                    resultText += $"... 还有 {result.Errors.Count - 3} 个错误\n";
                resultText += "\n";
            }

            if (result.Warnings.Any())
            {
                resultText += $"警告 ({result.Warnings.Count}个)：\n";
                foreach (var warning in result.Warnings.Take(3)) // 只显示前3个
                {
                    resultText += $"• {warning.Message}\n";
                }
                if (result.Warnings.Count > 3)
                    resultText += $"... 还有 {result.Warnings.Count - 3} 个警告\n";
                resultText += "\n";
            }

            if (result.Suggestions.Any())
            {
                resultText += $"改进建议 ({result.Suggestions.Count}个)：\n";
                foreach (var suggestion in result.Suggestions.Take(2)) // 只显示前2个
                {
                    resultText += $"• {suggestion.Content}\n";
                }
                if (result.Suggestions.Count > 2)
                    resultText += $"... 还有 {result.Suggestions.Count - 2} 个建议\n";
            }

            if (result.Summary != null)
            {
                resultText += $"\n总结：{result.Summary}";
            }

            // 根据结果类型选择不同的对话框
            if (result.Errors.Any())
            {
                await _commonDialogService.ShowErrorAsync(resultText, "处方验证结果");
            }
            else if (result.Warnings.Any())
            {
                await _commonDialogService.ShowWarningAsync(resultText, "处方验证结果");
            }
            else
            {
                await _commonDialogService.ShowInformationAsync(resultText, "处方验证结果");
            }
        }

        /// <summary>
        /// 获取质量等级文本
        /// </summary>
        private string GetQualityLevelText(PrescriptionQualityLevel level)
        {
            return level switch
            {
                PrescriptionQualityLevel.Excellent => "优秀",
                PrescriptionQualityLevel.Good => "良好",
                PrescriptionQualityLevel.Fair => "一般",
                PrescriptionQualityLevel.NeedsImprovement => "需改进",
                PrescriptionQualityLevel.Poor => "不合格",
                _ => "未知"
            };
        }

        private void ExecuteSelectPatient()
        {
            // 创建并显示患者选择对话框
            var dialog = new Views.PatientSelectionDialog();
            var viewModel = new PatientSelectionDialogViewModel(_patientService);
            dialog.DataContext = viewModel;
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                var selectedPatient = viewModel.GetSelectedPatient();
                if (selectedPatient != null)
                {
                    _patientId = selectedPatient.Id;
                    PatientName = selectedPatient.Name;
                    
                    // 更新可保存状态
                    UpdateCanSaveState();
                }
            }
        }

        private void ClearForm()
        {
            _patientId = null;
            PatientName = string.Empty;
            DoctorName = string.Empty;
            Diagnosis = string.Empty;
            DosageCount = 1;
            Usage = string.Empty;
            Remark = string.Empty;

            Items.Clear();
            ExecuteAddItem();
        }

        private void UpdateCanSaveState()
        {
            RaisePropertyChanged(nameof(CanSave));
            SaveCommand?.RaiseCanExecuteChanged();
            SaveAndContinueCommand?.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// 处方项目编辑模型
    /// </summary>
    public class PrescriptionItemEditModel : BindableBase
    {
        private Guid _herbId = Guid.Empty;
        private string _herbName = string.Empty;
        private decimal _quantity = 0;
        private string _unit = "g";
        private decimal _price = 0;

        /// <summary>药材ID</summary>
        public Guid HerbId
        {
            get => _herbId;
            set => SetProperty(ref _herbId, value);
        }

        /// <summary>药材名称</summary>
        public string HerbName
        {
            get => _herbName;
            set => SetProperty(ref _herbName, value);
        }

        /// <summary>用量</summary>
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    RaisePropertyChanged(nameof(Subtotal));
                }
            }
        }

        /// <summary>单位</summary>
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        /// <summary>单价</summary>
        public decimal Price
        {
            get => _price;
            set
            {
                if (SetProperty(ref _price, value))
                {
                    RaisePropertyChanged(nameof(Subtotal));
                }
            }
        }

        /// <summary>小计</summary>
        public decimal Subtotal => Quantity * Price;

        /// <summary>是否有效</summary>
        public bool IsValid => HerbId != Guid.Empty &&
                               !string.IsNullOrWhiteSpace(HerbName) &&
                               Quantity > 0 &&
                               Price >= 0;
    }
}