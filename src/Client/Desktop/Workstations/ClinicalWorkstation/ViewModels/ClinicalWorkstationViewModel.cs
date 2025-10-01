using System.Collections.ObjectModel;
using System.Windows.Input;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;

namespace LYBT.Desktop.ClinicalWorkstation.ViewModels
{
    /// <summary>
    /// ���ƹ���̨��ͼģ��
    /// </summary>
    public class ClinicalWorkstationViewModel : UnifiedViewModelBase
    {
        private readonly IRegionManager _regionManager;

        private string _currentUserName = string.Empty;
        private string _currentPatientName = "δѡ��";
        private int _selectedTabIndex = 0;

        // �������
        private DiagnosisData _diagnosis = new();
        private ObservableCollection<DiagnosisHistoryItem> _diagnosisHistory = new();

        // ��������
        private ObservableCollection<PrescriptionGridItem> _prescriptionGrid = new();
        private ObservableCollection<FormulaTemplate> _formulaTemplates = new();
        private FormulaTemplate? _selectedFormula;
        private string _cookingInstructions = "ˮ�����һ��һ�����������·�";
        private int _prescriptionCount = 7;
        private decimal _unitPrice = 0;
        private decimal _totalPrice = 0;

        public ClinicalWorkstationViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, null, userNotificationService)
        {
            _regionManager = regionManager;

            // ��ʼ����������
            NavigationService = new Services.ClinicalNavigator(regionManager);

            // ��ʼ������
            SelectPatientCommand = new DelegateCommand(ExecuteSelectPatient);
            LogoutCommand = new DelegateCommand(ExecuteLogout);
            ImportDiagnosisCommand = new DelegateCommand<DiagnosisHistoryItem>(ExecuteImportDiagnosis);

            SearchHerbCommand = new DelegateCommand(ExecuteSearchHerb);
            ImportFormulaCommand = new DelegateCommand(ExecuteImportFormula);
            ShowHistoryCommand = new DelegateCommand(ExecuteShowHistory);
            ClearPrescriptionCommand = new DelegateCommand(ExecuteClearPrescription);
            SavePrescriptionCommand = new DelegateCommand(ExecuteSavePrescription);
            PrintPrescriptionCommand = new DelegateCommand(ExecutePrintPrescription);

            // ���ĵ�¼�ɹ��¼�
            EventAggregator.GetEvent<UserLoggedInEvent>().Subscribe(OnUserLoggedIn);

            // ��ʼ����������4��6��
            InitializePrescriptionGrid();

            // ��ʼ����������
            InitializeTestData();
        }

        #region Services

        public Navigation.IClinicalNavigator NavigationService { get; private set; }

        #endregion

        #region Properties

        public string CurrentUserName
        {
            get => _currentUserName;
            set => SetProperty(ref _currentUserName, value);
        }

        public string CurrentPatientName
        {
            get => _currentPatientName;
            set => SetProperty(ref _currentPatientName, value);
        }

        // ��������Tab����
        private int _mainTabIndex = 0;

        public int MainTabIndex
        {
            get => _mainTabIndex;
            set => SetProperty(ref _mainTabIndex, value);
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        // �������
        public DiagnosisData Diagnosis
        {
            get => _diagnosis;
            set => SetProperty(ref _diagnosis, value);
        }

        public ObservableCollection<DiagnosisHistoryItem> DiagnosisHistory
        {
            get => _diagnosisHistory;
            set => SetProperty(ref _diagnosisHistory, value);
        }

        // ��������
        public ObservableCollection<PrescriptionGridItem> PrescriptionGrid
        {
            get => _prescriptionGrid;
            set => SetProperty(ref _prescriptionGrid, value);
        }

        public ObservableCollection<FormulaTemplate> FormulaTemplates
        {
            get => _formulaTemplates;
            set => SetProperty(ref _formulaTemplates, value);
        }

        public FormulaTemplate? SelectedFormula
        {
            get => _selectedFormula;
            set => SetProperty(ref _selectedFormula, value);
        }

        public string CookingInstructions
        {
            get => _cookingInstructions;
            set => SetProperty(ref _cookingInstructions, value);
        }

        public int PrescriptionCount
        {
            get => _prescriptionCount;
            set
            {
                SetProperty(ref _prescriptionCount, value);
                CalculateTotalPrice();
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set => SetProperty(ref _unitPrice, value);
        }

        public decimal TotalPrice
        {
            get => _totalPrice;
            set => SetProperty(ref _totalPrice, value);
        }

        #endregion

        #region Commands

        public ICommand SelectPatientCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand ImportDiagnosisCommand { get; }
        public ICommand SearchHerbCommand { get; }
        public ICommand ImportFormulaCommand { get; }
        public ICommand ShowHistoryCommand { get; }
        public ICommand ClearPrescriptionCommand { get; }
        public ICommand SavePrescriptionCommand { get; }
        public ICommand PrintPrescriptionCommand { get; }

        #endregion

        #region Methods

        private void InitializePrescriptionGrid()
        {
            // ��ʼ��4��6����24�����ӣ�
            for (int i = 0; i < 24; i++)
            {
                PrescriptionGrid.Add(new PrescriptionGridItem());
            }
        }

        private void InitializeTestData()
        {
            // ���Ӳ�����ʷ�������
            DiagnosisHistory.Add(new DiagnosisHistoryItem
            {
                Date = DateTime.Now.AddDays(-7),
                ChiefComplaint = "ʧ�߶��Σ��ļ�",
                Diagnosis = "��Ƣ����֤",
                DoctorName = "��ҽ��"
            });

            DiagnosisHistory.Add(new DiagnosisHistoryItem
            {
                Date = DateTime.Now.AddDays(-14),
                ChiefComplaint = "ͷ��Ŀѣ������",
                Diagnosis = "��Ѫ����֤",
                DoctorName = "��ҽ��"
            });

            // ���Ӳ����鷽ģ��
            FormulaTemplates.Add(new FormulaTemplate { Name = "������", Id = 1 });
            FormulaTemplates.Add(new FormulaTemplate { Name = "��������", Id = 2 });
            FormulaTemplates.Add(new FormulaTemplate { Name = "��ңɢ", Id = 3 });
        }

        private void ExecuteSelectPatient()
        {
            try
            {
                Logger.LogInformation("ѡ����");
                // TODO: ��������ѡ��Ի���
                CurrentPatientName = "���������ԣ�";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ѡ����ʧ��");
                ShowErrorMessage($"ѡ����ʧ�ܣ�{ex.Message}");
            }
        }

        private void ExecuteLogout()
        {
            try
            {
                Logger.LogInformation("ҽ�������˳���¼");

                // �����ǳ��¼�
                EventAggregator.GetEvent<UserLoggedOutEvent>().Publish();

                // �����ص�¼����
                _regionManager.RequestNavigate("ContentRegion", "LoginView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "�˳���¼ʧ��");
                ShowErrorMessage($"�˳���¼ʧ�ܣ�{ex.Message}");
            }
        }

        private void ExecuteImportDiagnosis(DiagnosisHistoryItem item)
        {
            if (item == null) return;

            Logger.LogInformation($"������ʷ��ϣ�{item.Date:yyyy-MM-dd} - {item.ChiefComplaint}");

            // ����������ݵ���ǰ���
            Diagnosis.ChiefComplaint = item.ChiefComplaint;
            Diagnosis.DiagnosisResult = item.Diagnosis;
        }

        private void ExecuteSearchHerb()
        {
            Logger.LogInformation("��ҩ������");
            // TODO: ����ҩ�������Ի���
        }

        private void ExecuteImportFormula()
        {
            if (SelectedFormula == null)
            {
                SetStatus("����ѡ��һ���鷽");
                return;
            }

            Logger.LogInformation($"�����鷽��{SelectedFormula.Name}");
            // TODO: �����鷽����������
        }

        private void ExecuteShowHistory()
        {
            Logger.LogInformation("��ʾ��ʷ����");
            // TODO: ������ʷ�����Ի���
        }

        private void ExecuteClearPrescription()
        {
            Logger.LogInformation("��մ���");

            // ������д�������
            foreach (var item in PrescriptionGrid)
            {
                item.HerbName = string.Empty;
                item.Dosage = 0;
            }

            UnitPrice = 0;
            TotalPrice = 0;
        }

        private void ExecuteSavePrescription()
        {
            Logger.LogInformation("���洦��");
            // TODO: ���洦�������ݿ�
            SetStatus("�����ѱ���");
        }

        private void ExecutePrintPrescription()
        {
            Logger.LogInformation("��ӡ����");
            // TODO: ���ô�ӡ����
            SetStatus("���ڴ�ӡ����...");
        }

        private void CalculateTotalPrice()
        {
            // �����ܼ�
            TotalPrice = UnitPrice * PrescriptionCount;
        }

        private void OnUserLoggedIn(UserLoggedInEventArgs args)
        {
            CurrentUserName = args.Username;
            Logger.LogInformation($"ҽ�� {args.Username} �ѵ�¼���ƹ���̨");
        }

        #endregion
    }

    #region Data Models

    /// <summary>
    /// �������ģ��
    /// </summary>
    public class DiagnosisData : BindableBase
    {
        private string _wangZhen = string.Empty;
        private string _wenZhen = string.Empty;
        private string _wenZhen2 = string.Empty;
        private string _qieZhen = string.Empty;
        private string _chiefComplaint = string.Empty;
        private string _presentIllness = string.Empty;
        private string _diagnosisResult = string.Empty;
        private string _remarks = string.Empty;

        public string WangZhen { get => _wangZhen; set => SetProperty(ref _wangZhen, value); }
        public string WenZhen { get => _wenZhen; set => SetProperty(ref _wenZhen, value); }
        public string WenZhen2 { get => _wenZhen2; set => SetProperty(ref _wenZhen2, value); }
        public string QieZhen { get => _qieZhen; set => SetProperty(ref _qieZhen, value); }
        public string ChiefComplaint { get => _chiefComplaint; set => SetProperty(ref _chiefComplaint, value); }
        public string PresentIllness { get => _presentIllness; set => SetProperty(ref _presentIllness, value); }
        public string DiagnosisResult { get => _diagnosisResult; set => SetProperty(ref _diagnosisResult, value); }
        public string Remarks { get => _remarks; set => SetProperty(ref _remarks, value); }
    }

    /// <summary>
    /// ��ʷ�����
    /// </summary>
    public class DiagnosisHistoryItem
    {
        public DateTime Date { get; set; }
        public string ChiefComplaint { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
    }

    /// <summary>
    /// ����������
    /// </summary>
    public class PrescriptionGridItem : BindableBase
    {
        private string _herbName = string.Empty;
        private decimal _dosage;

        public string HerbName { get => _herbName; set => SetProperty(ref _herbName, value); }
        public decimal Dosage { get => _dosage; set => SetProperty(ref _dosage, value); }
    }

    /// <summary>
    /// �鷽ģ��
    /// </summary>
    public class FormulaTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
