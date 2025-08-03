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
using LYBT.WPF.Client.Core.Models.Consultation;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.Shared.Models.Records;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Records;
using LYBT.Shared.Models.Enums;
using HerbStatus = LYBT.Shared.Models.Enums.HerbStatus;

namespace LYBT.WPF.Client.Modules.Doctor.ViewModels
{
    /// <summary>
    /// 看诊界面视图模型
    /// </summary>
    public class ConsultationViewModel : BindableBase, INotifyPropertyChanged
    {
        private readonly IHerbService _herbService;
        private readonly IRecordService _recordService;
        private readonly IPrescriptionPrintService _prescriptionPrintService;

        public ConsultationViewModel(IHerbService herbService, IRecordService recordService, IPrescriptionPrintService prescriptionPrintService)
        {
            _herbService = herbService;
            _recordService = recordService;
            _prescriptionPrintService = prescriptionPrintService;

            InitializeCommands();
            InitializeData();
        }

        #region Properties

        private ConsultationRecord _currentRecord = new();
        public ConsultationRecord CurrentRecord
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
            CurrentRecord = new ConsultationRecord
            {
                Id = Guid.NewGuid(),
                ConsultationDate = DateTime.Now,
                Status = ConsultationStatus.InProgress,
                DoctorId = Guid.NewGuid(), // 当前登录医生ID
                DoctorName = "当前医生", // 从登录信息获取
                Patient = new PatientInfo
                {
                    Id = Guid.NewGuid(),
                    Name = "新患者",
                    Gender = Gender.Unknown,
                    BirthDate = DateTime.Now.AddYears(-30)
                }
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
                MessageBox.Show("该药材已在处方中，请修改剂量或删除后重新添加。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var prescriptionItem = new PrescriptionItem
            {
                Herb = SelectedHerb,
                Dosage = 10, // 默认剂量
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
                var medicalRecord = CurrentRecord.ToMedicalRecord();
                var preview = await _prescriptionPrintService.PreviewPrescriptionAsync(medicalRecord);
                PrescriptionPreview = preview.Content;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"生成预览失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SaveRecord()
        {
            try
            {
                var medicalRecord = CurrentRecord.ToMedicalRecord();
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
                        Dosage = h.Dosage,
                        Unit = h.Unit
                    }).ToList(),
                    IsShared = false,
                    CreatedTime = DateTime.Now,
                    RecordTime = DateTime.Now
                };

                var result = await _recordService.AddAsync(createDto);
                if (result.IsSuccess)
                {
                    MessageBox.Show("病历保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"病历保存失败：{result.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存病历时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task PrintPrescription()
        {
            try
            {
                if (!CurrentRecord.Prescription.Any())
                {
                    MessageBox.Show("处方为空，无法打印。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var medicalRecord = CurrentRecord.ToMedicalRecord();
                var success = await _prescriptionPrintService.PrintPrescriptionAsync(medicalRecord);
                
                if (success)
                {
                    MessageBox.Show("处方已发送到打印机。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("打印失败，请检查打印机设置。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打印处方时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SavePdf()
        {
            try
            {
                if (!CurrentRecord.Prescription.Any())
                {
                    MessageBox.Show("处方为空，无法保存。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    var medicalRecord = CurrentRecord.ToMedicalRecord();
                    var success = await _prescriptionPrintService.SaveAsPdfAsync(medicalRecord, saveDialog.FileName);
                    
                    if (success)
                    {
                        MessageBox.Show($"处方已保存到：{saveDialog.FileName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("保存失败。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存PDF时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CompleteConsultation()
        {
            try
            {
                // 保存病历
                await SaveRecord();
                
                // 标记看诊完成
                CurrentRecord.Status = ConsultationStatus.Completed;
                CurrentRecord.UpdatedTime = DateTime.Now;
                
                MessageBox.Show("看诊已完成！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // 重置为新的看诊
                InitializeNewConsultation();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"完成看诊时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helper Methods

        private async Task LoadAvailableHerbs()
        {
            try
            {
                var result = await _herbService.GetAvailableAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    AvailableHerbs.Clear();
                    foreach (var herb in result.Data)
                    {
                        AvailableHerbs.Add(ConvertHerbDtoToHerbInfo(herb));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载药材列表失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                PinyinCode = herbDto.PinyinCode,
                // WuBiCode = herbDto.WuBiCode, // HerbDto中没有WuBiCode属性
                Origin = herbDto.Origin,
                Spec = herbDto.Spec,
                Unit = herbDto.Unit,
                Price = herbDto.Price,
                Stock = (int)herbDto.Stock, // 需要转换为int
                // BatchNo = herbDto.BatchNo, // HerbDto中没有BatchNo属性
                // ExpireDate = herbDto.ExpireDate, // HerbDto中没有ExpireDate属性
                Effect = herbDto.Effect,
                Remark = herbDto.Remark,
                Status = (HerbStatus)herbDto.Status, // 需要转换为枚举
                IsActive = herbDto.IsActive,
                CreateTime = herbDto.CreateTime,
                UpdateTime = herbDto.UpdateTime
            };
        }

        #endregion
    }
}