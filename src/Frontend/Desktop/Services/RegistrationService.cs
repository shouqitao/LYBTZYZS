using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Registration;
using LYBT.WPF.Client.Services.Interfaces;
using PagedResult = LYBT.WPF.Client.Core.Models.Common.PagedResult<LYBT.WPF.Client.Core.Models.Registration.RegistrationInfo>;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 挂号服务实现类
    /// </summary>
    public class RegistrationService : IRegistrationService
    {
        private readonly IRegistrationApiService _registrationApiService;

        public RegistrationService(IRegistrationApiService registrationApiService)
        {
            _registrationApiService = registrationApiService;
        }

        /// <summary>
        /// 分页查询挂号记录
        /// </summary>
        public async Task<PagedResult> SearchRegistrationsAsync(RegistrationPagedQueryDto query)
        {
            try
            {
                var response = await _registrationApiService.GetPagedRegistrationsAsync(query);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var registrationInfos = response.Content.Items.Select(ConvertToRegistrationInfo).ToList();
                    return new PagedResult
                    {
                        Items = registrationInfos,
                        TotalCount = response.Content.TotalCount,
                        CurrentPage = response.Content.CurrentPage,
                        PageSize = response.Content.PageSize
                    };
                }
                return new PagedResult 
                { 
                    Items = new List<RegistrationInfo>(), 
                    TotalCount = 0,
                    CurrentPage = query.CurrentPage,
                    PageSize = query.PageSize,
                    ErrorMessage = "获取挂号记录失败"
                };
            }
            catch (Exception ex)
            {
                return new PagedResult
                {
                    Items = new List<RegistrationInfo>(),
                    TotalCount = 0,
                    CurrentPage = query.CurrentPage,
                    PageSize = query.PageSize,
                    ErrorMessage = $"搜索挂号记录失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取挂号列表
        /// </summary>
        public async Task<List<RegistrationInfo>> GetRegistrationsAsync()
        {
            try
            {
                var response = await _registrationApiService.GetRegistrationsAsync();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return response.Content.Select(ConvertToRegistrationInfo).ToList();
                }
                return new List<RegistrationInfo>();
            }
            catch (Exception ex)
            {
                throw new Exception($"获取挂号列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取挂号详情
        /// </summary>
        public async Task<RegistrationInfo?> GetByIdAsync(Guid id)
        {
            try
            {
                var response = await _registrationApiService.GetRegistrationByIdAsync(id);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return ConvertDetailToRegistrationInfo(response.Content);
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"获取挂号详情失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 新增挂号
        /// </summary>
        public async Task<ApiResponse<object>> CreateRegistrationAsync(RegistrationCreateDto dto)
        {
            try
            {
                var response = await _registrationApiService.CreateRegistrationAsync(dto);
                return new ApiResponse<object>
                {
                    IsSuccess = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode ? "新增挂号成功" : response.Error?.Content ?? "新增挂号失败",
                    Data = response.Content
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"新增挂号失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 编辑挂号
        /// </summary>
        public async Task<ApiResponse<object>> UpdateRegistrationAsync(RegistrationEditDto dto)
        {
            try
            {
                var response = await _registrationApiService.UpdateRegistrationAsync(dto);
                return new ApiResponse<object>
                {
                    IsSuccess = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode ? "更新挂号成功" : response.Error?.Content ?? "更新挂号失败",
                    Data = response.Content
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"更新挂号失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 删除挂号
        /// </summary>
        public async Task<ApiResponse<object>> DeleteRegistrationAsync(Guid id)
        {
            try
            {
                var response = await _registrationApiService.DeleteRegistrationAsync(id);
                return new ApiResponse<object>
                {
                    IsSuccess = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode ? "删除挂号成功" : response.Error?.Content ?? "删除挂号失败",
                    Data = response.Content
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"删除挂号失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 取消挂号
        /// </summary>
        public async Task<ApiResponse<object>> CancelRegistrationAsync(Guid id)
        {
            try
            {
                var response = await _registrationApiService.CancelRegistrationAsync(id);
                return new ApiResponse<object>
                {
                    IsSuccess = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode ? "取消挂号成功" : response.Error?.Content ?? "取消挂号失败",
                    Data = response.Content
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"取消挂号失败: {ex.Message}"
                };
            }
        }


        /// <summary>
        /// 获取医生可预约时间段
        /// </summary>
        public async Task<List<TimeSlotInfo>> GetAvailableSlotsAsync(Guid doctorId, DateTime date)
        {
            try
            {
                var response = await _registrationApiService.GetAvailableSlotsAsync(doctorId, date);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return response.Content.Select(ConvertToTimeSlotInfo).ToList();
                }
                return new List<TimeSlotInfo>();
            }
            catch (Exception ex)
            {
                throw new Exception($"获取可预约时间段失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 转换RegistrationDto到RegistrationInfo
        /// </summary>
        private RegistrationInfo ConvertToRegistrationInfo(RegistrationDto dto)
        {
            return new RegistrationInfo
            {
                Id = dto.Id,
                RegistrationNumber = dto.RegistrationNumber ?? "",
                PatientId = Guid.TryParse(dto.PatientId, out var patientId) ? patientId : Guid.Empty,
                PatientName = dto.PatientName,
                PatientPhone = dto.PatientPhone ?? "",
                DoctorId = Guid.TryParse(dto.DoctorId, out var doctorId) ? doctorId : Guid.Empty,
                DoctorName = dto.DoctorName,
                Department = dto.Department ?? "",
                RegistrationType = ParseRegistrationType(dto.RegistrationType),
                RegistrationFee = dto.RegistrationFee,
                RegistrationTime = dto.RegistrationTime,
                AppointmentDate = dto.AppointmentDate,
                AppointmentTimeSlot = dto.AppointmentTimeSlot,
                Status = ParseRegistrationStatus(dto.Status),
                QueueNumber = dto.QueueNumber,
                IsPaid = dto.IsPaid,
                Remark = dto.Remark,
                CreateTime = dto.CreateTime,
                UpdateTime = dto.UpdateTime
            };
        }

        /// <summary>
        /// 转换RegistrationDetailDto到RegistrationInfo
        /// </summary>
        private RegistrationInfo ConvertDetailToRegistrationInfo(RegistrationDetailDto dto)
        {
            return new RegistrationInfo
            {
                Id = dto.Id,
                RegistrationNumber = dto.RegistrationNumber ?? "",
                PatientId = Guid.TryParse(dto.PatientId, out var patientId) ? patientId : Guid.Empty,
                PatientName = dto.PatientName,
                PatientPhone = dto.PatientPhone ?? "",
                DoctorId = Guid.TryParse(dto.DoctorId, out var doctorId) ? doctorId : Guid.Empty,
                DoctorName = dto.DoctorName ?? "",
                Department = dto.Department ?? "",
                RegistrationType = ParseRegistrationType(dto.RegistrationType),
                RegistrationFee = dto.RegistrationFee,
                RegistrationTime = dto.RegistrationTime,
                AppointmentDate = dto.AppointmentDate,
                AppointmentTimeSlot = dto.AppointmentTimeSlot,
                Status = ParseRegistrationStatus(dto.Status),
                QueueNumber = dto.QueueNumber,
                IsPaid = dto.IsPaid,
                Remark = dto.Remark,
                CreateTime = dto.CreateTime,
                UpdateTime = dto.UpdateTime
            };
        }


        /// <summary>
        /// 转换TimeSlotDto到TimeSlotInfo
        /// </summary>
        private TimeSlotInfo ConvertToTimeSlotInfo(TimeSlotDto dto)
        {
            return new TimeSlotInfo
            {
                Id = dto.Id,
                StartTime = TimeSpan.TryParse(dto.StartTime, out var startTime) ? startTime : TimeSpan.Zero,
                EndTime = TimeSpan.TryParse(dto.EndTime, out var endTime) ? endTime : TimeSpan.Zero,
                MaxCount = dto.MaxCount,
                BookedCount = dto.BookedCount
            };
        }

        /// <summary>
        /// 解析挂号类型
        /// </summary>
        private RegistrationType ParseRegistrationType(string typeStr)
        {
            return typeStr switch
            {
                "1" or "Regular" or "普通号" => RegistrationType.Regular,
                "2" or "Expert" or "专家号" => RegistrationType.Expert,
                "3" or "Emergency" or "急诊号" => RegistrationType.Emergency,
                "4" or "Appointment" or "预约号" => RegistrationType.Appointment,
                _ => RegistrationType.Regular
            };
        }

        /// <summary>
        /// 解析挂号状态
        /// </summary>
        private RegistrationStatus ParseRegistrationStatus(string statusStr)
        {
            return statusStr switch
            {
                "0" or "Scheduled" or "已预约" => RegistrationStatus.Scheduled,
                "1" or "Arrived" or "已到达" => RegistrationStatus.Arrived,
                "2" or "InConsultation" or "就诊中" => RegistrationStatus.InConsultation,
                "3" or "Completed" or "已完成" => RegistrationStatus.Completed,
                "-1" or "Cancelled" or "已取消" => RegistrationStatus.Cancelled,
                "-2" or "NoShow" or "爽约" => RegistrationStatus.NoShow,
                "-3" or "Expired" or "已过期" => RegistrationStatus.Expired,
                _ => RegistrationStatus.Scheduled
            };
        }

        /// <summary>
        /// 分页获取挂号记录
        /// </summary>
        public async Task<LYBT.WPF.Client.Core.Models.Common.PagedResult<RegistrationInfo>> GetPagedAsync(int page, int pageSize, string? searchKeyword = null, DateTime? startDate = null, DateTime? endDate = null, string? status = null, string? registrationType = null)
        {
            var query = new RegistrationPagedQueryDto
            {
                CurrentPage = page,
                PageSize = pageSize,
                SearchKeyword = searchKeyword,
                StartDate = startDate,
                EndDate = endDate,
                Status = status != null ? Enum.TryParse<RegistrationStatus>(status, out var s) ? s : (RegistrationStatus?)null : null,
                RegistrationType = registrationType != null ? Enum.TryParse<RegistrationType>(registrationType, out var rt) ? rt : (RegistrationType?)null : null
            };
            
            var result = await SearchRegistrationsAsync(query);
            return result;
        }

        /// <summary>
        /// 创建挂号
        /// </summary>
        public async Task<ApiResponse<object>> CreateAsync(RegistrationCreateDto dto)
        {
            return await CreateRegistrationAsync(dto);
        }

        /// <summary>
        /// 更新挂号
        /// </summary>
        public async Task<ApiResponse<object>> UpdateAsync(RegistrationEditDto dto)
        {
            return await UpdateRegistrationAsync(dto);
        }

        /// <summary>
        /// 取消挂号
        /// </summary>
        public async Task<ApiResponse<object>> CancelAsync(Guid id)
        {
            return await CancelRegistrationAsync(id);
        }

        /// <summary>
        /// 批量取消挂号
        /// </summary>
        public async Task<ApiResponse<object>> BatchCancelAsync(List<Guid> ids)
        {
            try
            {
                // 模拟批量取消
                await Task.Delay(300);
                return new ApiResponse<object>
                {
                    IsSuccess = true,
                    Message = $"成功取消 {ids.Count} 个挂号"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"批量取消失败: {ex.Message}"
                };
            }
        }
    }
}