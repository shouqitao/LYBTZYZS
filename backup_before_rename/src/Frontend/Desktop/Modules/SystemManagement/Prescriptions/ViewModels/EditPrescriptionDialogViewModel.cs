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
        // private readonly IHerbsApiService _herbService; // TODO: 等待IHerbsApiService实现
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
                       DosageCount != 1 || // TODO: != OriginalPrescription.DosageCount - 等待DTO属性添加
                       Usage != string.Empty || // TODO: != (OriginalPrescription.Usage ?? string.Empty) - 等待DTO属性添加
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
        public Action<object>? SaveSuccessCallback { get; set; } // TODO: 替换为实际的UpdatePrescriptionRequest类型

        public EditPrescriptionDialogViewModel(IPrescriptionService prescriptionService, Guid prescriptionId,
            IDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            _prescriptionService = prescriptionService;
            // _herbService = herbService; // TODO: 等待IHerbsApiService实现
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
                    PatientName = "患者" + OriginalPrescription.PatientId.ToString()[..8]; // TODO: 从其他服务获取患者姓名
                    DoctorName = "医生" + OriginalPrescription.DoctorId.ToString()[..8];   // TODO: 从其他服务获取医生姓名
                    Diagnosis = OriginalPrescription.Diagnosis ?? string.Empty;
                    DosageCount = 1; // TODO: OriginalPrescription.DosageCount; - 等待DTO属性添加
                    Usage = string.Empty; // TODO: OriginalPrescription.Usage ?? string.Empty; - 等待DTO属性添加
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
                                Price = 0 // TODO: item.Price - 等待DTO属性添加
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

                // TODO: 实现实际的UpdatePrescriptionRequest
                var requestData = new
                {
                    Id = _prescriptionId,
                    Diagnosis = Diagnosis,
                    DosageCount = DosageCount,
                    Usage = Usage,
                    Remark = Remark,
                    Items = Items.Select(item => new
                    {
                        HerbId = item.HerbId,
                        Quantity = item.Quantity,
                        Price = item.Price
                    }).ToList()
                };

                // TODO: 实现实际的API调用
                // var response = await _prescriptionService.UpdateAsync(request);

                // 暂时模拟成功响应
                await Task.Delay(1000); // 模拟网络延迟
                _commonDialogService.ShowInformationAsync("处方保存成功（模拟）", "成功").GetAwaiter().GetResult();
                SaveSuccessCallback?.Invoke(requestData);
                CloseDialogCallback?.Invoke();
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

        private void ExecuteSelectHerb(PrescriptionItemEditModel item)
        {
            if (item == null) return;

            try
            {
                // TODO: 实现药材选择对话框
                _commonDialogService.ShowInformationAsync("药材选择功能开发中...", "提示").GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"选择药材失败: {ex.Message}", "错误").GetAwaiter().GetResult();
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
                    original.Quantity != current.Quantity)
                // TODO: || original.Price != current.Price - 等待DTO属性添加
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