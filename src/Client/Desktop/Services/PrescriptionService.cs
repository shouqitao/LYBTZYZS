using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models;
// using LYBT.Desktop.Core.Models.Common; // 已迁移到 LYBT.Shared.Models.Contracts.Common
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 处方服务实现
    /// </summary>
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IPrescriptionApiService _prescriptionApiService;

        public PrescriptionService(IPrescriptionApiService prescriptionApiService)
        {
            _prescriptionApiService = prescriptionApiService;
        }

        /// <summary>
        /// 分页查询处方
        /// </summary>
        public async Task<PagedResult<PrescriptionDto>> GetPagedAsync(PagedQueryBaseDto request)
        {
            try
            {
                var response = await _prescriptionApiService.GetListAsync(
                    page: request.PageIndex,
                    pageSize: request.PageSize,
                    keyword: request.Keyword
                );

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return new PagedResult<PrescriptionDto>
                    {
                        Items = response.Content.Items.ToList(),
                        TotalCount = (int)response.Content.TotalCount,
                        CurrentPage = response.Content.CurrentPage,
                        PageSize = response.Content.PageSize
                    };
                }

                return new PagedResult<PrescriptionDto>
                {
                    Items = new List<PrescriptionDto>(),
                    TotalCount = 0,
                    CurrentPage = request.PageIndex,
                    PageSize = request.PageSize,
                    ErrorMessage = "获取处方列表失败"
                };
            }
            catch (Exception ex)
            {
                return new PagedResult<PrescriptionDto>
                {
                    Items = new List<PrescriptionDto>(),
                    TotalCount = 0,
                    CurrentPage = request.PageIndex,
                    PageSize = request.PageSize,
                    ErrorMessage = $"查询处方失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取处方详情
        /// </summary>
        public async Task<ServiceResult<PrescriptionDetailDto>> GetByIdAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _prescriptionApiService.GetByIdAsync(id),
                operationName: "获取处方详情",
                maxRetries: 2
            );
        }

        /// <summary>
        /// 创建处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _prescriptionApiService.CreatePrescriptionAsync(dto),
                operationName: "创建处方",
                maxRetries: 1
            );
        }

        /// <summary>
        /// 更新处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(PrescriptionEditDto dto)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _prescriptionApiService.UpdatePrescriptionAsync(dto.Id, dto),
                operationName: "更新处方",
                maxRetries: 1
            );
        }

        /// <summary>
        /// 删除处方
        /// </summary>
        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () =>
                await _prescriptionApiService.DeletePrescriptionAsync(id),
                operationName: "删除处方",
                maxRetries: 1
            );
        }

        /// <summary>
        /// 作废处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CancelAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _prescriptionApiService.CancelPrescriptionAsync(id),
                operationName: "作废处方",
                maxRetries: 1
            );
        }

        /// <summary>
        /// 根据患者ID获取处方列表
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                // 使用查询接口，通过患者姓名筛选
                var response = await _prescriptionApiService.GetListAsync(
                    page: 1,
                    pageSize: 100,
                    patientName: patientId.ToString() // 这里需要后端支持按患者ID查询
                );

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return ServiceResult<List<PrescriptionDto>>.Success(response.Content.Items.ToList());
                }

                return ServiceResult<List<PrescriptionDto>>.Failure("获取患者处方列表失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<PrescriptionDto>>.Failure($"获取患者处方失败: {ex.Message}", null, ex);
            }
        }

        /// <summary>
        /// 根据医生ID获取处方列表
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetByDoctorIdAsync(Guid doctorId)
        {
            try
            {
                // 使用查询接口，通过医生姓名筛选
                var response = await _prescriptionApiService.GetListAsync(
                    page: 1,
                    pageSize: 100,
                    doctorName: doctorId.ToString() // 这里需要后端支持按医生ID查询
                );

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return ServiceResult<List<PrescriptionDto>>.Success(response.Content.Items.ToList());
                }

                return ServiceResult<List<PrescriptionDto>>.Failure("获取医生处方列表失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<PrescriptionDto>>.Failure($"获取医生处方失败: {ex.Message}", null, ex);
            }
        }

        /// <summary>
        /// 获取今日处方列表
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetTodayPrescriptionsAsync()
        {
            try
            {
                var today = DateTime.Today;
                var response = await _prescriptionApiService.GetListAsync(
                    page: 1,
                    pageSize: 1000,
                    startDate: today,
                    endDate: today.AddDays(1).AddSeconds(-1)
                );

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return ServiceResult<List<PrescriptionDto>>.Success(response.Content.Items.ToList());
                }

                return ServiceResult<List<PrescriptionDto>>.Failure("获取今日处方列表失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<PrescriptionDto>>.Failure($"获取今日处方失败: {ex.Message}", null, ex);
            }
        }

        /// <summary>
        /// 根据医疗案例ID获取处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDetailDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                // 使用查询接口，按医疗案例ID查找处方
                var response = await _prescriptionApiService.GetListAsync(
                    page: 1,
                    pageSize: 1
                );

                // TODO: 这里需要后端支持按医疗案例ID查询的API接口
                // 目前的API可能不支持按medicalCaseId查询，需要后端扩展

                if (response.IsSuccessStatusCode && response.Content != null && response.Content.Items.Any())
                {
                    var prescription = response.Content.Items.First();
                    
                    // 将PrescriptionDto转换为PrescriptionDetailDto
                    var detailDto = new PrescriptionDetailDto
                    {
                        Id = prescription.Id,
                        PatientId = prescription.PatientId,
                        PatientName = prescription.PatientName,
                        DoctorId = prescription.DoctorId,
                        DoctorName = prescription.DoctorName,
                        Diagnosis = prescription.Diagnosis ?? "",
                        DosageCount = prescription.DosageCount,
                        Status = prescription.Status,
                        TotalPrice = prescription.TotalPrice,
                        CreateTime = prescription.CreateTime,
                        Items = prescription.Items,
                        Usage = "",
                        Remark = ""
                    };

                    return ServiceResult<PrescriptionDetailDto>.Success(detailDto);
                }

                return ServiceResult<PrescriptionDetailDto>.Failure("未找到对应的处方记录");
            }
            catch (Exception ex)
            {
                return ServiceResult<PrescriptionDetailDto>.Failure($"根据医疗案例ID获取处方失败: {ex.Message}", null, ex);
            }
        }

        /// <summary>
        /// 创建或更新处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CreateOrUpdateAsync(PrescriptionCreateDto dto)
        {
            try
            {
                // 简单实现：始终创建新处方
                // 实际应用中可能需要检查是否存在同一医疗案例的处方，然后决定创建或更新
                return await CreateAsync(dto);
            }
            catch (Exception ex)
            {
                return ServiceResult<PrescriptionDto>.Failure($"创建或更新处方失败: {ex.Message}", null, ex);
            }
        }

        /// <summary>
        /// 批量获取处方详情（性能优化）
        /// </summary>
        public async Task<List<PrescriptionDto>> GetBatchAsync(IEnumerable<Guid> ids)
        {
            var idList = ids.ToList();
            if (!idList.Any())
                return new List<PrescriptionDto>();

            try
            {
                // TODO: 等待后端实现真正的批量API
                // 目前使用优化的并发获取方式
                var tasks = idList.Select(async id =>
                {
                    try
                    {
                        var result = await GetByIdAsync(id);
                        if (result.IsSuccess && result.Data != null)
                        {
                            // 将PrescriptionDetailDto转换为PrescriptionDto
                            return new PrescriptionDto
                            {
                                Id = result.Data.Id,
                                PatientId = result.Data.PatientId,
                                PatientName = result.Data.PatientName,
                                DoctorId = result.Data.DoctorId,
                                DoctorName = result.Data.DoctorName,
                                Diagnosis = result.Data.Diagnosis,
                                DosageCount = result.Data.DosageCount,
                                Status = result.Data.Status,
                                TotalPrice = result.Data.TotalPrice,
                                CreateTime = result.Data.CreateTime,
                                Items = result.Data.Items
                            };
                        }
                        return null;
                    }
                    catch
                    {
                        return null;
                    }
                }).ToArray();

                var results = await Task.WhenAll(tasks);
                return results.Where(r => r != null).ToList()!;
            }
            catch (Exception ex)
            {
                // 记录错误，返回空列表
                return new List<PrescriptionDto>();
            }
        }

        /// <summary>
        /// 批量更新处方状态（性能优化）
        /// </summary>
        public async Task<ServiceResult<int>> UpdateBatchStatusAsync(IEnumerable<Guid> ids, int status, string? reason = null)
        {
            var idList = ids.ToList();
            if (!idList.Any())
                return ServiceResult<int>.Success(0);

            try
            {
                int successCount = 0;
                var tasks = idList.Select(async id =>
                {
                    try
                    {
                        // 目前只支持取消状态（status = 2）
                        if (status == 2) // 假设2表示取消状态
                        {
                            var result = await CancelAsync(id);
                            return result.IsSuccess;
                        }
                        return false;
                    }
                    catch
                    {
                        return false;
                    }
                }).ToArray();

                var results = await Task.WhenAll(tasks);
                successCount = results.Count(r => r);

                return ServiceResult<int>.Success(successCount);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Failure($"批量更新处方状态失败: {ex.Message}", null, ex);
            }
        }
    }
}