using System;
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
    /// 编辑处方对话框视图模型
    /// </summary>
    public class EditPrescriptionDialogViewModel : BindableBase
    {
        private readonly IDialogService _commonDialogService;

        private readonly IPrescriptionService _prescriptionService;
        private readonly IHerbService _herbService;
        private readonly Guid _prescriptionId;

        #region 属性

        private PrescriptionDetailDto? _originalPrescription;
        private string _patientName = string.Empty;
        private string _doctorName = string.Empty;
        private string _diagnosis = string.Empty;
        private int _dosageCount = 1;
        private string _usage = string.Empty;
        private string _remark = string.Empty;
        private bool _isLoading = true;
        private bool _isSaving = false;

        /// <summary>原始处方数据</summary>
        public PrescriptionDetailDto? OriginalPrescription
        {
            get => _originalPrescription;
            set => SetProperty(ref _originalPrescription, value);
        }

        /// <summary>患者姓名</summary>
        public string PatientName
        {
            get => _patientName;
            set => SetProperty(ref _patientName, value);
        }

        /// <summary>医生姓名</summary>
        public string DoctorName
        {
            get => _doctorName;
            set => SetProperty(ref _doctorName, value);
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
            set
            {
                if (SetProperty(ref _usage, value))
                {
                    UpdateCanSaveState();
                }
            }
        }

        /// <summary>备注</summary>
        public string Remark
        {
            get => _remark;
            set
            {
                if (SetProperty(ref _remark, value))
                {
                    UpdateCanSaveState();
                }
            }
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

        /// <summary>处方编号</summary>
        public string PrescriptionNumber => OriginalPrescription != null
            ? $"CF{OriginalPrescription.CreateTime:yyyyMMdd}{OriginalPrescription.Id.ToString("N")[..6].ToUpper()}"
            : string.Empty;

        /// <summary>是否可以保存</summary>
        public bool CanSave => !string.IsNullOrWhiteSpace(PatientName) &&
                               !string.IsNullOrWhiteSpace(DoctorName) &&
                               !string.IsNullOrWhiteSpace(Diagnosis) &&
                               Items.Any() &&
                               DosageCount > 0 &&
                               !IsSaving &&
                               !IsLoading &&
                               HasChanges;

        /// <summary>是否有变更</summary>
        public bool HasChanges
        {
            get
            {
                if (OriginalPrescription == null) return false;

                return Diagnosis != OriginalPrescription.Diagnosis ||
                       DosageCount != OriginalPrescription.DosageCount || // DTO属性已存在
                       Usage != (OriginalPrescription.Usage ?? string.Empty) || // DTO属性已存在
                       Remark != (OriginalPrescription.Remark ?? string.Empty) ||
                       HasItemChanges();
            }
        }

        #endregion

        #region 命令

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand AddItemCommand { get; }
        public DelegateCommand<PrescriptionItemEditModel> RemoveItemCommand { get; }
        public DelegateCommand<PrescriptionItemEditModel> SelectHerbCommand { get; }

        #endregion

        public Action? CloseDialogCallback { get; set; }
        public Action<PrescriptionDto>? SaveSuccessCallback { get; set; } // 使用PrescriptionDto作为回调参数

        public EditPrescriptionDialogViewModel(IPrescriptionService prescriptionService, Guid prescriptionId,
            IDialogService commonDialogService, IHerbService herbService)
        {
            _commonDialogService = commonDialogService;
            _prescriptionService = prescriptionService;
            _herbService = herbService;
            _prescriptionId = prescriptionId;

            Items = new ObservableCollection<PrescriptionItemEditModel>();

            // 先初始化命令
            SaveCommand = new DelegateCommand(async () => await ExecuteSaveAsync(), () => CanSave);
            CancelCommand = new DelegateCommand(ExecuteCancel);
            AddItemCommand = new DelegateCommand(ExecuteAddItem);
            RemoveItemCommand = new DelegateCommand<PrescriptionItemEditModel>(ExecuteRemoveItem);
            SelectHerbCommand = new DelegateCommand<PrescriptionItemEditModel>(ExecuteSelectHerb);

            // 然后添加事件处理器
            Items.CollectionChanged += (s, e) =>
            {
                RaisePropertyChanged(nameof(TotalPrice));
                RaisePropertyChanged(nameof(HasChanges));
                RaisePropertyChanged(nameof(CanSave));
                SaveCommand?.RaiseCanExecuteChanged();
            };

            // 加载处方详情
            _ = LoadPrescriptionAsync();
        }

        private async Task LoadPrescriptionAsync()
        {
            try
            {
                IsLoading = true;
                var response = await _prescriptionService.GetByIdAsync(_prescriptionId);

                if (response.IsSuccess && response.Data != null)
                {
                    OriginalPrescription = response.Data;

                    // 填充表单数据
                    PatientName = OriginalPrescription.PatientName ?? "患者" + OriginalPrescription.PatientId.ToString()[..8]; // TODO: 从患者服务获取完整姓名
                    DoctorName = OriginalPrescription.DoctorName ?? "医生" + OriginalPrescription.DoctorId.ToString()[..8];   // TODO: 从用户服务获取完整姓名
                    Diagnosis = OriginalPrescription.Diagnosis ?? string.Empty;
                    DosageCount = OriginalPrescription.DosageCount; // DTO属性已存在
                    Usage = OriginalPrescription.Usage ?? string.Empty; // DTO属性已存在
                    Remark = OriginalPrescription.Remark ?? string.Empty;

                    // 填充药材项目
                    Items.Clear();
                    if (OriginalPrescription.Items != null)
                    {
                        foreach (var item in OriginalPrescription.Items)
                        {
                            var editModel = new PrescriptionItemEditModel
                            {
                                HerbId = item.HerbId,
                                HerbName = item.HerbName,
                                Quantity = item.Quantity,
                                Unit = item.Unit ?? "g",
                                Price = item.UnitPrice // DTO属性已存在
                            };

                            editModel.PropertyChanged += (s, e) =>
                            {
                                if (e.PropertyName == nameof(PrescriptionItemEditModel.Quantity) ||
                                    e.PropertyName == nameof(PrescriptionItemEditModel.Price))
                                {
                                    RaisePropertyChanged(nameof(TotalPrice));
                                    RaisePropertyChanged(nameof(HasChanges));
                                    RaisePropertyChanged(nameof(CanSave));
                                    SaveCommand.RaiseCanExecuteChanged();
                                }
                            };

                            Items.Add(editModel);
                        }
                    }
                }
                else
                {
                    var error = response.ErrorMessage ?? "获取处方详情失败";
                    _commonDialogService.ShowErrorAsync($"加载处方详情失败: {error}", "错误").GetAwaiter().GetResult();
                    CloseDialogCallback?.Invoke();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"加载处方详情失败: {ex.Message}", "错误").GetAwaiter().GetResult();
                CloseDialogCallback?.Invoke();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExecuteSaveAsync()
        {
            if (!CanSave) return;

            try
            {
                IsSaving = true;

                // 创建更新请求对象
                var request = new PrescriptionEditDto
                {
                    Id = _prescriptionId,
                    Diagnosis = Diagnosis,
                    DosageCount = DosageCount,
                    Advice = Usage, // 使用Advice字段存储用药建议
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
                var response = await _prescriptionService.UpdateAsync(request);

                if (response.IsSuccess && response.Data != null)
                {
                    await _commonDialogService.ShowInformationAsync("处方更新成功", "成功");
                    SaveSuccessCallback?.Invoke(response.Data);
                    CloseDialogCallback?.Invoke();
                }
                else
                {
                    var errorMsg = response.ErrorMessage ?? "更新处方失败";
                    await _commonDialogService.ShowErrorAsync(errorMsg, "错误");
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"保存处方失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void ExecuteCancel()
        {
            if (HasChanges)
            {
                var result = _commonDialogService.ShowConfirmationAsync("确定要取消编辑吗？未保存的数据将丢失。", "确认取消").GetAwaiter().GetResult();

                if (!result)
                    return;
            }

            CloseDialogCallback?.Invoke();
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
                    RaisePropertyChanged(nameof(HasChanges));
                    RaisePropertyChanged(nameof(CanSave));
                    SaveCommand.RaiseCanExecuteChanged();
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

        private bool HasItemChanges()
        {
            if (OriginalPrescription?.Items == null) return Items.Any();

            var originalItems = OriginalPrescription.Items.ToList();

            if (originalItems.Count != Items.Count) return true;

            for (int i = 0; i < originalItems.Count; i++)
            {
                var original = originalItems[i];
                var current = Items[i];

                if (original.HerbId != current.HerbId ||
                    original.Quantity != current.Quantity ||
                    original.UnitPrice != current.Price) // DTO属性已存在
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateCanSaveState()
        {
            RaisePropertyChanged(nameof(HasChanges));
            RaisePropertyChanged(nameof(CanSave));
            SaveCommand.RaiseCanExecuteChanged();
        }
    }
}