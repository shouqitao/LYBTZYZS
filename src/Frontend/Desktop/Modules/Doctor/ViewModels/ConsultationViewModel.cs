using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Records;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Records;
using LYBT.Shared.Models.Enums;
using HerbStatus = LYBT.Shared.Models.Enums.HerbStatus;
using Gender = LYBT.Shared.Models.Enums.Gender;

namespace LYBT.WPF.Client.Modules.Doctor.ViewModels
{
    /// <summary>
    /// 看诊界面视图模型
    /// </summary>
    public class ConsultationViewModel : BindableBase, INotifyPropertyChanged
    {
        private readonly ICommonDialogService _commonDialogService;

        private readonly IHerbService _herbService;
        private readonly IRecordService _recordService;
        private readonly IPrescriptionPrintService _prescriptionPrintService;

        public ConsultationViewModel(IHerbService herbService, IRecordService recordService, IPrescriptionPrintService prescriptionPrintService,
            ICommonDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            _herbService = herbService;
            _recordService = recordService;
            _prescriptionPrintService = prescriptionPrintService;

            InitializeCommands();
            InitializeData();
        }

        #region Properties

        private RecordInfo _currentRecord = new();
        public RecordInfo CurrentRecord
        {
            get => _currentRecord;
            set
            {
                SetProperty(ref _currentRecord, value);
                RaisePropertyChanged(nameof(PatientAge));
                RaisePropertyChanged(nameof(PrescriptionSummary));
                RaisePropertyChanged(nameof(TotalAmountText));
            }
        }

        private ObservableCollection<HerbInfo> _availableHerbs = new();
        public ObservableCollection<HerbInfo> AvailableHerbs
        {
            get => _availableHerbs;
            set => SetProperty(ref _availableHerbs, value);
        }

        private HerbInfo _selectedHerb;
        public HerbInfo SelectedHerb
        {
            get => _selectedHerb;
            set => SetProperty(ref _selectedHerb, value);
        }

        private string _prescriptionPreview = string.Empty;
        public string PrescriptionPreview
        {
            get => _prescriptionPreview;
            set => SetProperty(ref _prescriptionPreview, value);
        }

        public string PatientAge
        {
            get
            {
                if (CurrentRecord.Patient.BirthDate.HasValue)
                {
                    var age = DateTime.Now.Year - CurrentRecord.Patient.BirthDate.Value.Year;
                    if (DateTime.Now.DayOfYear < CurrentRecord.Patient.BirthDate.Value.DayOfYear)
                        age--;
                    return age > 0 ? $"{age}岁" : "不满1岁";
                }
                return "未知";
            }
        }

        public string PrescriptionSummary
        {
            get
            {
                var count = CurrentRecord.Prescription?.Count ?? 0;
                return $"处方药材：{count} 种";
            }
        }

        public string TotalAmountText
        {
            get
            {
                var total = CurrentRecord.TotalAmount;
                return $"总计金额：￥{total:F2}";
            }
        }

        #endregion

        #region Commands

        public DelegateCommand AddHerbCommand { get; private set; } = null!;
        public DelegateCommand<PrescriptionItem> RemoveHerbCommand { get; private set; } = null!;
        public DelegateCommand GeneratePreviewCommand { get; private set; } = null!;
        public DelegateCommand SaveRecordCommand { get; private set; } = null!;
        public DelegateCommand PrintPrescriptionCommand { get; private set; } = null!;
        public DelegateCommand SavePdfCommand { get; private set; } = null!;
        public DelegateCommand CompleteConsultationCommand { get; private set; } = null!;

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            AddHerbCommand = new DelegateCommand(AddHerb, CanAddHerb);
            RemoveHerbCommand = new DelegateCommand<PrescriptionItem>(RemoveHerb);
            GeneratePreviewCommand = new DelegateCommand(async () => await GeneratePreview());
            SaveRecordCommand = new DelegateCommand(async () => await SaveRecord());
            PrintPrescriptionCommand = new DelegateCommand(async () => await PrintPrescription());
            SavePdfCommand = new DelegateCommand(async () => await SavePdf());
            CompleteConsultationCommand = new DelegateCommand(async () => await CompleteConsultation());
        }

        private async void InitializeData()
        {
            await LoadAvailableHerbs();
            InitializeNewConsultation();
        }

        private void InitializeNewConsultation()
        {
            CurrentRecord = new RecordInfo
            {
                Id = Guid.NewGuid(),
                RecordId = Guid.NewGuid(),
                CreateTime = DateTime.Now,
                RecordTime = DateTime.Now,
                VisitTime = DateTime.Now,
                Status = "InProgress",
                DoctorId = Guid.NewGuid(), // 当前登录医生ID
                DoctorName = "当前医生", // 从登录信息获取
                Patient = new PatientInfo
                {
                    Id = Guid.NewGuid(),
                    Name = "新患者",
                    Gender = Gender.Unknown,
                    BirthDate = DateTime.Now.AddYears(-30)
                },
                PatientId = Guid.NewGuid()
            };
        }

        #endregion

        #region Command Implementations

        private bool CanAddHerb()
        {
            return SelectedHerb != null;
        }

        private void AddHerb()
        {
            if (SelectedHerb == null) return;

            // 检查是否已存在
            var existingItem = CurrentRecord.Prescription.FirstOrDefault(p => p.Herb.Id == SelectedHerb.Id);
            if (existingItem != null)
            {
                _commonDialogService.ShowInformationAsync("该药材已在处方中，请修改剂量或删除后重新添加。", "提示").GetAwaiter().GetResult();
                return;
            }

            var prescriptionItem = new PrescriptionItem
            {
                Herb = SelectedHerb,
                Quantity = 10, // 默认数量
                Unit = "g",
                UnitPrice = SelectedHerb.Price,
                Usage = "水煎服"
            };

            CurrentRecord.Prescription.Add(prescriptionItem);
            
            // 更新界面
            RaisePropertyChanged(nameof(PrescriptionSummary));
            RaisePropertyChanged(nameof(TotalAmountText));
            
            SelectedHerb = null;
        }

        private void RemoveHerb(PrescriptionItem item)
        {
            if (item != null)
            {
                CurrentRecord.Prescription.Remove(item);
                RaisePropertyChanged(nameof(PrescriptionSummary));
                RaisePropertyChanged(nameof(TotalAmountText));
            }
        }

        private async Task GeneratePreview()
        {
            try
            {
                var medicalRecord = ConvertToMedicalRecord(CurrentRecord);
                var preview = await _prescriptionPrintService.PreviewPrescriptionAsync(medicalRecord);
                PrescriptionPreview = preview.Content;
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"生成预览失败：{ex.Message}", "错误");
            }
        }

        private async Task SaveRecord()
        {
            try
            {
                var medicalRecord = ConvertToMedicalRecord(CurrentRecord);
                var createDto = new RecordCreateDto
                {
                    PatientId = medicalRecord.PatientId,
                    RegistrationId = Guid.NewGuid(), // TODO: 需要从实际挂号记录获取
                    Diagnosis = medicalRecord.Diagnosis,
                    ChiefComplaint = medicalRecord.ChiefComplaint,
                    PresentIllness = medicalRecord.PresentIllness,
                    TreatmentAdvice = medicalRecord.TreatmentAdvice,
                    DiagnosisResults = new List<string>(), // TODO: 从诊断结果获取
                    HerbalFormula = medicalRecord.HerbalFormula?.Select(h => new FormulaIngredientDto
                    {
                        HerbId = h.HerbId,
                        Quantity = h.Quantity,
                        Unit = h.Unit
                    }).ToList(),
                    IsShared = false,
                    CreateTime = DateTime.Now,
                    RecordTime = DateTime.Now
                };

                var result = await _recordService.AddAsync(createDto);
                if (result.IsSuccess)
                {
                    _commonDialogService.ShowInformationAsync("病历保存成功！", "成功").GetAwaiter().GetResult();
                }
                else
                {
                    _commonDialogService.ShowErrorAsync($"病历保存失败：{result.ErrorMessage}", "错误").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"保存病历时发生错误：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private async Task PrintPrescription()
        {
            try
            {
                if (!CurrentRecord.Prescription.Any())
                {
                    await _commonDialogService.ShowWarningAsync("处方为空，无法打印。", "提示");
                    return;
                }

                var medicalRecord = ConvertToMedicalRecord(CurrentRecord);
                var success = await _prescriptionPrintService.PrintPrescriptionAsync(medicalRecord);
                
                if (success)
                {
                    _commonDialogService.ShowInformationAsync("处方已发送到打印机。", "成功").GetAwaiter().GetResult();
                }
                else
                {
                    _commonDialogService.ShowErrorAsync("打印失败，请检查打印机设置。", "错误").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打印处方时发生错误：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private async Task SavePdf()
        {
            try
            {
                if (!CurrentRecord.Prescription.Any())
                {
                    await _commonDialogService.ShowWarningAsync("处方为空，无法保存。", "提示");
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                    DefaultExt = "txt",
                    FileName = $"处方_{CurrentRecord.Patient.Name}_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var medicalRecord = ConvertToMedicalRecord(CurrentRecord);
                    var success = await _prescriptionPrintService.SaveAsPdfAsync(medicalRecord, saveDialog.FileName);
                    
                    if (success)
                    {
                        _commonDialogService.ShowInformationAsync($"处方已保存到：{saveDialog.FileName}", "成功").GetAwaiter().GetResult();
                    }
                    else
                    {
                        _commonDialogService.ShowErrorAsync("保存失败。", "错误").GetAwaiter().GetResult();
                    }
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"保存PDF时发生错误：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private async Task CompleteConsultation()
        {
            try
            {
                // 保存病历
                await SaveRecord();
                
                // 标记看诊完成
                CurrentRecord.Status = "Completed";
                CurrentRecord.UpdateTime = DateTime.Now;
                
                await _commonDialogService.ShowInformationAsync("看诊已完成！", "成功");
                
                // 重置为新的看诊
                InitializeNewConsultation();
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"完成看诊时发生错误：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        #endregion

        #region Helper Methods

        private async Task LoadAvailableHerbs()
        {
            try
            {
                var herbs = await _herbService.GetAvailableHerbsAsync();
                AvailableHerbs.Clear();
                foreach (var herb in herbs)
                {
                    AvailableHerbs.Add(herb);
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"加载药材列表失败：{ex.Message}", "错误");
            }
        }

        /// <summary>
        /// 将 HerbDto 转换为 HerbInfo
        /// </summary>
        private HerbInfo ConvertHerbDtoToHerbInfo(LYBT.Shared.Models.Contracts.Herbs.HerbDto herbDto)
        {
            return new HerbInfo
            {
                Id = herbDto.Id,
                Name = herbDto.Name,
                PinYinCode = herbDto.PinYinCode,
                // WuBiCode = herbDto.WuBiCode, // HerbDto中没有WuBiCode属性
                Origin = herbDto.Origin,
                Spec = herbDto.Spec,
                Unit = herbDto.Unit,
                Price = herbDto.Price,
                /* Stock = (int)herbDto.Stock, */ // 需要转换为int
                // /* BatchNo = herbDto.BatchNo, */ // HerbDto中没有BatchNo属性
                // ExpireDate = herbDto.ExpireDate, // HerbDto中没有ExpireDate属性
                Effect = herbDto.Effect,
                Remark = herbDto.Remark,
                Status = (HerbStatus)herbDto.Status, // 需要转换为枚举
                IsActive = herbDto.IsActive,
                CreateTime = herbDto.CreateTime,
                UpdateTime = herbDto.UpdateTime
            };
        }

        /// <summary>
        /// 将RecordInfo转换为MedicalRecord（用于打印和显示）
        /// </summary>
        private MedicalRecord ConvertToMedicalRecord(RecordInfo record)
        {
            return new MedicalRecord
            {
                Id = record.Id,
                RecordId = record.RecordId,
                PatientId = record.PatientId,
                PatientName = record.Patient.Name,
                PatientGender = record.Patient.Gender.ToString(),
                PatientAge = GetPatientAge(record.Patient.BirthDate),
                PatientPhone = record.Patient.PhoneNumber ?? string.Empty,
                DoctorId = record.DoctorId,
                DoctorName = record.DoctorName,
                ChiefComplaint = record.ChiefComplaint,
                Diagnosis = record.Diagnosis,
                PresentIllness = record.PresentIllness,
                TreatmentAdvice = record.TreatmentAdvice,
                HerbalFormula = record.Prescription.Select(p => new FormulaIngredient
                {
                    HerbId = p.Herb.Id,
                    HerbName = p.Herb.Name,
                    Quantity = p.Quantity,
                    Unit = p.Unit,
                    Usage = p.Usage
                }).ToList(),
                RecordTime = record.RecordTime,
                TotalAmount = record.TotalAmount
            };
        }

        /// <summary>
        /// 计算患者年龄
        /// </summary>
        private int GetPatientAge(DateTime? birthDate)
        {
            if (!birthDate.HasValue) return 0;
            var age = DateTime.Now.Year - birthDate.Value.Year;
            if (DateTime.Now.DayOfYear < birthDate.Value.DayOfYear)
                age--;
            return age;
        }

        #endregion
    }
}