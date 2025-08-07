using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.WPF.Client.Core.Services;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Services
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
        public async Task<PagedResult<PrescriptionDto>> GetPagedAsync(PaginationRequest request)
        {
            try
            {
                var response = await _prescriptionApiService.GetListAsync(
                    page: request.CurrentPage,
                    pageSize: request.PageSize,
                    keyword: request.SearchKeyword
                );

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return new PagedResult<PrescriptionDto>
                    {
                        Items = response.Content.Items,
                        TotalCount = response.Content.TotalCount,
                        CurrentPage = response.Content.CurrentPage,
                        PageSize = response.Content.PageSize
                    };
                }

                return new PagedResult<PrescriptionDto>
                {
                    Items = new List<PrescriptionDto>(),
                    TotalCount = 0,
                    CurrentPage = request.CurrentPage,
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
                    CurrentPage = request.CurrentPage,
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
                await _prescriptionApiService.GetByIdAsync(id)
            );
        }

        /// <summary>
        /// 创建处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _prescriptionApiService.CreatePrescriptionAsync(dto)
            );
        }

        /// <summary>
        /// 更新处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(PrescriptionEditDto dto)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _prescriptionApiService.UpdatePrescriptionAsync(dto.Id, dto)
            );
        }

        /// <summary>
        /// 删除处方
        /// </summary>
        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            var response = await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _prescriptionApiService.DeletePrescriptionAsync(id)
            );
            
            return response.IsSuccess 
                ? ServiceResult.Success() 
                : ServiceResult.Failure(response.ErrorMessage ?? "删除处方失败");
        }

        /// <summary>
        /// 作废处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CancelAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _prescriptionApiService.CancelPrescriptionAsync(id)
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
                return ServiceResult<List<PrescriptionDto>>.Failure($"获取患者处方失败: {ex.Message}", ex);
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
                return ServiceResult<List<PrescriptionDto>>.Failure($"获取医生处方失败: {ex.Message}", ex);
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
                return ServiceResult<List<PrescriptionDto>>.Failure($"获取今日处方失败: {ex.Message}", ex);
            }
        }
    }
}