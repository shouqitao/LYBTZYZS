using LYBT.Shared.Models.Enums;
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

namespace LYBT.WPF.Client.Modules.Consultation.ViewModels
{
    /// <summary>
    /// 看诊主界面视图模型 - 简化版（不依赖外部服务）
    /// </summary>
    public class ConsultationMainViewModel : BindableBase
    {
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

        private ObservableCollection<PrescriptionItemInfo> _prescriptionItems = new();
        public ObservableCollection<PrescriptionItemInfo> PrescriptionItems
        {
            get => _prescriptionItems;
            set => SetProperty(ref _prescriptionItems, value);
        }

        private ObservableCollection<HerbInfo> _availableHerbs = new();
        public ObservableCollection<HerbInfo> AvailableHerbs
        {
            get => _availableHerbs;
            set => SetProperty(ref _availableHerbs, value);
        }

        private string _searchKeyword = string.Empty;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    SearchPatients();
                }
            }
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
        public ICommand PrintPrescriptionCommand { get; }
        public ICommand RemovePrescriptionItemCommand { get; }
        public ICommand AddHerbCommand { get; }

        #endregion

        public ConsultationMainViewModel()
        {
            // 初始化集合

            // 初始化命令
            RefreshCommand = new DelegateCommand(async () => await LoadPatientsAsync());
            NewConsultationCommand = new DelegateCommand(StartNewConsultation, () => SelectedPatient != null);
            SaveConsultationCommand = new DelegateCommand(async () => await SaveConsultationAsync(), () => CurrentConsultation != null);
            PrintPrescriptionCommand = new DelegateCommand(PrintPrescription, () => PrescriptionItems?.Any() == true);
            RemovePrescriptionItemCommand = new DelegateCommand<PrescriptionItemInfo>(RemovePrescriptionItem);
            AddHerbCommand = new DelegateCommand<HerbInfo>(AddHerbToPrescription);

            // 加载初始数据
            _ = LoadInitialDataAsync();
        }

        #region 方法

        private async Task LoadInitialDataAsync()
        {
            await LoadPatientsAsync();
            await LoadAvailableHerbsAsync();
        }

        private async Task LoadPatientsAsync()
        {
            try
            {
                IsLoading = true;
                await Task.Delay(500); // 模拟加载

                // 创建示例数据
                var samplePatients = new[]
                {
                    new PatientInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "张三",
                        Gender = (Gender)1,
                        Age = 35,
                        PhoneNumber = "13800138001"
                    },
                    new PatientInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "李四",
                        Gender = (Gender)0,
                        Age = 28,
                        PhoneNumber = "13800138002"
                    },
                    new PatientInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "王五",
                        Gender = (Gender)1,
                        Age = 42,
                        PhoneNumber = "13800138003"
                    }
                };

                Patients.Clear();
                foreach (var patient in samplePatients)
                {
                    Patients.Add(patient);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadAvailableHerbsAsync()
        {
            try
            {
                IsLoading = true;
                await Task.Delay(300); // 模拟加载

                // 创建示例中药材数据
                var sampleHerbs = new[]
                {
                    new HerbInfo { Id = Guid.NewGuid(), Name = "人参", Unit = "g", Price = 280.00m },
                    new HerbInfo { Id = Guid.NewGuid(), Name = "黄芪", Unit = "g", Price = 45.00m },
                    new HerbInfo { Id = Guid.NewGuid(), Name = "当归", Unit = "g", Price = 38.00m },
                    new HerbInfo { Id = Guid.NewGuid(), Name = "白术", Unit = "g", Price = 28.00m },
                    new HerbInfo { Id = Guid.NewGuid(), Name = "茯苓", Unit = "g", Price = 25.00m },
                    new HerbInfo { Id = Guid.NewGuid(), Name = "甘草", Unit = "g", Price = 18.00m }
                };

                AvailableHerbs.Clear();
                foreach (var herb in sampleHerbs)
                {
                    AvailableHerbs.Add(herb);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SearchPatients()
        {
            // TODO: 实现患者搜索逻辑
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                _ = LoadPatientsAsync();
            }
        }

        private void OnPatientSelected()
        {
            if (SelectedPatient == null)
            {
                CurrentConsultation = null;
                return;
            }

            // 创建新的看诊记录
            CurrentConsultation = new ConsultationInfo
            {
                Id = Guid.NewGuid(),
                PatientId = SelectedPatient.Id,
                PatientName = SelectedPatient.Name,
                PatientAge = SelectedPatient.Age,
                PatientGender = SelectedPatient.Gender == Gender.Male ? "男" : "女",
                ConsultationTime = DateTime.Now,
                DoctorName = "当前医生"
            };
        }

        private void StartNewConsultation()
        {
            if (SelectedPatient == null) return;

            CurrentConsultation = new ConsultationInfo
            {
                Id = Guid.NewGuid(),
                PatientId = SelectedPatient.Id,
                PatientName = SelectedPatient.Name,
                PatientAge = SelectedPatient.Age,
                PatientGender = SelectedPatient.Gender == Gender.Male ? "男" : "女",
                ConsultationTime = DateTime.Now,
                DoctorName = "当前医生"
            };

            PrescriptionItems.Clear();
        }

        private async Task SaveConsultationAsync()
        {
            if (CurrentConsultation == null) return;

            try
            {
                IsLoading = true;
                await Task.Delay(500); // 模拟保存

                // TODO: 实际保存逻辑
                Console.WriteLine($"保存看诊记录: {CurrentConsultation.Id}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void PrintPrescription()
        {
            if (!PrescriptionItems.Any()) return;

            // TODO: 实现打印逻辑
            Console.WriteLine("打印处方...");
        }

        private void AddHerbToPrescription(HerbInfo herb)
        {
            if (herb == null) return;

            var existingItem = PrescriptionItems.FirstOrDefault(p => p.HerbId == herb.Id);
            if (existingItem != null)
            {
                existingItem.Quantity += 10; // 默认增加10单位
            }
            else
            {
                PrescriptionItems.Add(new PrescriptionItemInfo
                {
                    Id = Guid.NewGuid(),
                    HerbId = herb.Id,
                    HerbName = herb.Name,
                    Quantity = 10, // 默认数量
                    Unit = herb.Unit
                });
            }
        }

        private void RemovePrescriptionItem(PrescriptionItemInfo item)
        {
            if (item == null) return;
            PrescriptionItems.Remove(item);
        }

        #endregion
    }

    /// <summary>
    /// 处方项信息
    /// </summary>
    public class PrescriptionItemInfo : BindableBase
    {
        private Guid _id;
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private Guid _herbId;
        public Guid HerbId
        {
            get => _herbId;
            set => SetProperty(ref _herbId, value);
        }

        private string _herbName = string.Empty;
        public string HerbName
        {
            get => _herbName;
            set => SetProperty(ref _herbName, value);
        }

        private decimal _quantity;
        public decimal Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        private string _unit = "g";
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }
    }
}