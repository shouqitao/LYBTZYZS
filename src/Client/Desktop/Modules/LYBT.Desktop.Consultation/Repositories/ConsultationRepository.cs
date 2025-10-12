using LYBT.Desktop.Consultation.Interfaces;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Consultation.Repositories
{
    /// <summary>
    /// 诊疗数据仓储实现 - ADR-002合规版本
    /// 直接调用IConsultationApi（Refit HTTP客户端），符合架构决策
    /// </summary>
    public class ConsultationRepository : IConsultationRepository
    {
        private readonly IConsultationApi _consultationApi;
        private readonly ILogger<ConsultationRepository> _logger;

        public ConsultationRepository(
            IConsultationApi consultationApi,
            ILogger<ConsultationRepository> logger)
        {
            _consultationApi = consultationApi;
            _logger = logger;
        }

        /// <summary>
        /// 根据ID获取诊疗记录详情
        /// </summary>
        public async Task<ConsultationDto> GetByIdAsync(Guid id)
        {
            try
            {
                var response = await _consultationApi.GetConsultationByIdAsync(id);
                return response.Content ?? throw new InvalidOperationException($"诊疗记录 {id} 不存在");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取诊疗记录详情失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 创建新诊疗记录（使用CreateDto）
        /// </summary>
        public async Task<ConsultationDto> CreateAsync(ConsultationCreateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            try
            {
                var response = await _consultationApi.CreateConsultationAsync(dto);
                return response.Content ?? throw new InvalidOperationException("创建诊疗记录失败，服务器未返回数据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建诊疗记录失败");
                throw;
            }
        }

        /// <summary>
        /// 更新诊疗记录信息（使用UpdateDto）
        /// </summary>
        public async Task<ConsultationDto> UpdateAsync(ConsultationUpdateDto dto)
        {
            if (dto?.Id == null || dto.Id == Guid.Empty)
            {
                _logger.LogError("Cannot update consultation with null or invalid id");
                throw new ArgumentException("Consultation ID is required", nameof(dto));
            }

            try
            {
                var response = await _consultationApi.UpdateConsultationAsync(dto.Id, dto);
                return response.Content ?? throw new InvalidOperationException($"更新诊疗记录失败，ID: {dto.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新诊疗记录失败，ID: {Id}", dto.Id);
                throw;
            }
        }

        /// <summary>
        /// 删除诊疗记录（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var response = await _consultationApi.DeleteConsultationAsync(id);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除诊疗记录失败，ID: {Id}", id);
                return false;
            }
        }

        /// <summary>
        /// 搜索诊疗记录（关键字查询）
        /// </summary>
        public async Task<List<ConsultationDto>> SearchAsync(string keyword)
        {
            try
            {
                var response = await _consultationApi.GetConsultationsAsync(page: 1, pageSize: 1000, keyword: keyword);
                return response.Content?.Items ?? new List<ConsultationDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索诊疗记录失败，关键字: {Keyword}", keyword);
                throw;
            }
        }

        /// <summary>
        /// 分页查询诊疗记录列表（服务端分页）
        /// </summary>
        public async Task<PagedResult<ConsultationDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                var response = await _consultationApi.GetConsultationsAsync(page, pageSize, keyword);
                return response.Content ?? new PagedResult<ConsultationDto>
                {
                    Items = new List<ConsultationDto>(),
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询诊疗记录失败，Page: {Page}, PageSize: {PageSize}, Keyword: {Keyword}",
                    page, pageSize, keyword);
                throw;
            }
        }

        /// <summary>
        /// 根据医案ID获取诊疗记录列表
        /// </summary>
        public async Task<List<ConsultationDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                var response = await _consultationApi.GetConsultationsByMedicalCaseIdAsync(medicalCaseId);
                return response.Content ?? new List<ConsultationDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医案诊疗记录失败: {MedicalCaseId}", medicalCaseId);
                return new List<ConsultationDto>();
            }
        }

        /// <summary>
        /// 启动诊疗（创建新诊疗记录并关联患者）
        /// </summary>
        public async Task<ConsultationDto> StartAsync(Guid patientId)
        {
            try
            {
                var response = await _consultationApi.StartConsultationAsync(new { PatientId = patientId });
                return response.Content ?? throw new InvalidOperationException("启动诊疗失败，服务器未返回数据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动诊疗失败，PatientId: {PatientId}", patientId);
                throw;
            }
        }
    }
}
