using LYBT.Desktop.Consultation.Interfaces;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Consultation.Repositories
{
    /// <summary>
    /// 诊疗数据仓储实现 - ADR-002合规版本
    /// 直接调用IConsultationApi（Refit HTTP客户端），符合架构决策
    /// </summary>
    public class ConsultationRepository : RepositoryBase<ConsultationDto, ConsultationCreateDto, ConsultationUpdateDto, IConsultationApi>, IConsultationRepository
    {

        public ConsultationRepository(
            IConsultationApi consultationApi,
            ILogger<ConsultationRepository> logger)
            : base(consultationApi, logger)
        {
        }

  

  

  

    

      

    

        /// <summary>
        /// 根据医案ID获取诊疗记录列表
        /// </summary>
        public async Task<List<ConsultationDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                var response = await _api.GetConsultationsByMedicalCaseIdAsync(medicalCaseId);
                return response.Data ?? new List<ConsultationDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医案诊疗记录失败: {MedicalCaseId}", medicalCaseId);
                return new List<ConsultationDto>();
            }
        }

        // Issue #1562 Phase 1: 已删除 StartAsync（工作流启动方法）

        #region RepositoryBase抽象方法实现

        protected override Task<ApiResponse<ConsultationDto>> CallApiGetByIdAsync(Guid id)
        {
            return _api.GetConsultationByIdAsync(id);
        }

        protected override Task<ApiResponse<PagedResult<ConsultationDto>>> CallApiGetPagedAsync(int page, int pageSize, string? keyword)
        {
            return _api.GetConsultationsAsync(page, pageSize, keyword);
        }

        protected override Task<ApiResponse<ConsultationDto>> CallApiCreateAsync(ConsultationCreateDto dto)
        {
            return _api.CreateConsultationAsync(dto);
        }

        protected override Task<ApiResponse<ConsultationDto>> CallApiUpdateAsync(Guid id, ConsultationUpdateDto dto)
        {
            return _api.UpdateConsultationAsync(id, dto);
        }

        protected override Task<ApiResponse<ApiResponse>> CallApiDeleteAsync(Guid id)
        {
            return _api.DeleteConsultationAsync(id);
        }

        protected override Guid? GetIdFromUpdateDto(ConsultationUpdateDto dto)
        {
            return dto?.Id;
        }

        #endregion
    }
}
