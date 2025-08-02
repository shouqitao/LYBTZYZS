using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Services;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Records;
using LYBT.WPF.Client.Core.Models.DTOs;
using LYBT.WPF.Client.Core.Interfaces.Services;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 病历服务实现
    /// </summary>
    public class RecordService : IRecordService
    {
        private readonly IApiService _apiService;
        private readonly IRecordApiService _recordApiService;

        public RecordService(IApiService apiService, IRecordApiService recordApiService)
        {
            _apiService = apiService;
            _recordApiService = recordApiService;
        }

        /// <summary>
        /// 获取病历列表
        /// </summary>
        public async Task<ApiResponse<List<LYBT.WPF.Client.Core.Models.DTOs.RecordDto>>> GetListAsync()
        {
            try
            {
                var response = await _recordApiService.GetRecordsAsync();
                if (response.IsSuccess && response.Data != null)
                {
                    var result = new List<LYBT.WPF.Client.Core.Models.DTOs.RecordDto>();
                    foreach (var item in response.Data)
                    {
                        result.Add(ConvertToRecordDto(item));
                    }
                    return new ApiResponse<List<LYBT.WPF.Client.Core.Models.DTOs.RecordDto>>
                    {
                        IsSuccess = true,
                        Data = result,
                        Message = response.Message
                    };
                }
                return new ApiResponse<List<LYBT.WPF.Client.Core.Models.DTOs.RecordDto>>
                {
                    IsSuccess = false,
                    Message = response.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<LYBT.WPF.Client.Core.Models.DTOs.RecordDto>>
                {
                    IsSuccess = false,
                    Message = $"获取病历列表失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 根据患者ID获取病历列表
        /// </summary>
        public async Task<ApiResponse<List<LYBT.WPF.Client.Core.Models.DTOs.RecordDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                var response = await _recordApiService.GetPatientRecordsAsync(patientId);
                if (response.IsSuccess && response.Data != null)
                {
                    var result = new List<LYBT.WPF.Client.Core.Models.DTOs.RecordDto>();
                    foreach (var item in response.Data)
                    {
                        result.Add(ConvertToRecordDto(item));
                    }
                    return new ApiResponse<List<LYBT.WPF.Client.Core.Models.DTOs.RecordDto>>
                    {
                        IsSuccess = true,
                        Data = result,
                        Message = response.Message
                    };
                }
                return new ApiResponse<List<LYBT.WPF.Client.Core.Models.DTOs.RecordDto>>
                {
                    IsSuccess = false,
                    Message = response.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<LYBT.WPF.Client.Core.Models.DTOs.RecordDto>>
                {
                    IsSuccess = false,
                    Message = $"获取患者病历失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取病历详情
        /// </summary>
        public async Task<ApiResponse<LYBT.WPF.Client.Core.Models.DTOs.RecordDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var response = await _recordApiService.GetRecordByIdAsync(id);
                if (response.IsSuccess && response.Data != null)
                {
                    return new ApiResponse<LYBT.WPF.Client.Core.Models.DTOs.RecordDetailDto>
                    {
                        IsSuccess = true,
                        Data = ConvertToRecordDetailDto(response.Data),
                        Message = response.Message
                    };
                }
                return new ApiResponse<LYBT.WPF.Client.Core.Models.DTOs.RecordDetailDto>
                {
                    IsSuccess = false,
                    Message = response.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<LYBT.WPF.Client.Core.Models.DTOs.RecordDetailDto>
                {
                    IsSuccess = false,
                    Message = $"获取病历详情失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 新增病历
        /// </summary>
        public async Task<ApiResponse<object>> AddAsync(RecordCreateDto dto)
        {
            try
            {
                var createDto = new CreateRecordDto
                {
                    PatientId = dto.PatientId,
                    DoctorId = dto.DoctorId,
                    Department = dto.Department,
                    ChiefComplaint = dto.ChiefComplaint,
                    PresentIllness = dto.PresentIllness,
                    PastHistory = dto.PastHistory,
                    AllergyHistory = dto.AllergyHistory,
                    FamilyHistory = dto.FamilyHistory,
                    PersonalHistory = dto.PersonalHistory,
                    MenstrualHistory = dto.MenstrualHistory,
                    MaritalHistory = dto.MaritalHistory,
                    PhysicalExamination = dto.PhysicalExamination,
                    Inspection = dto.Inspection,
                    Auscultation = dto.Auscultation,
                    Inquiry = dto.Inquiry,
                    Palpation = dto.Palpation,
                    TongueExamination = dto.TongueExamination,
                    PulseExamination = dto.PulseExamination,
                    SyndromeDifferentiation = dto.SyndromeDifferentiation,
                    TreatmentPrinciple = dto.TreatmentPrinciple,
                    TCMDiagnosis = dto.TCMDiagnosis,
                    WesternDiagnosis = dto.WesternDiagnosis,
                    Treatment = dto.Treatment,
                    Remark = dto.Remark
                };
                
                var response = await _recordApiService.CreateRecordAsync(createDto);
                return new ApiResponse<object>
                {
                    IsSuccess = response.IsSuccess,
                    Message = response.Message,
                    Data = response.Data
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"创建病历失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 编辑病历
        /// </summary>
        public async Task<ApiResponse<object>> UpdateAsync(RecordEditDto dto)
        {
            try
            {
                var updateDto = new UpdateRecordDto
                {
                    Id = dto.Id,
                    PatientId = dto.PatientId,
                    DoctorId = dto.DoctorId,
                    Department = dto.Department,
                    ChiefComplaint = dto.ChiefComplaint,
                    PresentIllness = dto.PresentIllness,
                    PastHistory = dto.PastHistory,
                    AllergyHistory = dto.AllergyHistory,
                    FamilyHistory = dto.FamilyHistory,
                    PersonalHistory = dto.PersonalHistory,
                    MenstrualHistory = dto.MenstrualHistory,
                    MaritalHistory = dto.MaritalHistory,
                    PhysicalExamination = dto.PhysicalExamination,
                    Inspection = dto.Inspection,
                    Auscultation = dto.Auscultation,
                    Inquiry = dto.Inquiry,
                    Palpation = dto.Palpation,
                    TongueExamination = dto.TongueExamination,
                    PulseExamination = dto.PulseExamination,
                    SyndromeDifferentiation = dto.SyndromeDifferentiation,
                    TreatmentPrinciple = dto.TreatmentPrinciple,
                    TCMDiagnosis = dto.TCMDiagnosis,
                    WesternDiagnosis = dto.WesternDiagnosis,
                    Treatment = dto.Treatment,
                    Status = dto.Status,
                    Remark = dto.Remark
                };
                
                var response = await _recordApiService.UpdateRecordAsync(dto.Id, updateDto);
                return new ApiResponse<object>
                {
                    IsSuccess = response.IsSuccess,
                    Message = response.Message,
                    Data = response.Data
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"更新病历失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 删除病历
        /// </summary>
        public async Task<ApiResponse<object>> DeleteAsync(Guid id)
        {
            try
            {
                var response = await _recordApiService.DeleteRecordAsync(id);
                return new ApiResponse<object>
                {
                    IsSuccess = response.IsSuccess,
                    Message = response.Message,
                    Data = response.Data
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"删除病历失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 标记病历为共享
        /// </summary>
        public async Task<ApiResponse<object>> MarkAsSharedAsync(Guid id, List<string> doctorIds)
        {
            try
            {
                return await _apiService.PostAsync<object>($"record/share/{id}", doctorIds);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"共享病历失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 撤销病历共享
        /// </summary>
        public async Task<ApiResponse<object>> RevokeSharingAsync(Guid id)
        {
            try
            {
                return await _apiService.PostAsync<object>($"record/unshare/{id}", new object());
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"撤销病历共享失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取共享给当前医生的病历
        /// </summary>
        public async Task<ApiResponse<List<LYBT.WPF.Client.Core.Models.DTOs.RecordDto>>> GetSharedRecordsAsync(Guid doctorId)
        {
            try
            {
                var response = await _recordApiService.GetDoctorRecordsAsync(doctorId);
                if (response.IsSuccess && response.Data != null)
                {
                    var result = new List<LYBT.WPF.Client.Core.Models.DTOs.RecordDto>();
                    foreach (var item in response.Data)
                    {
                        result.Add(ConvertToRecordDto(item));
                    }
                    return new ApiResponse<List<LYBT.WPF.Client.Core.Models.DTOs.RecordDto>>
                    {
                        IsSuccess = true,
                        Data = result,
                        Message = response.Message
                    };
                }
                return new ApiResponse<List<LYBT.WPF.Client.Core.Models.DTOs.RecordDto>>
                {
                    IsSuccess = false,
                    Message = response.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<LYBT.WPF.Client.Core.Models.DTOs.RecordDto>>
                {
                    IsSuccess = false,
                    Message = $"获取共享病历失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取今日病例
        /// </summary>
        public async Task<ApiResponse<List<LYBT.Shared.Models.Records.RecordDto>>> GetTodayRecordsAsync()
        {
            try
            {
                return await _recordApiService.GetTodayRecordsAsync();
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<LYBT.Shared.Models.Records.RecordDto>>
                {
                    IsSuccess = false,
                    Message = $"获取今日病例失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 导出病例
        /// </summary>
        public async Task<ApiResponse<byte[]>> ExportRecordAsync(Guid id, string format = "pdf")
        {
            try
            {
                return await _recordApiService.ExportRecordAsync(id, format);
            }
            catch (Exception ex)
            {
                return new ApiResponse<byte[]>
                {
                    IsSuccess = false,
                    Message = $"导出病例失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取病例统计
        /// </summary>
        public async Task<ApiResponse<RecordStatisticsDto>> GetStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _recordApiService.GetStatisticsAsync(startDate, endDate);
            }
            catch (Exception ex)
            {
                return new ApiResponse<RecordStatisticsDto>
                {
                    IsSuccess = false,
                    Message = $"获取病例统计失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 转换RecordDto
        /// </summary>
        private LYBT.WPF.Client.Core.Models.DTOs.RecordDto ConvertToRecordDto(LYBT.Shared.Models.Records.RecordDto dto)
        {
            return new LYBT.WPF.Client.Core.Models.DTOs.RecordDto
            {
                Id = dto.Id,
                RecordNo = dto.RecordNo,
                PatientId = dto.PatientId,
                PatientName = dto.PatientName,
                PatientGender = dto.PatientGender,
                PatientAge = dto.PatientAge,
                DoctorId = dto.DoctorId,
                DoctorName = dto.DoctorName,
                Department = dto.Department,
                ChiefComplaint = dto.ChiefComplaint,
                PresentIllness = dto.PresentIllness,
                PastHistory = dto.PastHistory,
                AllergyHistory = dto.AllergyHistory,
                PhysicalExamination = dto.PhysicalExamination,
                TCMDiagnosis = dto.TCMDiagnosis,
                WesternDiagnosis = dto.WesternDiagnosis,
                Treatment = dto.Treatment,
                PrescriptionId = dto.PrescriptionId,
                VisitTime = dto.VisitTime,
                CreatedTime = dto.CreatedTime,
                UpdatedTime = dto.UpdatedTime,
                Status = dto.Status,
                Remark = dto.Remark
            };
        }

        /// <summary>
        /// 转换RecordDetailDto
        /// </summary>
        private LYBT.WPF.Client.Core.Models.DTOs.RecordDetailDto ConvertToRecordDetailDto(LYBT.Shared.Models.Records.RecordDetailDto dto)
        {
            var detail = new LYBT.WPF.Client.Core.Models.DTOs.RecordDetailDto
            {
                Id = dto.Id,
                RecordNo = dto.RecordNo,
                PatientId = dto.PatientId,
                PatientName = dto.PatientName,
                PatientGender = dto.PatientGender,
                PatientAge = dto.PatientAge,
                DoctorId = dto.DoctorId,
                DoctorName = dto.DoctorName,
                Department = dto.Department,
                ChiefComplaint = dto.ChiefComplaint,
                PresentIllness = dto.PresentIllness,
                PastHistory = dto.PastHistory,
                AllergyHistory = dto.AllergyHistory,
                PhysicalExamination = dto.PhysicalExamination,
                TCMDiagnosis = dto.TCMDiagnosis,
                WesternDiagnosis = dto.WesternDiagnosis,
                Treatment = dto.Treatment,
                PrescriptionId = dto.PrescriptionId,
                VisitTime = dto.VisitTime,
                CreatedTime = dto.CreatedTime,
                UpdatedTime = dto.UpdatedTime,
                Status = dto.Status,
                Remark = dto.Remark,
                FamilyHistory = dto.FamilyHistory,
                PersonalHistory = dto.PersonalHistory,
                MenstrualHistory = dto.MenstrualHistory,
                MaritalHistory = dto.MaritalHistory,
                Inspection = dto.Inspection,
                Auscultation = dto.Auscultation,
                Inquiry = dto.Inquiry,
                Palpation = dto.Palpation,
                TongueExamination = dto.TongueExamination,
                PulseExamination = dto.PulseExamination,
                SyndromeDifferentiation = dto.SyndromeDifferentiation,
                TreatmentPrinciple = dto.TreatmentPrinciple
            };

            // 转换辅助检查
            foreach (var exam in dto.AuxiliaryExaminations)
            {
                detail.AuxiliaryExaminations.Add(new LYBT.WPF.Client.Core.Models.DTOs.AuxiliaryExamination
                {
                    ExaminationItem = exam.ExaminationItem,
                    Result = exam.Result,
                    ExaminationDate = exam.ExaminationDate
                });
            }

            // 转换附件
            foreach (var attachment in dto.Attachments)
            {
                detail.Attachments.Add(new LYBT.WPF.Client.Core.Models.DTOs.RecordAttachment
                {
                    Id = attachment.Id,
                    FileName = attachment.FileName,
                    FilePath = attachment.FilePath,
                    FileType = attachment.FileType,
                    FileSize = attachment.FileSize,
                    UploadTime = attachment.UploadTime
                });
            }

            // 转换随访记录
            foreach (var followUp in dto.FollowUps)
            {
                detail.FollowUps.Add(new LYBT.WPF.Client.Core.Models.DTOs.FollowUpRecord
                {
                    Id = followUp.Id,
                    FollowUpTime = followUp.FollowUpTime,
                    Content = followUp.Content,
                    DoctorName = followUp.DoctorName
                });
            }

            return detail;
        }
    }
}