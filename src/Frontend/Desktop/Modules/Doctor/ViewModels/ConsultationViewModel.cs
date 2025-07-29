using LYBT.WPF.Client.Core.Models.Consultation;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Models.Users;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.WPF.Client.Core.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.WPF.Client.Modules.Doctor.ViewModels
{
    /// <summary>
    /// 诊疗视图模型
    /// </summary>
    public class ConsultationViewModel : BindableBase
    {
        private ConsultationRecord _currentRecord;
        private bool _isLoading = false;

        // Commands
        public DelegateCommand SaveRecordCommand { get; }
        public DelegateCommand PrintPrescriptionCommand { get; }
        public DelegateCommand CompleteConsultationCommand { get; }
        public DelegateCommand AddHerbCommand { get; }
        public DelegateCommand LoadTemplateCommand { get; }
        public DelegateCommand<PrescriptionItem> RemoveHerbCommand { get; }

        /// <summary>当前诊疗记录</summary>
        public ConsultationRecord CurrentRecord
        {
            get => _currentRecord;
            set => SetProperty(ref _currentRecord, value);
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ConsultationViewModel()
        {
            // 初始化命令
            SaveRecordCommand = new DelegateCommand(ExecuteSaveRecord);
            PrintPrescriptionCommand = new DelegateCommand(ExecutePrintPrescription);
            CompleteConsultationCommand = new DelegateCommand(ExecuteCompleteConsultation);
            AddHerbCommand = new DelegateCommand(ExecuteAddHerb);
            LoadTemplateCommand = new DelegateCommand(ExecuteLoadTemplate);
            RemoveHerbCommand = new DelegateCommand<PrescriptionItem>(ExecuteRemoveHerb);

            // 初始化诊疗记录
            InitializeConsultationRecord();
        }

        private void InitializeConsultationRecord()
        {
            // 创建示例诊疗记录
            CurrentRecord = new ConsultationRecord
            {
                Id = Guid.NewGuid(),
                ConsultationDate = DateTime.Now,
                Status = ConsultationStatus.InProgress,
                Patient = new PatientInfo
                {
                    Id = Guid.NewGuid(),
                    Name = "张三",
                    Gender = Gender.Male,
                    Age = 45,
                    PhoneNumber = "13800138000",
                    Address = "北京市朝阳区",
                    CreatedTime = DateTime.Now
                },
                Doctor = new UserInfo
                {
                    Id = Guid.NewGuid(),
                    UserName = "doctor01",
                    RealName = "李医生",
                    Role = UserRole.DiagnosingDoctor,
                    CreatedTime = DateTime.Now
                },
                TCMDiagnosis = new TCMDiagnosis(),
                Prescription = new List<PrescriptionItem>(),
                CreatedTime = DateTime.Now
            };

            // 添加示例处方
            AddSamplePrescription();
        }

        private void AddSamplePrescription()
        {
            var sampleHerbs = new[]
            {
                new { Name = "当归", Dosage = 10m, Price = 2.5m },
                new { Name = "白芍", Dosage = 15m, Price = 1.8m },
                new { Name = "川芎", Dosage = 8m, Price = 3.2m },
                new { Name = "熟地黄", Dosage = 12m, Price = 2.8m }
            };

            foreach (var herb in sampleHerbs)
            {
                CurrentRecord.Prescription.Add(new PrescriptionItem
                {
                    Herb = new HerbInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = herb.Name,
                        Unit = "g",
                        Price = herb.Price,
                        Stock = 100
                    },
                    Dosage = herb.Dosage,
                    Unit = "g",
                    UnitPrice = herb.Price,
                    Usage = "煎服"
                });
            }
        }

        private void ExecuteSaveRecord()
        {
            try
            {
                IsLoading = true;
                
                // TODO: 调用API保存诊疗记录
                // await _consultationService.SaveRecordAsync(CurrentRecord);
                
                // 模拟保存操作
                Task.Delay(1000).Wait();
                
                // 显示成功消息
                System.Windows.MessageBox.Show("诊疗记录保存成功！", "提示", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"保存失败：{ex.Message}", "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecutePrintPrescription()
        {
            try
            {
                // TODO: 实现处方打印功能
                System.Windows.MessageBox.Show("处方打印功能开发中...", "提示", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"打印失败：{ex.Message}", "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteCompleteConsultation()
        {
            try
            {
                var result = System.Windows.MessageBox.Show("确定要完成本次诊疗吗？", "确认", 
                    System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    CurrentRecord.Status = ConsultationStatus.Completed;
                    
                    // TODO: 调用API更新诊疗状态
                    ExecuteSaveRecord();
                    
                    System.Windows.MessageBox.Show("诊疗已完成！", "提示", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"完成诊疗失败：{ex.Message}", "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteAddHerb()
        {
            try
            {
                // TODO: 打开药材选择对话框
                // 这里添加示例药材
                var newHerb = new PrescriptionItem
                {
                    Herb = new HerbInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "甘草",
                        Unit = "g",
                        Price = 1.5m,
                        Stock = 50
                    },
                    Dosage = 6m,
                    Unit = "g",
                    UnitPrice = 1.5m,
                    Usage = "调和诸药"
                };

                CurrentRecord.Prescription.Add(newHerb);
                RaisePropertyChanged(nameof(CurrentRecord));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"添加药材失败：{ex.Message}", "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteLoadTemplate()
        {
            try
            {
                // TODO: 打开验方模板选择对话框
                System.Windows.MessageBox.Show("验方模板加载功能开发中...", "提示", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载模板失败：{ex.Message}", "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteRemoveHerb(PrescriptionItem item)
        {
            if (item == null) return;

            try
            {
                var result = System.Windows.MessageBox.Show($"确定要删除药材\"{item.Herb.Name}\"吗？", "确认", 
                    System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    CurrentRecord.Prescription.Remove(item);
                    RaisePropertyChanged(nameof(CurrentRecord));
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"删除药材失败：{ex.Message}", "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}