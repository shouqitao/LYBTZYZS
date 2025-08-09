using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Logging.Dtos;
using LYBT.Models.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;

namespace LYBT.Module.Patients.Services
{
    /// <summary>
    /// 患者核心服务实现（业务逻辑层）
    /// 只包含基础CRUD操作，其他功能已拆分到专门服务
    /// 实现软删除策略：患者档案只能禁用/启用，不能物理删除
    /// </summary>
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IMapper _mapper;
        private readonly IUnifiedLogService _logService;
        private readonly PatientValidationService _validationService;
        private readonly PatientArchiveService _archiveService;
        private readonly PatientStatisticsService _statisticsService;

        public PatientService(
            IPatientRepository patientRepository,
            IMapper mapper,
            IUnifiedLogService logService,
            PatientValidationService validationService,
            PatientArchiveService archiveService,
            PatientStatisticsService statisticsService)
        {
            _patientRepository = patientRepository;
            _mapper = mapper;
            _logService = logService;
            _validationService = validationService;
            _archiveService = archiveService;
            _statisticsService = statisticsService;
        }

        /// <summary>
        /// 新增患者档案，并记录操作日志
        /// </summary>
        public async Task<PatientDetailDto?> CreateAsync(PatientDetailDto dto, Guid operatorId, string operatorName)
        {
            // 数据验证
            await _validationService.ValidateForCreateAsync(dto);

            var model = _mapper.Map<PatientModel>(dto);
            model.Id = Guid.NewGuid();
            model.PinYinCode = CommonHelper.GetPinyinCode(model.Name);
            model.CreateTime = DateTime.Now;
            model.UpdateTime = DateTime.Now;

            // 处理身份证信息
            _validationService.ProcessIdNumberInfo(model);

            var result = await _patientRepository.AddAsync(model);

            if (result != null)
            {
                await LogPatientOperationAsync(operatorId, operatorName, LogActionType.Create,
                    $"新增患者档案：{result.Name}", JsonSerializer.Serialize(result));

                return _mapper.Map<PatientDetailDto>(result);
            }

            return null;
        }

        /// <summary>
        /// 更新患者信息
        /// </summary>
        public async Task<PatientDetailDto?> UpdateAsync(Guid id, PatientDetailDto dto, Guid operatorId, string operatorName)
        {
            var model = await _patientRepository.GetByIdAsync(id, true);
            if (model == null)
                throw new ArgumentException("患者不存在");

            // 数据验证
            await _validationService.ValidateForUpdateAsync(id, dto);

            var oldJson = JsonSerializer.Serialize(model);
            _mapper.Map(dto, model);
            model.PinYinCode = CommonHelper.GetPinyinCode(model.Name);
            model.UpdateTime = DateTime.Now;

            // 处理身份证信息
            _validationService.ProcessIdNumberInfo(model);

            var result = await _patientRepository.UpdateAsync(model);

            if (result != null)
            {
                await _logService.CreateLogAsync(new LogCreateDto
                {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = result.Id,
                    ActionType = ActionType.Edit,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    Content = $"编辑患者档案：{result.Name}",
                    OldValue = oldJson,
                    NewValue = JsonSerializer.Serialize(dto)
                });

                return _mapper.Map<PatientDetailDto>(result);
            }

            return null;
        }

        /// <summary>
        /// 根据患者ID获取患者详情
        /// </summary>
        public async Task<PatientDetailDto?> GetByIdAsync(Guid id)
        {
            bool includeDisabled = true;
            var model = await _patientRepository.GetByIdAsync(id, includeDisabled);
            return model == null ? null : _mapper.Map<PatientDetailDto>(model);
        }

        /// <summary>
        /// 获取所有患者列表
        /// </summary>
        public async Task<List<PatientDetailDto>> GetAllAsync()
        {
            var list = await _patientRepository.GetAllAsync();
            return list.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

        /// <summary>
        /// 分页查询患者
        /// </summary>
        public async Task<PaginatedResult<PatientDetailDto>> GetPagedAsync(PatientPagedQueryDto query)
        {
            // 使用BaseRepository的分页方法
            var pagedResult = await _patientRepository.GetPagedAsync(
                p => string.IsNullOrEmpty(query.Name) || p.Name.Contains(query.Name),
                query.CurrentPage, 
                query.PageSize,
                p => p.CreateTime,
                false  // 按创建时间降序排列
            );
            
            return new PaginatedResult<PatientDetailDto>
            {
                TotalCount = pagedResult.TotalCount,
                Items = pagedResult.Items.Select(_mapper.Map<PatientDetailDto>).ToList(),
                CurrentPage = query.CurrentPage,
                PageSize = query.PageSize
            };
        }

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName)
        {
            var result = await _patientRepository.DisableAsync(id);
            if (result)
            {
                await _logService.CreateLogAsync(new LogCreateDto
                {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = id,
                    ActionType = ActionType.Disable,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    Content = $"删除患者：{id}"
                });
            }
            return result;
        }

        /// <summary>
        /// 设置患者状态（启用/禁用）
        /// </summary>
        public async Task<bool> SetStatusAsync(Guid id, bool isActive, Guid operatorId, string operatorName)
        {
            bool result;
            string action;

            if (isActive)
            {
                result = await _patientRepository.EnableAsync(id);
                action = "启用";
            }
            else
            {
                result = await _patientRepository.DisableAsync(id);
                action = "禁用";
            }

            if (result)
            {
                await _logService.CreateLogAsync(new LogCreateDto
                {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = id,
                    ActionType = isActive ? ActionType.Enable : ActionType.Disable,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    Content = $"{action}患者：{id}"
                });
            }
            return result;
        }

        /// <summary>
        /// 搜索患者（根据姓名、手机号、身份证号）
        /// </summary>
        public async Task<List<PatientDetailDto>> SearchAsync(string keyword)
        {
            bool includeDisabled = true;
            var list = await _patientRepository.SearchAsync(keyword, includeDisabled);
            return list.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

        /// <summary>
        /// 获取可用患者列表（用于挂号选择）
        /// </summary>
        public async Task<List<PatientDetailDto>> GetActivePatientsAsync()
        {
            var patients = await _patientRepository.GetActivePatientsAsync();
            return patients.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

        /// <summary>
        /// 根据手机号查找患者
        /// </summary>
        public async Task<PatientDetailDto?> GetByPhoneNumberAsync(string phoneNumber)
        {
            var model = await _patientRepository.GetByPhoneNumberAsync(phoneNumber);
            return model == null ? null : _mapper.Map<PatientDetailDto>(model);
        }

        /// <summary>
        /// 根据身份证号查找患者
        /// </summary>
        public async Task<PatientDetailDto?> GetByIDNumberAsync(string idNumber)
        {
            var model = await _patientRepository.GetByIdNumberAsync(idNumber);
            return model == null ? null : _mapper.Map<PatientDetailDto>(model);
        }

        /// <summary>
        /// 高级搜索患者（简化实现，委托给基本查询）
        /// </summary>
        public async Task<PaginatedResult<PatientDetailDto>> AdvancedSearchAsync(PatientAdvancedSearchDto query)
        {
            var basicQuery = new PatientPagedQueryDto
            {
                Name = query.Name,
                CurrentPage = query.CurrentPage,
                PageSize = query.PageSize
            };
            return await GetPagedAsync(basicQuery);
        }

        #region 委托给专门服务的方法

        // 以下方法委托给专门的服务类处理

        public async Task<PatientVisitHistoryDto> GetVisitHistoryAsync(Guid patientId)
            => await _archiveService.GetVisitHistoryAsync(patientId);

        public async Task<bool> UpdateAllergyHistoryAsync(Guid patientId, string allergyHistory, Guid operatorId, string operatorName)
            => await _archiveService.UpdateAllergyHistoryAsync(patientId, allergyHistory, operatorId, operatorName);

        public async Task<PatientImportResultDto> ImportPatientsAsync(List<PatientImportDto> patients, Guid operatorId, string operatorName)
            => await _archiveService.ImportPatientsAsync(patients, operatorId, operatorName);

        public async Task<List<PatientExportDto>> ExportPatientsAsync(PatientExportQueryDto query)
            => await _archiveService.ExportPatientsAsync(query);

        public async Task<bool> MergeDuplicatePatientsAsync(Guid primaryId, Guid duplicateId, Guid operatorId, string operatorName)
            => await _archiveService.MergeDuplicatePatientsAsync(primaryId, duplicateId, operatorId, operatorName);

        public async Task<List<PatientTagDto>> GetPatientTagsAsync(Guid patientId)
            => await _archiveService.GetPatientTagsAsync(patientId);

        public async Task<bool> SetPatientTagsAsync(Guid patientId, List<string> tags, Guid operatorId, string operatorName)
            => await _archiveService.SetPatientTagsAsync(patientId, tags, operatorId, operatorName);

        public async Task<PatientStatisticsDto> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
            => await _statisticsService.GetStatisticsAsync(startDate, endDate);

        public async Task<List<AgeDistributionDto>> GetAgeDistributionAsync()
            => await _statisticsService.GetAgeDistributionAsync();

        public async Task<GenderDistributionDto> GetGenderDistributionAsync()
            => await _statisticsService.GetGenderDistributionAsync();

        public async Task<List<PatientTrendDto>> GetNewPatientTrendAsync(int months = 12)
            => await _statisticsService.GetNewPatientTrendAsync(months);

        public async Task<List<PatientDetailDto>> GetRecentActivePatientsAsync(int days = 30)
            => await _statisticsService.GetRecentActivePatientsAsync(days);

        public async Task<List<PatientDetailDto>> GetInactivePatientsAsync(int days = 180)
            => await _statisticsService.GetInactivePatientsAsync(days);

        public async Task<List<PatientDetailDto>> GetTodayNewPatientsAsync()
            => await _statisticsService.GetTodayNewPatientsAsync();

        public async Task<List<PatientDetailDto>> CheckDuplicatePatientsAsync(string idNumber, string phoneNumber)
            => await _validationService.CheckDuplicatePatientsAsync(idNumber, phoneNumber);

        #endregion

        /// <summary>
        /// 统一的患者操作日志记录
        /// </summary>
        private async Task LogPatientOperationAsync(Guid operatorId, string operatorName,
            LogActionType actionType, string content, string? parameters = null)
        {
            await _logService.LogUserActionAsync(
                operatorId,
                operatorName,
                actionType,
                "Patients",
                "PatientManagement",
                content,
                parameters: parameters
            );
        }
    }
}