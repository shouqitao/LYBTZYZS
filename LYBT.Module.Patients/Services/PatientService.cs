using AutoMapper;
using LYBT.Common.Enums.Logs;
using LYBT.Common.Models;
using LYBT.Models.Patients;
using LYBT.Module.Logs.Dtos;
using LYBT.Module.Logs.Interfaces;
using LYBT.Module.Patients.Dtos;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Records.Dtos;
using LYBT.Module.Records.Interfaces;
using CommonUtil = LYBT.CommonUtils.CommonUtils;
using System.Text.Json;

namespace LYBT.Module.Patients.Services {

    /// <summary>
    /// 病人服务实现（业务逻辑层）
    /// </summary>
    public class PatientService : IPatientService {
        private readonly IPatientRepository _patientRepository;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly IRecordService _recordService;

        public PatientService(IPatientRepository patientRepository,
            IMapper mapper,
            ILogService logService,
            IRecordService recordService) {
            _patientRepository = patientRepository;
            _mapper = mapper;
            _logService = logService;
            _recordService = recordService;
        }

        /// <summary>
        /// 新增患者档案，并记录操作日志
        /// </summary>
        /// <param name="dto">患者详细信息</param>
        /// <param name="operatorId">执行人员的标识</param>
        /// <param name="operatorName">执行人员姓名</param>
        /// <returns>新增是否成功</returns>
        public async Task<bool> AddAsync(PatientDetailDto dto, Guid operatorId, string operatorName) {
            var model = _mapper.Map<PatientModel>(dto);
            model.Id = Guid.NewGuid();
            model.PinyinCode = CommonUtil.GetPinyinCode(model.Name);
            var result = await _patientRepository.AddAsync(model);

            if (result) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = model.Id,
                    ActionType = ActionType.Create,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    LogTime = DateTime.Now,
                    Content = $"新增患者：{model.Name}",
                    NewValue = JsonSerializer.Serialize(model)
                });
            }

            return result;
        }

/// <summary>
/// 执行UpdateAsync操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <param name="operatorId">参数operatorId</param>
/// <param name="operatorName">参数operatorName</param>
/// <returns>返回值</returns>
        public async Task<bool> UpdateAsync(PatientDetailDto dto, Guid operatorId, string operatorName) {
            var model = await _patientRepository.GetByIdAsync(dto.Id);
            if (model == null)
                throw new ArgumentException("病人不存在");

            var oldJson = JsonSerializer.Serialize(model);
            _mapper.Map(dto, model);
            model.PinyinCode = CommonUtil.GetPinyinCode(model.Name);
            var result = await _patientRepository.UpdateAsync(model);

            if (result) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = model.Id,
                    ActionType = ActionType.Other,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    LogTime = DateTime.Now,
                    Content = $"编辑患者：{model.Name}",
                    OldValue = oldJson,
                    NewValue = JsonSerializer.Serialize(dto)
                });
            }

            return result;
        }

/// <summary>
/// 执行DeleteAsync操作。
/// </summary>
/// <param name="id">参数id</param>
/// <param name="operatorId">参数operatorId</param>
/// <param name="operatorName">参数operatorName</param>
/// <returns>返回值</returns>
        public async Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName) {
            var patient = await _patientRepository.GetByIdAsync(id);
            var result = await _patientRepository.DeleteAsync(id);

            if (result && patient != null) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = patient.Id,
                    ActionType = ActionType.Other,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    LogTime = DateTime.Now,
                    Content = $"删除患者：{patient.Name}",
                    OldValue = JsonSerializer.Serialize(patient)
                });
            }

            return result;
        }

        /// <summary>
        /// 根据患者Id获取患者详情
        /// </summary>
        /// <param name="id">患者唯一标识</param>
        /// <returns>患者详细信息，未找到则返回 null</returns>
        public async Task<PatientDetailDto> GetByIdAsync(Guid id) {
            var model = await _patientRepository.GetByIdAsync(id);
            return model == null ? null : _mapper.Map<PatientDetailDto>(model);
        }

        /// <summary>
        /// 获取所有患者列表
        /// </summary>
        /// <returns>患者信息集合</returns>
        public async Task<List<PatientDetailDto>> GetAllAsync() {
            var list = await _patientRepository.GetListAsync(null, 1, int.MaxValue);
            return list.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

        /// <summary>
        /// 按条件分页查询患者信息
        /// </summary>
        /// <param name="query">查询条件，包含关键字及分页参数</param>
        /// <returns>分页结果</returns>
        public async Task<PagedResultDto<PatientDetailDto>> GetPagedAsync(PatientPagedQueryDto query) {
            var list = await _patientRepository.GetListAsync(query.Keyword, query.Page, query.PageSize);
            var total = await _patientRepository.GetCountAsync(query.Keyword);
            return new PagedResultDto<PatientDetailDto> {
                TotalCount = total,
                Items = list.Select(_mapper.Map<PatientDetailDto>).ToList()
            };
        }

/// <summary>
/// 执行BatchDeleteAsync操作。
/// </summary>
/// <param name="ids">参数ids</param>
/// <param name="operatorId">参数operatorId</param>
/// <param name="operatorName">参数operatorName</param>
/// <returns>返回值</returns>
        public async Task<int> BatchDeleteAsync(List<string> ids, Guid operatorId, string operatorName) {
            int count = 0;
            foreach (var id in ids) {
                if (Guid.TryParse(id, out var guid) && await DeleteAsync(guid, operatorId, operatorName))
                    count++;
            }
            return count;
        }

/// <summary>
/// 执行EnableAsync操作。
/// </summary>
/// <param name="id">参数id</param>
/// <param name="operatorId">参数operatorId</param>
/// <param name="operatorName">参数operatorName</param>
/// <returns>返回值</returns>
        public async Task<bool> EnableAsync(Guid id, Guid operatorId, string operatorName) {
            var result = await _patientRepository.EnableAsync(id);
            if (result) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = id,
                    ActionType = ActionType.Enable,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    LogTime = DateTime.Now,
                    Content = $"启用患者：{id}"
                });
            }
            return result;
        }

/// <summary>
/// 执行DisableAsync操作。
/// </summary>
/// <param name="id">参数id</param>
/// <param name="operatorId">参数operatorId</param>
/// <param name="operatorName">参数operatorName</param>
/// <returns>返回值</returns>
        public async Task<bool> DisableAsync(Guid id, Guid operatorId, string operatorName) {
            var result = await _patientRepository.DisableAsync(id);
            if (result) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = id,
                    ActionType = ActionType.Disable,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    LogTime = DateTime.Now,
                    Content = $"禁用患者：{id}"
                });
            }
            return result;
        }

/// <summary>
/// 执行BatchDisableAsync操作。
/// </summary>
/// <param name="ids">参数ids</param>
/// <param name="operatorId">参数operatorId</param>
/// <param name="operatorName">参数operatorName</param>
/// <returns>返回值</returns>
        public async Task<int> BatchDisableAsync(List<Guid> ids, Guid operatorId, string operatorName) {
            var count = await _patientRepository.BatchDisableAsync(ids);
            if (count > 0) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = Guid.Empty,
                    ActionType = ActionType.Disable,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    LogTime = DateTime.Now,
                    Content = $"批量禁用患者：{count}人"
                });
            }
            return count;
        }

/// <summary>
/// 执行SearchAsync操作。
/// </summary>
/// <param name="keyword">参数keyword</param>
/// <returns>返回值</returns>
        public async Task<List<PatientDetailDto>> SearchAsync(string keyword) {
            var list = await _patientRepository.SearchAsync(keyword);
            return list.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

/// <summary>
/// 执行GetForDoctorAsync操作。
/// </summary>
/// <param name="doctorId">参数doctorId</param>
/// <returns>返回值</returns>
        public async Task<List<PatientDetailDto>> GetForDoctorAsync(Guid doctorId) {
            var list = await _patientRepository.GetForDoctorAsync(doctorId);
            return list.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

/// <summary>
/// 执行AssignDoctorAsync操作。
/// </summary>
/// <param name="patientId">参数patientId</param>
/// <param name="doctorId">参数doctorId</param>
/// <param name="operatorId">参数operatorId</param>
/// <param name="operatorName">参数operatorName</param>
/// <returns>返回值</returns>
        public async Task<bool> AssignDoctorAsync(Guid patientId, Guid doctorId, Guid operatorId, string operatorName) {
            var result = await _patientRepository.AssignDoctorAsync(patientId, doctorId);
            if (result) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = patientId,
                    ActionType = ActionType.Other,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    LogTime = DateTime.Now,
                    Content = $"授权患者{patientId}给医生{doctorId}"
                });
            }
            return result;
        }

/// <summary>
/// 执行ImportAsync操作。
/// </summary>
/// <param name="dtos">参数dtos</param>
/// <param name="operatorId">参数operatorId</param>
/// <param name="operatorName">参数operatorName</param>
/// <returns>返回值</returns>
        public async Task<int> ImportAsync(List<PatientDetailDto> dtos, Guid operatorId, string operatorName) {
            int count = 0;
            foreach (var dto in dtos) {
                if (await AddAsync(dto, operatorId, operatorName))
                    count++;
            }
            return count;
        }

/// <summary>
/// 执行ExportAsync操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<List<PatientDetailDto>> ExportAsync() {
            var list = await _patientRepository.GetListAsync(null, 1, int.MaxValue);
            return list.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

/// <summary>
/// 执行GetHistoryRecordsAsync操作。
/// </summary>
/// <param name="patientId">参数patientId</param>
/// <returns>返回值</returns>
        public async Task<List<RecordDto>> GetHistoryRecordsAsync(Guid patientId) {
            var records = await _recordService.GetByPatientIdAsync(patientId);
            return records;
        }
    }
}
