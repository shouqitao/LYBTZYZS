using System.Linq;
using AutoMapper;
using PrescriptionEntity = LYBT.Entities.Prescriptions.Prescription;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Services
{
    /// <summary>
    /// 处方服务 - 简化版，只包含基础CRUD
    /// </summary>
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IPrescriptionRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<PrescriptionService> _logger;

        public PrescriptionService(
            IPrescriptionRepository repository,
            IMapper mapper,
            ILogger<PrescriptionService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                // 使用优化后的查询方法，包含Items集合
                var pagedResult = await _repository.GetPagedWithDetailsAsync(page, pageSize, keyword);
                var dto = new PagedResult<PrescriptionDto>
                {
                    Items = _mapper.Map<List<PrescriptionDto>>(pagedResult.Items),
                    TotalCount = pagedResult.TotalCount,
                    CurrentPage = pagedResult.CurrentPage,
                    PageSize = pagedResult.PageSize
                };
                return ServiceResult<PagedResult<PrescriptionDto>>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方列表失败");
                return ServiceResult<PagedResult<PrescriptionDto>>.Failure("获取处方列表失败");
            }
        }

        public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
        {
            try
            {
                // 使用优化后的查询方法，包含处方项
                var entity = await _repository.GetByIdWithItemsAsync(id);
                if (entity == null)
                    return ServiceResult<PrescriptionDto>.Failure("处方不存在");

                var dto = _mapper.Map<PrescriptionDto>(entity);
                return ServiceResult<PrescriptionDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方详情失败");
                return ServiceResult<PrescriptionDto>.Failure("获取处方详情失败");
            }
        }

        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto)
        {
            try
            {
                var entity = _mapper.Map<PrescriptionEntity>(dto);
                var result = await _repository.AddAsync(entity);
                var resultDto = _mapper.Map<PrescriptionDto>(result);
                return ServiceResult<PrescriptionDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建处方失败");
                return ServiceResult<PrescriptionDto>.Failure("创建处方失败");
            }
        }

        public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionUpdateDto dto)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<PrescriptionDto>.Failure("处方不存在");

                _mapper.Map(dto, entity);
                var result = await _repository.UpdateAsync(entity);
                var resultDto = _mapper.Map<PrescriptionDto>(result);
                return ServiceResult<PrescriptionDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新处方失败");
                return ServiceResult<PrescriptionDto>.Failure("更新处方失败");
            }
        }

    public async Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
    {
        try
        {
            // 使用优化后的查询方法，直接查询并包含Items集合
            var prescriptions = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);
            
            // 转换为DTO
            var prescriptionDtos = _mapper.Map<List<PrescriptionDto>>(prescriptions);
            
            return ServiceResult<List<PrescriptionDto>>.Success(prescriptionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取病历相关处方时发生错误，病历ID：{MedicalCaseId}", medicalCaseId);
            return ServiceResult<List<PrescriptionDto>>.Failure($"获取病历相关处方失败：{ex.Message}");
        }
    }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                var result = await _repository.DeleteAsync(id);
                return result ? ServiceResult.Success() : ServiceResult.Failure("删除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除处方失败");
                return ServiceResult.Failure("删除处方失败");
            }
        }
    }
}