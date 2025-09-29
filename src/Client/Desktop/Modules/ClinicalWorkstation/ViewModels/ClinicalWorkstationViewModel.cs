using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System.Windows.Input;
using System;
using System.Collections.ObjectModel;
using Prism.Mvvm;

namespace LYBT.Desktop.ClinicalWorkstation.ViewModels
{
    /// <summary>
    /// 诊疗工作台视图模型
    /// </summary>
    public class ClinicalWorkstationViewModel : ModernViewModelBase
    {
        private readonly IRegionManager _regionManager;

        private string _currentUserName = string.Empty;
        private string _currentPatientName = "未选择";
        private int _selectedTabIndex = 0;

        // 诊断数据
        private DiagnosisData _diagnosis = new();
        private ObservableCollection<DiagnosisHistoryItem> _diagnosisHistory = new();

        // 处方数据
        private ObservableCollection<PrescriptionGridItem> _prescriptionGrid = new();
        private ObservableCollection<FormulaTemplate> _formulaTemplates = new();
        private FormulaTemplate? _selectedFormula;
        private string _cookingInstructions = "水煎服，一日一剂，分两次温服";
        private int _prescriptionCount = 7;
        private decimal _unitPrice = 0;
        private decimal _totalPrice = 0;

        public ClinicalWorkstationViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IErrorHandlingService errorHandlingService)
            : base(eventAggregator, loggerFactory, errorHandlingService)
        {
            _regionManager = regionManager;

            // 初始化导航服务
            NavigationService = new Services.ClinicalNavigator(regionManager);

            // 初始化命令
            SelectPatientCommand = new DelegateCommand(ExecuteSelectPatient);
            LogoutCommand = new DelegateCommand(ExecuteLogout);
            ImportDiagnosisCommand = new DelegateCommand<DiagnosisHistoryItem>(ExecuteImportDiagnosis);

            SearchHerbCommand = new DelegateCommand(ExecuteSearchHerb);
            ImportFormulaCommand = new DelegateCommand(ExecuteImportFormula);
            ShowHistoryCommand = new DelegateCommand(ExecuteShowHistory);
            ClearPrescriptionCommand = new DelegateCommand(ExecuteClearPrescription);
            SavePrescriptionCommand = new DelegateCommand(ExecuteSavePrescription);
            PrintPrescriptionCommand = new DelegateCommand(ExecutePrintPrescription);

            // 订阅登录成功事件
            EventAggregator.GetEvent<UserLoggedInEvent>().Subscribe(OnUserLoggedIn);

            // 初始化处方网格（4×6）
            InitializePrescriptionGrid();

            // 初始化测试数据
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

        // 新增导航Tab索引
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

        // 诊断属性
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

        // 处方属性
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
            // 初始化4×6网格（24个格子）
            for (int i = 0; i < 24; i++)
            {
                PrescriptionGrid.Add(new PrescriptionGridItem());
            }
        }

        private void InitializeTestData()
        {
            // 添加测试历史诊断数据
            DiagnosisHistory.Add(new DiagnosisHistoryItem
            {
                Date = DateTime.Now.AddDays(-7),
                ChiefComplaint = "失眠多梦，心悸",
                Diagnosis = "心脾两虚证",
                DoctorName = "张医生"
            });

            DiagnosisHistory.Add(new DiagnosisHistoryItem
            {
                Date = DateTime.Now.AddDays(-14),
                ChiefComplaint = "头晕目眩，乏力",
                Diagnosis = "气血不足证",
                DoctorName = "李医生"
            });

            // 添加测试验方模板
            FormulaTemplates.Add(new FormulaTemplate { Name = "四物汤", Id = 1 });
            FormulaTemplates.Add(new FormulaTemplate { Name = "六君子汤", Id = 2 });
            FormulaTemplates.Add(new FormulaTemplate { Name = "逍遥散", Id = 3 });
        }

        private void ExecuteSelectPatient()
        {
            try
            {
                Logger.LogInformation("选择患者");
                // TODO: 弹出患者选择对话框
                CurrentPatientName = "张三（测试）";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "选择患者失败");
                ShowErrorMessage($"选择患者失败：{ex.Message}");
            }
        }

        private void ExecuteLogout()
        {
            try
            {
                Logger.LogInformation("医生请求退出登录");

                // 发布登出事件
                EventAggregator.GetEvent<UserLoggedOutEvent>().Publish();

                // 导航回登录界面
                _regionManager.RequestNavigate("ContentRegion", "LoginView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "退出登录失败");
                ShowErrorMessage($"退出登录失败：{ex.Message}");
            }
        }

        private void ExecuteImportDiagnosis(DiagnosisHistoryItem item)
        {
            if (item == null) return;

            Logger.LogInformation($"导入历史诊断：{item.Date:yyyy-MM-dd} - {item.ChiefComplaint}");

            // 导入诊断数据到当前诊断
            Diagnosis.ChiefComplaint = item.ChiefComplaint;
            Diagnosis.DiagnosisResult = item.Diagnosis;
        }

        private void ExecuteSearchHerb()
        {
            Logger.LogInformation("打开药材搜索");
            // TODO: 弹出药材搜索对话框
        }

        private void ExecuteImportFormula()
        {
            if (SelectedFormula == null)
            {
                SetStatus("请先选择一个验方");
                return;
            }

            Logger.LogInformation($"导入验方：{SelectedFormula.Name}");
            // TODO: 导入验方到处方网格
        }

        private void ExecuteShowHistory()
        {
            Logger.LogInformation("显示历史处方");
            // TODO: 弹出历史处方对话框
        }

        private void ExecuteClearPrescription()
        {
            Logger.LogInformation("清空处方");

            // 清空所有处方格子
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
            Logger.LogInformation("保存处方");
            // TODO: 保存处方到数据库
            SetStatus("处方已保存");
        }

        private void ExecutePrintPrescription()
        {
            Logger.LogInformation("打印处方");
            // TODO: 调用打印功能
            SetStatus("正在打印处方...");
        }

        private void CalculateTotalPrice()
        {
            // 计算总价
            TotalPrice = UnitPrice * PrescriptionCount;
        }

        private void OnUserLoggedIn(UserLoggedInEventArgs args)
        {
            CurrentUserName = args.Username;
            Logger.LogInformation($"医生 {args.Username} 已登录诊疗工作台");
        }

        #endregion
    }

    #region Data Models

    /// <summary>
    /// 诊断数据模型
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
    /// 历史诊断项
    /// </summary>
    public class DiagnosisHistoryItem
    {
        public DateTime Date { get; set; }
        public string ChiefComplaint { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 处方网格项
    /// </summary>
    public class PrescriptionGridItem : BindableBase
    {
        private string _herbName = string.Empty;
        private decimal _dosage;

        public string HerbName { get => _herbName; set => SetProperty(ref _herbName, value); }
        public decimal Dosage { get => _dosage; set => SetProperty(ref _dosage, value); }
    }

    /// <summary>
    /// 验方模板
    /// </summary>
    public class FormulaTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}