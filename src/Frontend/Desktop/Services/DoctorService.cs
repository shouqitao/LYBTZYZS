using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Doctors;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.WPF.Client.Core.Models;
using LYBT.Shared.Models.Contracts.Doctors;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 医生服务实现
    /// </summary>
    public class DoctorService : IDoctorService
    {
        private readonly IDoctorsApiService _doctorsApiService;

        public DoctorService(IDoctorsApiService doctorsApiService)
        {
            _doctorsApiService = doctorsApiService;
        }

        public async Task<ServiceResult<List<DoctorInfo>>> GetDoctorsAsync()
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _doctorsApiService.GetActiveDoctorsAsync()
            );
            
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                var doctors = apiResponse.Data.Select(ConvertToDoctorInfo).ToList();
                return ServiceResult<List<DoctorInfo>>.Success(doctors);
            }
            
            return ServiceResult<List<DoctorInfo>>.Failure(apiResponse.ErrorMessage ?? "获取医生列表失败", apiResponse.Exception);
        }

        public async Task<ServiceResult<DoctorInfo>> GetDoctorByIdAsync(Guid id)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _doctorsApiService.GetByIdAsync(id)
            );
            
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                return ServiceResult<DoctorInfo>.Success(ConvertDetailToDoctorInfo(apiResponse.Data));
            }
            
            return ServiceResult<DoctorInfo>.Failure(apiResponse.ErrorMessage ?? "获取医生详情失败", apiResponse.Exception);
        }

        public async Task<ServiceResult> AddDoctorAsync(DoctorInfo doctor)
        {
            var dto = ConvertToDetailDto(doctor);
            var result = await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _doctorsApiService.AddAsync(dto)
            );
            return result.IsSuccess ? ServiceResult.Success() : ServiceResult.Failure(result.ErrorMessage ?? "操作失败", result.Exception);
        }

        public async Task<ServiceResult> UpdateDoctorAsync(DoctorInfo doctor)
        {
            var dto = ConvertToDetailDto(doctor);
            var result = await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _doctorsApiService.UpdateAsync(doctor.Id, dto)
            );
            return result.IsSuccess ? ServiceResult.Success() : ServiceResult.Failure(result.ErrorMessage ?? "操作失败", result.Exception);
        }

        public async Task<ServiceResult> DeleteDoctorAsync(Guid id)
        {
            var result = await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _doctorsApiService.DisableAsync(id)
            );
            return result.IsSuccess ? ServiceResult.Success() : ServiceResult.Failure(result.ErrorMessage ?? "操作失败", result.Exception);
        }

        public async Task<ServiceResult<List<DoctorInfo>>> GetByDepartmentAsync(string department)
        {
            // 由于API没有提供按科室查询的接口，暂时获取所有医生后在本地过滤
            var allDoctorsResult = await GetDoctorsAsync();
            if (allDoctorsResult.IsSuccess && allDoctorsResult.Data != null)
            {
                var filteredDoctors = allDoctorsResult.Data.Where(d => d.Department == department).ToList();
                return ServiceResult<List<DoctorInfo>>.Success(filteredDoctors);
            }
            
            return ServiceResult<List<DoctorInfo>>.Failure(allDoctorsResult.ErrorMessage ?? "获取科室医生失败", allDoctorsResult.Exception);
        }

        public async Task<ServiceResult<DoctorInfo>> GetDoctorByUserIdAsync(Guid userId)
        {
            // 获取所有医生后在本地过滤
            var allDoctorsResult = await GetDoctorsAsync();
            if (allDoctorsResult.IsSuccess && allDoctorsResult.Data != null)
            {
                var doctor = allDoctorsResult.Data.FirstOrDefault(d => d.UserId == userId);
                if (doctor != null)
                {
                    return ServiceResult<DoctorInfo>.Success(doctor);
                }
                return ServiceResult<DoctorInfo>.Failure("未找到该用户的医生档案");
            }
            
            return ServiceResult<DoctorInfo>.Failure(allDoctorsResult.ErrorMessage ?? "获取医生信息失败", allDoctorsResult.Exception);
        }

        /// <summary>
        /// 转换DoctorDto到DoctorInfo
        /// </summary>
        private DoctorInfo ConvertToDoctorInfo(DoctorDto dto)
        {
            return new DoctorInfo
            {
                Id = dto.Id,
                Code = dto.PinYinCode ?? "",
                Name = dto.Name ?? "",
                // Gender = dto.Gender // TODO: 字段已移除,
                /* Department = dto.Specialty ?? "", */
                // /* Title = dto.Title // TODO: 字段已移除, */
                Phone = dto.ContactNumber ?? "",
                Specialties = dto.Specialty ?? "",
                IsActive = dto.Status == DoctorStatus.Active,
                CreateTime = DateTime.Now // DoctorDto 不包含 CreateTime
            };
        }

        /// <summary>
        /// 转换DoctorDetailDto到DoctorInfo
        /// </summary>
        private DoctorInfo ConvertDetailToDoctorInfo(DoctorDetailDto dto)
        {
            return new DoctorInfo
            {
                Id = dto.Id,
                Code = dto.PinYinCode ?? "",
                Name = dto.Name ?? "",
                // Gender = dto.Gender // TODO: 字段已移除,
                /* Department = dto.Specialty ?? "", */
                // /* Title = dto.Title // TODO: 字段已移除, */
                Phone = dto.ContactNumber ?? "",
                Specialties = dto.Specialty ?? "",
                IsActive = dto.Status == DoctorStatus.Active,
                CreateTime = DateTime.Now // DoctorDetailDto 不包含 CreateTime
            };
        }

        /// <summary>
        /// 转换DoctorInfo到DoctorDetailDto
        /// </summary>
        private DoctorDetailDto ConvertToDetailDto(DoctorInfo info)
        {
            return new DoctorDetailDto
            {
                Id = info.Id,
                UserId = info.UserId != Guid.Empty ? info.UserId : Guid.NewGuid(), // 如果没有UserId，生成一个新的
                PinYinCode = info.PinYinCode ?? info.Code ?? string.Empty,
                Name = info.Name,
                // Gender = info.Gender, // 字段已移除
                // Birthday = info.Birthday, // 字段已移除
                // /* Title = info.Title, */ // 字段已移除
                LicenseNumber = info.LicenseNumber,
                // PhoneNumber = info.Phone, // 字段已移除
                ContactNumber = info.ContactNumber ?? info.Phone,
                Specialty = info.Specialties ?? info.Specialty ?? string.Empty,
                Status = info.IsActive ? DoctorStatus.Active : DoctorStatus.Inactive,
                // /* WorkStatus = info.WorkStatus, */ // 字段已移除
                // Remark = info.Remark, // 字段已移除
                // Age = info.Age // 字段已移除
                RegistrationFee = 50 // 默认挂号费
            };
        }
    }
}