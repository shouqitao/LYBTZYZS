using System.Text.Json;
using LYBT.Module.Logs.Dtos;
using LYBT.Module.Logs.Interfaces;
using LYBT.Common.Enums.Logs;
using LYBT.Module.Prescriptions.Models;

namespace LYBT.Module.Prescriptions.Services {
    /// <summary>
    /// 处方业务逻辑实现
    /// </summary>
    public class PrescriptionService : IPrescriptionService {
        // 临时内存存储，正式环境请替换为数据库
        private static readonly List<PrescriptionModel> _prescriptions = new();
        private readonly ILogService _logService;

        public PrescriptionService(ILogService logService) {
            _logService = logService;
        }

        public async Task<List<PrescriptionModel>> GetAllAsync() {
            return await Task.FromResult(_prescriptions.ToList());
        }

        public async Task<PrescriptionModel> GetByIdAsync(string id) {
            var item = _prescriptions.FirstOrDefault(x => x.PrescriptionId == id);
            return await Task.FromResult(item);
        }

        public async Task<bool> CreateAsync(PrescriptionModel prescription, Guid operatorId, string operatorName) {
            prescription.PrescriptionId = Guid.NewGuid().ToString();
            _prescriptions.Add(prescription);
            await _logService.AddLogAsync(new LogDto {
                LogType = LogType.Operation,
                ObjectType = ObjectType.Prescription,
                ObjectId = Guid.Parse(prescription.PrescriptionId),
                ActionType = ActionType.Create,
                OperatorId = operatorId,
                OperatorName = operatorName,
                LogTime = DateTime.Now,
                Content = "新增处方",
                NewValue = JsonSerializer.Serialize(prescription)
            });
            return await Task.FromResult(true);
        }

        public async Task<bool> UpdateAsync(PrescriptionModel prescription, Guid operatorId, string operatorName) {
            var index = _prescriptions.FindIndex(x => x.PrescriptionId == prescription.PrescriptionId);
            if (index < 0)
                return await Task.FromResult(false);
            var old = _prescriptions[index];
            _prescriptions[index] = prescription;
            await _logService.AddLogAsync(new LogDto {
                LogType = LogType.Operation,
                ObjectType = ObjectType.Prescription,
                ObjectId = Guid.Parse(prescription.PrescriptionId),
                ActionType = ActionType.Edit,
                OperatorId = operatorId,
                OperatorName = operatorName,
                LogTime = DateTime.Now,
                Content = "编辑处方",
                OldValue = JsonSerializer.Serialize(old),
                NewValue = JsonSerializer.Serialize(prescription)
            });
            return await Task.FromResult(true);
        }

        public async Task<bool> DeleteAsync(string id, Guid operatorId, string operatorName) {
            var item = _prescriptions.FirstOrDefault(x => x.PrescriptionId == id);
            if (item == null)
                return await Task.FromResult(false);
            _prescriptions.Remove(item);
            await _logService.AddLogAsync(new LogDto {
                LogType = LogType.Operation,
                ObjectType = ObjectType.Prescription,
                ObjectId = Guid.Parse(id),
                ActionType = ActionType.Other,
                OperatorId = operatorId,
                OperatorName = operatorName,
                LogTime = DateTime.Now,
                Content = "删除处方",
                OldValue = JsonSerializer.Serialize(item)
            });
            return await Task.FromResult(true);
        }
    }
}
