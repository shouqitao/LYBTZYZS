using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Physiotherapy;
using LYBT.WPF.Client.Core.Models.TreatmentRoom;
using LYBT.WPF.Client.Core.Models.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 理疗服务实现
    /// </summary>
    public class PhysiotherapyService : IPhysiotherapyService
    {
        public async Task<List<TreatmentExecutionInfo>> GetExecutionsAsync(DateTime? date = null, string? status = null)
        {
            await Task.Delay(300); // 模拟API调用
            
            var executions = new List<TreatmentExecutionInfo>
            {
                new TreatmentExecutionInfo
                {
                    Id = Guid.NewGuid(),
                    ExecutionNumber = "TE202501010001",
                    RecordId = Guid.NewGuid(),
                    TreatmentCatalogId = Guid.NewGuid(),
                    TreatmentCatalogName = "针灸治疗",
                    PatientId = Guid.NewGuid(),
                    PatientName = "张三",
                    PatientGender = "男",
                    PatientAge = 45,
                    PatientPhone = "13800138001",
                    DoctorId = Guid.NewGuid(),
                    DoctorName = "王医生",
                    TherapistId = Guid.NewGuid(),
                    TherapistName = "李医师",
                    Status = "Appointed",
                    AppointmentTime = DateTime.Today.AddHours(9),
                    TimeSlot = "09:00-09:30",
                    Fee = 180,
                    IsPaid = true,
                    CreateTime = DateTime.Now.AddDays(-1)
                },
                new TreatmentExecutionInfo
                {
                    Id = Guid.NewGuid(),
                    ExecutionNumber = "TE202501010002",
                    RecordId = Guid.NewGuid(),
                    TreatmentCatalogId = Guid.NewGuid(),
                    TreatmentCatalogName = "推拿按摩",
                    PatientId = Guid.NewGuid(),
                    PatientName = "李四",
                    PatientGender = "女",
                    PatientAge = 38,
                    PatientPhone = "13900139002",
                    DoctorId = Guid.NewGuid(),
                    DoctorName = "赵医生",
                    TherapistId = Guid.NewGuid(),
                    TherapistName = "王医师",
                    Status = "InProgress",
                    AppointmentTime = DateTime.Today.AddHours(10),
                    TimeSlot = "10:00-10:45",
                    StartTime = DateTime.Today.AddHours(10),
                    Fee = 150,
                    IsPaid = true,
                    CreateTime = DateTime.Now.AddDays(-1)
                }
            };

            return executions;
        }

        public async Task<List<TreatmentCatalogInfo>> GetTreatmentCatalogAsync()
        {
            await Task.Delay(200); // 模拟API调用
            
            return new List<TreatmentCatalogInfo>
            {
                new TreatmentCatalogInfo
                {
                    Id = Guid.NewGuid(),
                    Code = "ZJ001",
                    Name = "针灸治疗",
                    Category = "针灸",
                    Price = 180,
                    Duration = 30,
                    Description = "传统针灸治疗，适用于各种疼痛症状",
                    Precautions = "空腹或饱食后不宜针灸",
                    Indications = "颈肩腰腿痛、头痛、失眠等",
                    Contraindications = "孕妇、严重心脏病患者慎用",
                    IsActive = true,
                    CreateTime = DateTime.Now.AddMonths(-6)
                },
                new TreatmentCatalogInfo
                {
                    Id = Guid.NewGuid(),
                    Code = "TN001",
                    Name = "推拿按摩",
                    Category = "推拿",
                    Price = 150,
                    Duration = 45,
                    Description = "中医推拿按摩，舒筋活络",
                    Precautions = "皮肤破损处不宜推拿",
                    Indications = "肌肉酸痛、疲劳、关节不适",
                    Contraindications = "骨折、皮肤病变处",
                    IsActive = true,
                    CreateTime = DateTime.Now.AddMonths(-6)
                },
                new TreatmentCatalogInfo
                {
                    Id = Guid.NewGuid(),
                    Code = "GS001",
                    Name = "拔罐治疗",
                    Category = "拔罐",
                    Price = 120,
                    Duration = 20,
                    Description = "拔罐疗法，祛湿散寒",
                    Precautions = "皮肤过敏者慎用",
                    Indications = "风寒感冒、湿气重、肌肉酸痛",
                    Contraindications = "皮肤溃疡、血液病患者",
                    IsActive = true,
                    CreateTime = DateTime.Now.AddMonths(-3)
                }
            };
        }

        public async Task<bool> CreateExecutionAsync(TreatmentExecutionInfo execution)
        {
            await Task.Delay(300);
            return true;
        }

        public async Task<bool> UpdateExecutionStatusAsync(Guid executionId, string status)
        {
            await Task.Delay(200);
            return true;
        }

        public async Task<bool> StartTreatmentAsync(Guid executionId)
        {
            await Task.Delay(200);
            return true;
        }

        public async Task<bool> CompleteTreatmentAsync(Guid executionId, string notes)
        {
            await Task.Delay(200);
            return true;
        }

        public async Task<bool> CancelExecutionAsync(Guid executionId, string reason)
        {
            await Task.Delay(200);
            return true;
        }

        public async Task<bool> AddTreatmentCatalogAsync(TreatmentCatalogInfo catalog)
        {
            await Task.Delay(300);
            return true;
        }

        public async Task<bool> UpdateTreatmentCatalogAsync(TreatmentCatalogInfo catalog)
        {
            await Task.Delay(300);
            return true;
        }

        public async Task<bool> DeleteTreatmentCatalogAsync(Guid catalogId)
        {
            await Task.Delay(200);
            return true;
        }

        public async Task<List<UserInfo>> GetTherapistsAsync()
        {
            await Task.Delay(200);
            
            return new List<UserInfo>
            {
                new UserInfo
                {
                    Id = Guid.NewGuid(),
                    Username = "therapist1",
                    RealName = "李医师",
                    PhoneNumber = "13800138000",
                    Role = UserRole.PhysiotherapyStaff,
                    IsActive = true,
                    CreateTime = DateTime.Now.AddYears(-2)
                },
                new UserInfo
                {
                    Id = Guid.NewGuid(),
                    Username = "therapist2",
                    RealName = "王医师",
                    PhoneNumber = "13900139000",
                    Role = UserRole.PhysiotherapyStaff,
                    IsActive = true,
                    CreateTime = DateTime.Now.AddYears(-1)
                }
            };
        }

        public async Task<List<PhysiotherapyAppointmentInfo>> GetAppointmentsAsync(DateTime? date = null, string? status = null)
        {
            await Task.Delay(200);
            
            return new List<PhysiotherapyAppointmentInfo>
            {
                new PhysiotherapyAppointmentInfo
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "张三",
                    TreatmentType = "针灸",
                    TreatmentName = "针灸治疗",
                    AppointmentTime = DateTime.Today.AddHours(9),
                    TherapistId = Guid.NewGuid(),
                    TherapistName = "李医师",
                    Status = "已预约",
                    Remark = "首次治疗",
                    CreateTime = DateTime.Now.AddDays(-1)
                },
                new PhysiotherapyAppointmentInfo
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "李四",
                    TreatmentType = "推拿",
                    TreatmentName = "推拿按摩",
                    AppointmentTime = DateTime.Today.AddHours(10),
                    TherapistId = Guid.NewGuid(),
                    TherapistName = "王医师",
                    Status = "进行中",
                    Remark = "第二次治疗",
                    CreateTime = DateTime.Now.AddDays(-1)
                }
            };
        }

        public async Task<List<TreatmentTypeInfo>> GetTreatmentTypesAsync()
        {
            await Task.Delay(200);
            
            return new List<TreatmentTypeInfo>
            {
                new TreatmentTypeInfo
                {
                    Id = Guid.NewGuid(),
                    Name = "针灸治疗",
                    Duration = 30,
                    Price = 180,
                    Description = "传统针灸治疗"
                },
                new TreatmentTypeInfo
                {
                    Id = Guid.NewGuid(),
                    Name = "推拿按摩",
                    Duration = 45,
                    Price = 150,
                    Description = "中医推拿按摩"
                },
                new TreatmentTypeInfo
                {
                    Id = Guid.NewGuid(),
                    Name = "拔罐治疗",
                    Duration = 20,
                    Price = 120,
                    Description = "拔罐疗法"
                }
            };
        }
    }
}