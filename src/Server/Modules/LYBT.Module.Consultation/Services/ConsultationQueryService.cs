using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultation.Services
{
    /// <summary>
    /// 诊疗查询服务实现
    /// </summary>
    public class ConsultationQueryService : IConsultationQueryService
    {
        private readonly IConsultationRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationQueryService> _logger;

        public ConsultationQueryService(
            IConsultationRepository repository,
            IMapper mapper,
            ILogger<ConsultationQueryService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResult<ConsultationDto>> GetPagedConsultationsAsync(ConsultationSearchDto searchDto)
        {
            _logger.LogDebug("查询诊疗记录列表");
            // TODO: 实现分页查询逻辑
            var result = new PagedResult<ConsultationDto>
            {
                Items = new List<ConsultationDto>(),
                TotalCount = 0,
                CurrentPage = searchDto.PageIndex,
                PageSize = searchDto.PageSize
            };
            return await Task.FromResult(result);
        }

        public async Task<ConsultationDto?> GetConsultationByIdAsync(Guid consultationId)
        {
            _logger.LogDebug("查询诊疗记录详情: {Id}", consultationId);
            var record = await _repository.GetByIdAsync(consultationId);
            return record != null ? _mapper.Map<ConsultationDto>(record) : null;
        }
    }
}