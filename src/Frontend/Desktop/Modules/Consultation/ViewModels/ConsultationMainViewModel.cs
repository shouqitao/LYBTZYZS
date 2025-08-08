using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Models.Prescriptions;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.WPF.Client.Core.Models.Consultation;
using LYBT.WPF.Client.Core.Models.Formulas;
using LYBT.WPF.Client.Modules.Consultation.Services;
using LYBT.WPF.Client.Modules.Consultation.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace LYBT.WPF.Client.Modules.Consultation.ViewModels
{
    /// <summary>
    /// 看诊主界面视图模型 - 重构后的精简协调器
    /// 负责协调各个服务完成看诊流程，不包含具体业务逻辑
    /// </summary>
    public class ConsultationMainViewModel : BindableBase
    {
        #region 依赖服务

        private readonly IConsultationDataService _dataService;
        private readonly IPrescriptionManager _prescriptionManager;
        private readonly IFormulaManager _formulaManager;
        private readonly IConsultationValidator _validator;
        private readonly IConsultationEventHandler _eventHandler;
        private readonly ILogger<ConsultationMainViewModel> _logger;

        #endregion

        #region 属性

        private string _title = "看诊工作台";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private ObservableCollection<PatientInfo> _patients = new();
        public ObservableCollection<PatientInfo> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }

        private PatientInfo? _selectedPatient;
        public PatientInfo? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (SetProperty(ref _selectedPatient, value))
                {
                    OnPatientSelected();
                }
            }
        }

        private ConsultationInfo? _currentConsultation;
        public ConsultationInfo? CurrentConsultation
        {
            get => _currentConsultation;
            set => SetProperty(ref _currentConsultation, value);
        }

        public ObservableCollection<PrescriptionItemInfo> PrescriptionItems => 
            _prescriptionManager.PrescriptionItems;

        public decimal TotalPrice => _prescriptionManager.TotalPrice;

        private ObservableCollection<HerbInfo> _availableHerbs = new();
        public ObservableCollection<HerbInfo> AvailableHerbs
        {
            get => _availableHerbs;
            set => SetProperty(ref _availableHerbs, value);
        }

        private ObservableCollection<FormulaInfo> _availableFormulas = new();
        public ObservableCollection<FormulaInfo> AvailableFormulas
        {
            get => _availableFormulas;
            set => SetProperty(ref _availableFormulas, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region 命令

        public ICommand RefreshCommand { get; }
        public ICommand NewConsultationCommand { get; }
        public ICommand SaveConsultationCommand { get; }
        public ICommand AddHerbCommand { get; }
        public ICommand RemoveHerbCommand { get; }
        public ICommand ApplyFormulaCommand { get; }

        #endregion

        #region 构造函数

        public ConsultationMainViewModel(
            IConsultationDataService dataService,
            IPrescriptionManager prescriptionManager,
            IFormulaManager formulaManager,
            IConsultationValidator validator,
            IConsultationEventHandler eventHandler,
            ILogger<ConsultationMainViewModel> logger)
        {
            _dataService = dataService;
            _prescriptionManager = prescriptionManager;
            _formulaManager = formulaManager;
            _validator = validator;
            _eventHandler = eventHandler;
            _logger = logger;

            RefreshCommand = new DelegateCommand(async () => await RefreshDataAsync());
            NewConsultationCommand = new DelegateCommand(async () => await StartNewConsultationAsync());
            SaveConsultationCommand = new DelegateCommand(async () => await SaveConsultationAsync());
            AddHerbCommand = new DelegateCommand<HerbInfo>(AddHerbToPrescription);
            RemoveHerbCommand = new DelegateCommand<PrescriptionItemInfo>(RemoveHerbFromPrescription);
            ApplyFormulaCommand = new DelegateCommand<FormulaInfo>(async f => await ApplyFormulaAsync(f));

            SubscribeToEvents();
            _ = InitializeAsync();
        }

        #endregion

        #region 初始化和数据加载

        private async Task InitializeAsync()
        {
            IsLoading = true;
            try
            {
                await RefreshDataAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshDataAsync()
        {
            var patientsTask = _dataService.LoadPatientsAsync();
            var herbsTask = _dataService.LoadHerbsAsync();
            var formulasTask = _dataService.LoadFormulasAsync();

            await Task.WhenAll(patientsTask, herbsTask, formulasTask);

            Patients = new ObservableCollection<PatientInfo>(await patientsTask);
            AvailableHerbs = new ObservableCollection<HerbInfo>(await herbsTask);
            AvailableFormulas = new ObservableCollection<FormulaInfo>(await formulasTask);

            _eventHandler.PublishStatusMessage("数据刷新完成", StatusMessageType.Success);
        }

        #endregion

        #region 看诊流程

        private void OnPatientSelected()
        {
            if (SelectedPatient != null)
            {
                _eventHandler.PublishPatientSelected(SelectedPatient);
            }
        }

        private async Task StartNewConsultationAsync()
        {
            if (SelectedPatient == null) return;

            var validation = _validator.ValidatePatientForConsultation(SelectedPatient);
            if (!validation.IsValid)
            {
                _eventHandler.PublishError("Consultation", validation.FirstError ?? "患者验证失败");
                return;
            }

            CurrentConsultation = await _dataService.CreateConsultationAsync(SelectedPatient.Id);
            if (CurrentConsultation != null)
            {
                _prescriptionManager.ClearPrescription();
                _eventHandler.PublishConsultationStarted(CurrentConsultation);
            }
        }

        private async Task SaveConsultationAsync()
        {
            if (CurrentConsultation == null) return;

            var validation = await _prescriptionManager.ValidatePrescriptionAsync();
            if (!validation)
            {
                _eventHandler.PublishError("Prescription", "处方验证失败");
                return;
            }

            var saved = await _prescriptionManager.SavePrescriptionAsync(
                CurrentConsultation.Id,
                CurrentConsultation.Diagnosis ?? "",
                "汤剂", 7, "每日一剂，水煎服");

            if (saved)
            {
                _eventHandler.PublishConsultationCompleted(CurrentConsultation);
                CurrentConsultation = null;
                SelectedPatient = null;
            }
        }

        #endregion

        #region 处方管理

        private void AddHerbToPrescription(HerbInfo? herb)
        {
            if (herb != null && _prescriptionManager.AddHerbToPrescription(herb))
            {
                RaisePropertyChanged(nameof(TotalPrice));
            }
        }

        private void RemoveHerbFromPrescription(PrescriptionItemInfo? item)
        {
            if (item != null && _prescriptionManager.RemoveHerbFromPrescription(item.HerbId))
            {
                RaisePropertyChanged(nameof(TotalPrice));
            }
        }

        private async Task ApplyFormulaAsync(FormulaInfo? formula)
        {
            if (formula == null) return;

            var items = _formulaManager.ApplyFormulaTemplate(formula);
            _prescriptionManager.ImportPrescriptionItems(items);
            RaisePropertyChanged(nameof(TotalPrice));

            await Task.CompletedTask;
        }

        #endregion

        #region 事件订阅

        private void SubscribeToEvents()
        {
            _eventHandler.SubscribeToDataRefreshRequest(async type => 
            {
                if (type == DataRefreshType.All)
                    await RefreshDataAsync();
            });
        }

        #endregion
    }
}