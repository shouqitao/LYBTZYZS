using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Patients.Services
{
    /// <summary>
    /// 患者查询服务实现
    /// </summary>
    public class PatientQueryService : IPatientQueryService
    {
        private readonly IPatientRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<PatientQueryService> _logger;

        public PatientQueryService(
            IPatientRepository repository,
            IMapper mapper,
            ILogger<PatientQueryService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResult<PatientDto>> GetPagedPatientsAsync(PatientSearchDto searchDto)
        {
            _logger.LogDebug("查询患者列表");
            // TODO: 实现分页查询逻辑
            var result = new PagedResult<PatientDto>
            {
                Items = new List<PatientDto>(),
                TotalCount = 0,
                CurrentPage = searchDto.PageIndex,
                PageSize = searchDto.PageSize
            };
            return await Task.FromResult(result);
        }

        public async Task<PatientDto?> GetPatientByIdAsync(Guid patientId)
        {
            _logger.LogDebug("查询患者详情: {Id}", patientId);
            var patient = await _repository.GetByIdAsync(patientId);
            return patient != null ? _mapper.Map<PatientDto>(patient) : null;
        }
    }
}