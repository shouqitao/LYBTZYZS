using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Herbs.Services
{
    /// <summary>
    /// 中草药查询服务实现
    /// </summary>
    public class HerbQueryService : IHerbQueryService
    {
        private readonly IHerbRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<HerbQueryService> _logger;

        public HerbQueryService(
            IHerbRepository repository,
            IMapper mapper,
            ILogger<HerbQueryService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResult<HerbDto>> GetPagedHerbsAsync(HerbSearchDto searchDto)
        {
            _logger.LogDebug("查询中药列表");
            // TODO: 实现分页查询逻辑
            var result = new PagedResult<HerbDto>
            {
                Items = new List<HerbDto>(),
                TotalCount = 0,
                CurrentPage = searchDto.PageIndex,
                PageSize = searchDto.PageSize
            };
            return await Task.FromResult(result);
        }

        public async Task<HerbDto?> GetHerbByIdAsync(Guid herbId)
        {
            _logger.LogDebug("查询中药详情: {Id}", herbId);
            var herb = await _repository.GetByIdAsync(herbId);
            return herb != null ? _mapper.Map<HerbDto>(herb) : null;
        }
    }
}