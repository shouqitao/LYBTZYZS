using System;
using Prism.Mvvm;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Doctors;
using LYBT.Shared.Models.Contracts.FormulaTemplates;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Contracts.Billing;
using LYBT.Shared.Models.Contracts.DiagnosisTreatment;
using LYBT.Shared.Models.Contracts.Queueing;
using LYBT.Shared.Models.Contracts.Records;
using LYBT.Shared.Models.Contracts.Pharmacy;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Modules.Common.ViewModels
{
    /// <summary>
    /// 控件展示页面视图模型
    /// </summary>
    public class ControlShowcaseViewModel : BindableBase
    {
        private HerbDto _sampleHerb;
        private PatientDto _samplePatient;
        private DoctorDto _sampleDoctor;
        private FormulaTemplateDto _sampleFormulaTemplate;
        private UserDto _sampleUser;
        private PrescriptionDto _samplePrescription;
        private RegistrationDto _sampleRegistration;
        private BillingDto _sampleBilling;
        private DiagnosisTreatmentDto _sampleDiagnosisTreatment;
        private QueueingDto _sampleQueueItem;
        private RecordDto _sampleRecord;
        private PharmacyDto _samplePharmacy;
        private int _sampleQueueNumber;

        public HerbDto SampleHerb
        {
            get => _sampleHerb;
            set => SetProperty(ref _sampleHerb, value);
        }

        public PatientDto SamplePatient
        {
            get => _samplePatient;
            set => SetProperty(ref _samplePatient, value);
        }

        public DoctorDto SampleDoctor
        {
            get => _sampleDoctor;
            set => SetProperty(ref _sampleDoctor, value);
        }

        public FormulaTemplateDto SampleFormulaTemplate
        {
            get => _sampleFormulaTemplate;
            set => SetProperty(ref _sampleFormulaTemplate, value);
        }

        public UserDto SampleUser
        {
            get => _sampleUser;
            set => SetProperty(ref _sampleUser, value);
        }

        public PrescriptionDto SamplePrescription
        {
            get => _samplePrescription;
            set => SetProperty(ref _samplePrescription, value);
        }

        public RegistrationDto SampleRegistration
        {
            get => _sampleRegistration;
            set => SetProperty(ref _sampleRegistration, value);
        }

        public BillingDto SampleBilling
        {
            get => _sampleBilling;
            set => SetProperty(ref _sampleBilling, value);
        }

        public DiagnosisTreatmentDto SampleDiagnosisTreatment
        {
            get => _sampleDiagnosisTreatment;
            set => SetProperty(ref _sampleDiagnosisTreatment, value);
        }

        public QueueingDto SampleQueueItem
        {
            get => _sampleQueueItem;
            set => SetProperty(ref _sampleQueueItem, value);
        }

        public RecordDto SampleRecord
        {
            get => _sampleRecord;
            set => SetProperty(ref _sampleRecord, value);
        }

        public PharmacyDto SamplePharmacy
        {
            get => _samplePharmacy;
            set => SetProperty(ref _samplePharmacy, value);
        }

        public int SampleQueueNumber
        {
            get => _sampleQueueNumber;
            set => SetProperty(ref _sampleQueueNumber, value);
        }

        public ControlShowcaseViewModel()
        {
            InitializeSampleData();
        }

        private void InitializeSampleData()
        {
            // 中药材示例数据
            SampleHerb = new HerbDto
            {
                Id = Guid.NewGuid(),
                Name = "当归",
                Spec = "优质片",
                Price = 45.00m,
                /* Stock = 1500, */
                Unit = "克",
                IsActive = true
            };

            // 患者示例数据
            SamplePatient = new PatientDto
            {
                Id = Guid.NewGuid(),
                Name = "张三",
                Gender = Gender.Male,
                Age = 45,
                PhoneNumber = "138****5678",
                AllergyHistory = "青霉素过敏"
            };

            // 医生示例数据
            SampleDoctor = new DoctorDto
            {
                Id = Guid.NewGuid(),
                Name = "李医生",
                /* Title = DoctorTitle.ChiefPhysician, */
                /* WorkStatus = DoctorWorkStatus.Clinic, */
                Specialty = "内科"
            };

            // 验方模板示例数据
            SampleFormulaTemplate = new FormulaTemplateDto
            {
                Id = Guid.NewGuid(),
                Name = "四君子汤"
            };

            // 用户示例数据
            SampleUser = new UserDto
            {
                Id = Guid.NewGuid(),
                Username = "doctor01",
                Name = "王医生",
                Role = UserRole.DiagnosingDoctor,
                IsActive = true,
                IsOnline = true
            };

            // 处方示例数据
            SamplePrescription = new PrescriptionDto
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                CreateTime = DateTime.Now.AddHours(-2),
                Status = PrescriptionStatus.Draft
            };

            // 挂号示例数据
            SampleRegistration = new RegistrationDto
            {
                Id = Guid.NewGuid(),
                PatientName = "王五",
                DoctorName = "陈医生",
                RegistrationTime = DateTime.Now.AddHours(-1),
                Status = "已到达",
                RegistrationType = "普通号"
            };

            // 账单示例数据
            SampleBilling = new BillingDto
            {
                Id = Guid.NewGuid(),
                PatientName = "赵六",
                BillingTime = DateTime.Now,
                TotalAmount = 268.50m,
                PaidAmount = 100.00m,
                Status = BillingStatus.PartiallyPaid
            };

            // 诊断治疗示例数据
            SampleDiagnosisTreatment = new DiagnosisTreatmentDto
            {
                Id = Guid.NewGuid(),
                PatientName = "孙七",
                Diagnosis = "风寒感冒，症见发热恶寒、头痛身痛、鼻塞流涕、咳嗽咽干",
                CreateTime = DateTime.Now.AddHours(-3),
                // DiagnosisTreatmentDto 没有 DoctorName 属性
            };

            // 排队示例数据
            SampleQueueItem = new QueueingDto
            {
                Id = Guid.NewGuid(),
                PatientName = "周八",
                DoctorName = "吴医生",
                QueueType = "普通门诊",
                QueueTime = DateTime.Now.AddMinutes(-30),
                Status = "等待中"
            };
            SampleQueueNumber = 23;

            // 病历示例数据
            SampleRecord = new RecordDto
            {
                Id = Guid.NewGuid(),
                PatientName = "郑九",
                Diagnosis = "慢性胃炎，症见胃脘疼痛、食欲不振、嗳气反酸",
                RecordTime = DateTime.Now.AddDays(-7),
                IsShared = true
            };

            // 药房示例数据
            SamplePharmacy = new PharmacyDto
            {
                Id = Guid.NewGuid(),
                PatientName = "钱十",
                Status = "Dispensing", // 配药中
                CreateTime = DateTime.Now.AddMinutes(-15)
            };
        }
    }
}