using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Doctors;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 医生服务实现
    /// </summary>
    public class DoctorService : IDoctorService
    {
        public async Task<List<DoctorInfo>> GetDoctorsAsync()
        {
            await Task.Delay(300); // 模拟API调用
            
            return new List<DoctorInfo>
            {
                new DoctorInfo
                {
                    Id = Guid.NewGuid(),
                    Code = "D001",
                    Name = "张医生",
                    Gender = Gender.Male,
                    Department = "内科",
                    Title = DoctorTitle.ChiefPhysician,
                    Phone = "13800138001",
                    Specialties = "中医内科、脾胃病",
                    IsActive = true,
                    CreateTime = DateTime.Now.AddMonths(-6)
                },
                new DoctorInfo
                {
                    Id = Guid.NewGuid(),
                    Code = "D002",
                    Name = "李医生",
                    Gender = Gender.Female,
                    Department = "妇科",
                    Title = DoctorTitle.AssociateChiefPhysician,
                    Phone = "13900139001",
                    Specialties = "中医妇科、月经病",
                    IsActive = true,
                    CreateTime = DateTime.Now.AddMonths(-3)
                },
                new DoctorInfo
                {
                    Id = Guid.NewGuid(),
                    Code = "D003",
                    Name = "王医生",
                    Gender = Gender.Male,
                    Department = "骨科",
                    Title = DoctorTitle.AttendingPhysician,
                    Phone = "13700137001",
                    Specialties = "中医骨伤、针灸推拿",
                    IsActive = true,
                    CreateTime = DateTime.Now.AddMonths(-1)
                }
            };
        }

        public async Task<DoctorInfo> GetDoctorByIdAsync(Guid id)
        {
            var doctors = await GetDoctorsAsync();
            return doctors.Find(d => d.Id == id) ?? new DoctorInfo();
        }

        public async Task<bool> AddDoctorAsync(DoctorInfo doctor)
        {
            await Task.Delay(300);
            return true;
        }

        public async Task<bool> UpdateDoctorAsync(DoctorInfo doctor)
        {
            await Task.Delay(300);
            return true;
        }

        public async Task<bool> DeleteDoctorAsync(Guid id)
        {
            await Task.Delay(300);
            return true;
        }

        public async Task<List<DoctorInfo>> GetByDepartmentAsync(string department)
        {
            await Task.Delay(300);
            var allDoctors = await GetDoctorsAsync();
            return allDoctors.FindAll(d => d.Department == department);
        }
    }
}