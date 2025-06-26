using AutoMapper;
using LYBT.Common.Enums.Logs;
using LYBT.Common.Models;
using LYBT.Module.Logs.Dtos;
using LYBT.Module.Logs.Interfaces;
using LYBT.Module.Patients.Dtos;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Models;
using LYBT.Module.Records.Dtos;
using LYBT.Module.Records.Interfaces;
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

        public async Task<bool> AddAsync(PatientCreateDto dto, Guid operatorId, string operatorName) {
            // 必填项校验等业务逻辑...
            var model = _mapper.Map<PatientModel>(dto);
            model.Id = Guid.NewGuid();
            // 省略拼音码生成等细节...
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

        public async Task<bool> UpdateAsync(PatientEditDto dto, Guid operatorId, string operatorName) {
            var model = await _patientRepository.GetByIdAsync(dto.Id);
            if (model == null)
                throw new ArgumentException("病人不存在");

            var oldJson = JsonSerializer.Serialize(model);
            // 赋值更新，略...
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

        public async Task<PatientDetailDto> GetByIdAsync(Guid id) {
            var model = await _patientRepository.GetByIdAsync(id);
            return model == null ? null : _mapper.Map<PatientDetailDto>(model);
        }

        public async Task<List<PatientDto>> GetAllAsync() {
            var list = await _patientRepository.GetListAsync(null, 1, int.MaxValue);
            return list.Select(_mapper.Map<PatientDto>).ToList();
        }

        public async Task<PagedResultDto<PatientDto>> GetPagedAsync(PatientPagedQueryDto query) {
            // 若你有更高阶的分页接口，可自行扩展
            var list = await _patientRepository.GetListAsync(query.Keyword, query.Page, query.PageSize);
            var total = await _patientRepository.GetCountAsync(query.Keyword);
            return new PagedResultDto<PatientDto> {
                TotalCount = total,
                Items = list.Select(_mapper.Map<PatientDto>).ToList()
            };
        }

        public async Task<int> BatchDeleteAsync(List<string> ids, Guid operatorId, string operatorName) {
            int count = 0;
            foreach (var id in ids) {
                if (Guid.TryParse(id, out var guid) && await DeleteAsync(guid, operatorId, operatorName))
                    count++;
            }
            return count;
        }

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

        public async Task<List<PatientDto>> SearchAsync(string keyword) {
            var list = await _patientRepository.SearchAsync(keyword);
            return list.Select(_mapper.Map<PatientDto>).ToList();
        }

        public async Task<List<PatientDto>> GetForDoctorAsync(Guid doctorId) {
            var list = await _patientRepository.GetForDoctorAsync(doctorId);
            return list.Select(_mapper.Map<PatientDto>).ToList();
        }

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

        public async Task<int> ImportAsync(List<PatientCreateDto> dtos, Guid operatorId, string operatorName) {
            int count = 0;
            foreach (var dto in dtos) {
                if (await AddAsync(dto, operatorId, operatorName))
                    count++;
            }
            return count;
        }

        public async Task<List<PatientDto>> ExportAsync() {
            var list = await _patientRepository.GetListAsync(null, 1, int.MaxValue);
            return list.Select(_mapper.Map<PatientDto>).ToList();
        }

        public async Task<List<RecordDto>> GetHistoryRecordsAsync(Guid patientId) {
            var records = await _recordService.GetByPatientIdAsync(patientId);
            return records;
        }
    }
}