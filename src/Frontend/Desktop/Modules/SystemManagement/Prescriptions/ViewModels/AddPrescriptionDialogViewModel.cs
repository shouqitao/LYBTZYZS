using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.WPF.Client.Core.Models.Prescriptions;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.WPF.Client.Modules.SystemManagement.Prescriptions.ViewModels
{
    /// <summary>
    /// 新增处方对话框视图模型
    /// </summary>
    public class AddPrescriptionDialogViewModel : BindableBase
    {
        private readonly IPrescriptionsApiService _prescriptionService;
        // private readonly IHerbsApiService _herbService; // TODO: 等待IHerbsApiService实现

        #region 属性

        private string _patientName = string.Empty;
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
        public bool CanSave => !string.IsNullOrWhiteSpace(PatientName) &&
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
        public DelegateCommand<PrescriptionItemEditModel> RemoveItemCommand { get; }
        public DelegateCommand<PrescriptionItemEditModel> SelectHerbCommand { get; }

        #endregion

        public Action? CloseDialogCallback { get; set; }
        public Action<object>? SaveSuccessCallback { get; set; } // TODO: 替换为实际的CreatePrescriptionRequest类型

        public AddPrescriptionDialogViewModel(IPrescriptionsApiService prescriptionService)
        {
            _prescriptionService = prescriptionService;
            // _herbService = herbService; // TODO: 等待IHerbsApiService实现

            Items = new ObservableCollection<PrescriptionItemEditModel>();
            Items.CollectionChanged += (s, e) =>
            {
                RaisePropertyChanged(nameof(TotalPrice));
                RaisePropertyChanged(nameof(CanSave));
                SaveCommand.RaiseCanExecuteChanged();
                SaveAndContinueCommand.RaiseCanExecuteChanged();
            };

            SaveCommand = new DelegateCommand(async () => await ExecuteSaveAsync(), () => CanSave);
            SaveAndContinueCommand = new DelegateCommand(async () => await ExecuteSaveAndContinueAsync(), () => CanSave);
            CancelCommand = new DelegateCommand(ExecuteCancel);
            AddItemCommand = new DelegateCommand(ExecuteAddItem);
            RemoveItemCommand = new DelegateCommand<PrescriptionItemEditModel>(ExecuteRemoveItem);
            SelectHerbCommand = new DelegateCommand<PrescriptionItemEditModel>(ExecuteSelectHerb);

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
            MessageBox.Show("处方保存成功，可以继续添加新处方", "成功", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task<bool> SavePrescriptionAsync()
        {
            if (!CanSave) return false;

            try
            {
                IsSaving = true;

                // TODO: 实现实际的CreatePrescriptionRequest
                var requestData = new
                {
                    PatientId = Guid.NewGuid(), // TODO: 实际应从患者选择获取
                    DoctorId = Guid.NewGuid(),  // TODO: 实际应从医生选择获取
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
                // var response = await _prescriptionService.CreateAsync(request);
                
                // 暂时模拟成功响应
                await Task.Delay(1000); // 模拟网络延迟
                SaveSuccessCallback?.Invoke(requestData);
                MessageBox.Show("处方保存成功（模拟）", "成功", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存处方失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void ExecuteCancel()
        {
            var result = MessageBox.Show("确定要取消新增处方吗？未保存的数据将丢失。", "确认取消", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
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

            var result = MessageBox.Show($"确定要移除药材 {item.HerbName} 吗？", "确认移除", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
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
                MessageBox.Show("药材选择功能开发中...", "提示", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"选择药材失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
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
            SaveCommand.RaiseCanExecuteChanged();
            SaveAndContinueCommand.RaiseCanExecuteChanged();
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