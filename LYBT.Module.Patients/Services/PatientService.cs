using AutoMapper;
using LYBT.Common.Models;
using LYBT.Models.Patient;
using LYBT.Module.Patients.Dtos;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Models;
using LYBT.Module.Logs.Interfaces;
using LYBT.Module.Logs.Dtos;
using LYBT.Common.Enums.Logs;
using System.Text.Json;

namespace LYBT.Module.Patients.Services {
    /// <summary>
    /// 病人服务实现（业务逻辑层）
    /// </summary>
    public class PatientService : IPatientService {
        private readonly IPatientRepository _patientRepository;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        public PatientService(IPatientRepository patientRepository,
            IMapper mapper,
            ILogService logService) {
            _patientRepository = patientRepository;
            _mapper = mapper;
            _logService = logService;
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

        public async Task<bool> DeleteAsync(string id, Guid operatorId, string operatorName) {
            var patient = await _patientRepository.GetByIdAsync(Guid.Parse(id));
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
                if (await DeleteAsync(id, operatorId, operatorName))
                    count++;
            }
            return count;
        }
    }
}
