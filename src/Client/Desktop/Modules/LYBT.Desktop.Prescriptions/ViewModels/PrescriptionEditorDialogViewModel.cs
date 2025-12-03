using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Modules.Prescriptions.ViewModels.Components;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels
{
    /// <summary>处方编辑对话框视图模型</summary>
    public class PrescriptionEditorDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        private readonly PrescriptionDataManager _dataManager;

        private Guid _prescriptionId;
        private PrescriptionDto? _originalPrescription;
        private string _prescriptionNo = string.Empty;
        private int _dosageCount = 7;
        private string _usage = "水煎服，一日三次，饭后服用";
        private string _medicalAdvice = string.Empty;
        private string _remark = string.Empty;
        private decimal _discount = 1.0m;
        private decimal _totalAmount;
        private bool _hasChanges;
        private bool _isValidationEnabled = true;
        private bool _isReadOnly;
        private string _readOnlyReason = string.Empty;

        public Guid PrescriptionId { get => _prescriptionId; set => SetProperty(ref _prescriptionId, value); }
        public PrescriptionDto? OriginalPrescription { get => _originalPrescription; set => SetProperty(ref _originalPrescription, value); }

        [Required(ErrorMessage = "处方编号不能为空")]
        [StringLength(50, ErrorMessage = "处方编号长度不能超过50个字符")]
        public string PrescriptionNo
        {
            get => _prescriptionNo;
            set { if (SetProperty(ref _prescriptionNo, value)) { ValidateProperty(); MarkAsChanged(); } }
        }

        [Required(ErrorMessage = "剂数不能为空")]
        [Range(1, 100, ErrorMessage = "剂数必须在1-100之间")]
        public int DosageCount
        {
            get => _dosageCount;
            set { if (SetProperty(ref _dosageCount, value)) { ValidateProperty(); MarkAsChanged(); } }
        }

        [Required(ErrorMessage = "用法不能为空")]
        [StringLength(200, ErrorMessage = "用法长度不能超过200个字符")]
        public string Usage
        {
            get => _usage;
            set { if (SetProperty(ref _usage, value)) { ValidateProperty(); MarkAsChanged(); } }
        }

        [StringLength(500, ErrorMessage = "医嘱长度不能超过500个字符")]
        public string MedicalAdvice
        {
            get => _medicalAdvice;
            set { if (SetProperty(ref _medicalAdvice, value)) { ValidateProperty(); MarkAsChanged(); } }
        }

        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string Remark
        {
            get => _remark;
            set { if (SetProperty(ref _remark, value)) { ValidateProperty(); MarkAsChanged(); } }
        }

        [Required(ErrorMessage = "折扣不能为空")]
        [Range(0.1, 1.0, ErrorMessage = "折扣必须在0.1-1.0之间")]
        public decimal Discount
        {
            get => _discount;
            set { if (SetProperty(ref _discount, value)) { ValidateProperty(); MarkAsChanged(); } }
        }

        public decimal TotalAmount { get => _totalAmount; set => SetProperty(ref _totalAmount, value); }
        public bool HasChanges { get => _hasChanges; set => SetProperty(ref _hasChanges, value); }
        public bool IsValidationEnabled { get => _isValidationEnabled; set => SetProperty(ref _isValidationEnabled, value); }

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set { if (SetProperty(ref _isReadOnly, value)) { UpdateCommandStates(); RaisePropertyChanged(nameof(CanEdit)); } }
        }

        public string ReadOnlyReason { get => _readOnlyReason; set => SetProperty(ref _readOnlyReason, value); }
        public bool CanEdit => !IsReadOnly && !IsBusy;
        public string ChangeInfo => IsReadOnly ? ReadOnlyReason : (HasChanges ? "有未保存的更改" : "无更改");
        public string Title { get; set; } = "编辑处方信息";
        public event Action<IDialogResult>? RequestClose;

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand ResetCommand { get; }
        public DelegateCommand ValidateCommand { get; }
        public DelegateCommand AddHerbCommand { get; }
        public DelegateCommand EditHerbCommand { get; }
        public DelegateCommand RemoveHerbCommand { get; }
        public DelegateCommand LoadFormulaTemplateCommand { get; }
        public DelegateCommand PreviewCommand { get; }

        public PrescriptionEditorDialogViewModel(
            PrescriptionDataManager dataManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));

            SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => !IsReadOnly && HasChanges && !IsBusy && !HasErrors);
            CancelCommand = new DelegateCommand(async () => await CancelAsync());
            ResetCommand = new DelegateCommand(async () => await ResetAsync(), () => !IsReadOnly && HasChanges && OriginalPrescription != null);
            ValidateCommand = new DelegateCommand(() => ValidateAll());
            AddHerbCommand = new DelegateCommand(() => Logger.LogInformation("PrescriptionEditorDialog - 添加药材命令（骨架实现）"));
            EditHerbCommand = new DelegateCommand(() => Logger.LogInformation("PrescriptionEditorDialog - 编辑药材命令（骨架实现）"));
            RemoveHerbCommand = new DelegateCommand(() => Logger.LogInformation("PrescriptionEditorDialog - 移除药材命令（骨架实现）"));
            LoadFormulaTemplateCommand = new DelegateCommand(() => Logger.LogInformation("PrescriptionEditorDialog - 加载验方模板命令（骨架实现）"));
            PreviewCommand = new DelegateCommand(() => Logger.LogInformation("PrescriptionEditorDialog - 预览命令（骨架实现）"));

            PropertyChanged += (s, e) => { UpdateCommandStates(); if (e.PropertyName == nameof(HasChanges)) SaveCommand.RaiseCanExecuteChanged(); };
        }

        public bool CanCloseDialog() => !HasChanges || ShowConfirmMessage("有未保存的更改，确定要关闭吗？");
        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            try
            {
                if (parameters.ContainsKey("Title")) Title = parameters.GetValue<string>("Title");
                if (parameters.ContainsKey("PrescriptionId")) PrescriptionId = parameters.GetValue<Guid>("PrescriptionId");

                if (parameters.ContainsKey("Prescription"))
                {
                    OriginalPrescription = parameters.GetValue<PrescriptionDto>("Prescription");
                    LoadFromPrescription(OriginalPrescription);
                }
                else if (PrescriptionId != Guid.Empty)
                    Task.Run(async () => await LoadPrescriptionAsync());

                HasChanges = false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开处方编辑对话框时发生异常");
                _ = ShowErrorMessageAsync("初始化失败，请稍后重试");
            }
        }

        private async Task LoadPrescriptionAsync()
        {
            try
            {
                SetIsBusy(true, "正在加载处方信息...");
                var response = await _dataManager.GetPrescriptionByIdAsync(PrescriptionId);
                if (response.Data == null) { await ShowErrorMessageAsync("处方不存在"); return; }
                OriginalPrescription = response.Data;
                LoadFromPrescription(OriginalPrescription);
                Logger.LogInformation("处方信息加载完成");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载处方信息时发生异常");
                await ShowErrorMessageAsync("加载处方信息时发生系统错误");
            }
            finally { SetIsBusy(false); }
        }

        private void LoadFromPrescription(PrescriptionDto prescription)
        {
            if (prescription == null) return;
            try
            {
                IsValidationEnabled = false;

                if (prescription.CreatedAt.Date != DateTime.Today)
                {
                    IsReadOnly = true;
                    ReadOnlyReason = $"只读模式：该处方创建于 {prescription.CreatedAt:yyyy-MM-dd}，已超过可修改期限（仅限创建当天可修改）";
                    Logger.LogInformation("处方 {PrescriptionId} 进入只读模式", prescription.Id);
                }
                else { IsReadOnly = false; ReadOnlyReason = string.Empty; }

                PrescriptionNo = $"CF{DateTime.Now:yyyyMMddHHmmss}";
                DosageCount = prescription.DosageCount;
                Usage = prescription.Usage ?? "水煎服，一日三次，饭后服用";
                MedicalAdvice = prescription.Advice ?? string.Empty;
                Remark = prescription.Remark ?? string.Empty;
                Discount = prescription.Discount;
                TotalAmount = prescription.TotalPrice;
                HasChanges = false;
            }
            finally { IsValidationEnabled = true; }
        }

        private async Task SaveAsync()
        {
            try
            {
                if (IsReadOnly) { await ShowWarningMessageAsync(ReadOnlyReason); return; }
                if (!ValidateAll()) { await ShowWarningMessageAsync("请修正输入错误后再保存"); return; }

                SetIsBusy(true, "正在保存处方信息...");

                var updateDto = new PrescriptionUpdateDto
                {
                    DosageCount = DosageCount, Usage = Usage, Advice = MedicalAdvice, Remark = Remark, Discount = Discount
                };

                if (OriginalPrescription == null) { await ShowErrorMessageAsync("无法保存：缺少医案信息"); return; }

                var updatedPrescription = await _dataManager.UpdatePrescriptionAsync(OriginalPrescription.MedicalCaseId, updateDto);
                if (updatedPrescription == null) { await ShowErrorMessageAsync("保存处方失败"); return; }

                await ShowSuccessMessageAsync("处方信息保存成功");
                RequestClose?.Invoke(new DialogResult(ButtonResult.OK, new DialogParameters { { "UpdatedPrescription", updatedPrescription }, { "HasChanges", true } }));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存处方信息时发生异常");
                await ShowErrorMessageAsync("保存处方信息时发生系统错误");
            }
            finally { SetIsBusy(false); }
        }

        private async Task CancelAsync()
        {
            if (HasChanges && !await ShowConfirmationAsync("有未保存的更改，确定要取消吗？")) return;
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        private async Task ResetAsync()
        {
            if (OriginalPrescription != null && await ShowConfirmationAsync("确定要重置所有更改吗？"))
            {
                LoadFromPrescription(OriginalPrescription);
                ClearAllErrors();
            }
        }

        private bool ValidateAll()
        {
            if (!IsValidationEnabled) return true;
            ClearAllErrors();
            var isValid = true;

            if (string.IsNullOrWhiteSpace(PrescriptionNo)) { AddError(nameof(PrescriptionNo), "处方编号不能为空"); isValid = false; }
            if (DosageCount < 1 || DosageCount > 100) { AddError(nameof(DosageCount), "剂数必须在1-100之间"); isValid = false; }
            if (string.IsNullOrWhiteSpace(Usage)) { AddError(nameof(Usage), "用法不能为空"); isValid = false; }
            if (Discount < 0.1m || Discount > 1.0m) { AddError(nameof(Discount), "折扣必须在0.1-1.0之间"); isValid = false; }

            return isValid;
        }

        private void UpdateCommandStates() { SaveCommand.RaiseCanExecuteChanged(); ResetCommand.RaiseCanExecuteChanged(); }
        private void MarkAsChanged() { if (IsValidationEnabled) HasChanges = true; }
    }
}
