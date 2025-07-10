using System.Text.Json;
using AutoMapper;
using LYBT.Module.Logs.Dtos;
using LYBT.Module.Logs.Interfaces;
using LYBT.Common.Enums.Logs;
using LYBT.Module.Prescriptions.Dtos;
using LYBT.Models.Prescriptions;
using LYBT.Module.Prescriptions.Repositories;

namespace LYBT.Module.Prescriptions.Services {
    /// <summary>
    /// 处方业务逻辑实现
    /// </summary>
    public class PrescriptionService : IPrescriptionService {
        private readonly IPrescriptionRepository _repository;
        private readonly ILogService _logService;
        private readonly IMapper _mapper;

        public PrescriptionService(IPrescriptionRepository repository, ILogService logService, IMapper mapper) {
            _repository = repository;
            _logService = logService;
            _mapper = mapper;
        }

        public async Task<List<PrescriptionDto>> GetAllAsync() {
            var list = await _repository.GetListAsync();
            return _mapper.Map<List<PrescriptionDto>>(list);
        }

        public async Task<PrescriptionDetailDto?> GetByIdAsync(string id) {
            if (!Guid.TryParse(id, out var gid))
                return null;
            var model = await _repository.GetByIdAsync(gid);
            return model == null ? null : _mapper.Map<PrescriptionDetailDto>(model);
        }

        public async Task<bool> CreateAsync(PrescriptionCreateDto dto, Guid operatorId, string operatorName) {
            var model = _mapper.Map<PrescriptionModel>(dto);
            model.Id = Guid.NewGuid();
            var success = await _repository.AddAsync(model);
            await _logService.AddLogAsync(new LogDto {
                LogType = LogType.Operation,
                ObjectType = ObjectType.Prescription,
                ObjectId = model.Id,
                ActionType = ActionType.Create,
                OperatorId = operatorId,
                OperatorName = operatorName,
                LogTime = DateTime.Now,
                Content = "新增处方",
                NewValue = JsonSerializer.Serialize(model)
            });
            return success;
        }

        public async Task<bool> UpdateAsync(PrescriptionEditDto dto, Guid operatorId, string operatorName) {
            var old = await _repository.GetByIdAsync(dto.Id);
            if (old == null) return false;
            var model = _mapper.Map(dto, old);
            var success = await _repository.UpdateAsync(model);
            await _logService.AddLogAsync(new LogDto {
                LogType = LogType.Operation,
                ObjectType = ObjectType.Prescription,
                ObjectId = model.Id,
                ActionType = ActionType.Edit,
                OperatorId = operatorId,
                OperatorName = operatorName,
                LogTime = DateTime.Now,
                Content = "编辑处方",
                OldValue = JsonSerializer.Serialize(old),
                NewValue = JsonSerializer.Serialize(model)
            });
            return success;
        }

        public async Task<bool> DeleteAsync(string id, Guid operatorId, string operatorName) {
            if (!Guid.TryParse(id, out var gid))
                return false;
            var item = await _repository.GetByIdAsync(gid);
            if (item == null)
                return false;
            var success = await _repository.DeleteAsync(gid);
            await _logService.AddLogAsync(new LogDto {
                LogType = LogType.Operation,
                ObjectType = ObjectType.Prescription,
                ObjectId = gid,
                ActionType = ActionType.Other,
                OperatorId = operatorId,
                OperatorName = operatorName,
                LogTime = DateTime.Now,
                Content = "删除处方",
                OldValue = JsonSerializer.Serialize(item)
            });
            return success;
        }
    }
}
