using System;
using LYBT.Shared.Models.Contracts.Common;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Utilities.Helpers;

namespace LYBT.Module.Patients.Services.Core
{
    /// <summary>
    /// 患者基础CRUD操作服务实现
    /// UltraThink重构：单一职责原则，只负责患者的基础增删改查操作
    /// 代码行数：约120行，符合500行以下标准
    /// </summary>
    public class PatientCrudService : IPatientCrudService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<PatientCrudService> _logger;
        private readonly PatientValidationService _validationService;

        public PatientCrudService(
            IPatientRepository patientRepository,
            IMapper mapper,
            ILogger<PatientCrudService> logger,
            PatientValidationService validationService)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        }

        /// <summary>
        /// 创建新患者档案
        /// </summary>
        public async Task<ServiceResult<PatientDto>> CreatePatientAsync(PatientCreateDto dto)
        {
            try
            {
                // 数据验证 - 转换为PatientDto进行验证
                var detailDto = _mapper.Map<PatientDto>(dto);
                await _validationService.ValidateForCreateAsync(detailDto);

                var model = _mapper.Map<Patient>(dto);
                model.Id = Guid.NewGuid();
                model.PinYinCode = CommonHelper.GetPinyinCode(model.Name);

                // 处理身份证信息
                _validationService.ProcessIdNumberInfo(model);

                var result = await _patientRepository.AddAsync(model);

                if (result != null)
                {
                    _logger.LogInformation("新增患者档案成功 {PatientName} ({PatientId})", result.Name, result.Id);
                    var patientDto = _mapper.Map<PatientDto>(result);
                    return ServiceResult<PatientDto>.Success(patientDto);
                }

                return ServiceResult<PatientDto>.Failure("新增患者档案失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "新增患者档案失败 {PatientName}", dto.Name);
                return ServiceResult<PatientDto>.Failure("新增患者档案失败");
            }
        }

        /// <summary>
        /// 更新患者信息
        /// </summary>
        public async Task<ServiceResult<PatientDto>> UpdatePatientAsync(Guid id, PatientUpdateDto dto)
        {
            try
            {
                var model = await _patientRepository.GetByIdAsync(id, true);
                if (model == null)
                    return ServiceResult<PatientDto>.Failure("患者不存在");

                // 数据验证 - 转换为PatientDto进行验证
                var detailDto = _mapper.Map<PatientDto>(dto);
                detailDto.Id = id;
                await _validationService.ValidateForUpdateAsync(id, detailDto);

                // 更新实体
                _mapper.Map(dto, model);
                model.PinYinCode = CommonHelper.GetPinyinCode(model.Name);
                
                // 处理身份证信息
                _validationService.ProcessIdNumberInfo(model);

                var result = await _patientRepository.UpdateAsync(model);
                if (result != null)
                {
                    _logger.LogInformation("更新患者档案成功 {PatientName} ({PatientId})", result.Name, result.Id);
                    var patientDto = _mapper.Map<PatientDto>(result);
                    return ServiceResult<PatientDto>.Success(patientDto);
                }
                
                return ServiceResult<PatientDto>.Failure("更新患者档案失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新患者档案失败 PatientId={PatientId}", id);
                return ServiceResult<PatientDto>.Failure("更新患者档案失败");
            }
        }

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        public async Task<ServiceResult<bool>> DeletePatientAsync(Guid id)
        {
            try
            {
                var model = await _patientRepository.GetByIdAsync(id, true);
                if (model == null)
                    return ServiceResult<bool>.Failure("患者不存在");

                var result = await _patientRepository.DisableAsync(id);
                if (result)
                {
                    _logger.LogInformation("患者删除成功 {PatientId}", id);
                    return ServiceResult<bool>.Success(true);
                }

                return ServiceResult<bool>.Failure("删除患者失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除患者失败 {PatientId}", id);
                return ServiceResult<bool>.Failure("删除患者失败");
            }
        }

        /// <summary>
        /// 删除患者（带操作者信息）
        /// </summary>
        public async Task<ServiceResult<bool>> DeletePatientAsync(Guid id, Guid operatorId, string operatorName)
        {
            try
            {
                var result = await _patientRepository.DisableAsync(id);
                if (result)
                {
                    _logger.LogInformation("患者删除(软删除) - 操作者 {OperatorName} ({OperatorId}), 患者ID: {PatientId}",
                        operatorName, operatorId, id);
                }
                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除患者失败 {PatientId}", id);
                return ServiceResult<bool>.Failure("删除患者失败");
            }
        }
    }
}