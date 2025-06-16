using LYBT.Module.Prescriptions.Models;

namespace LYBT.Module.Prescriptions.Services {

    /// <summary>
    /// 处方业务逻辑实现
    /// </summary>
    public class PrescriptionService : IPrescriptionService {

        // 临时内存存储，正式环境请替换为数据库
        private static readonly List<PrescriptionModel> _prescriptions = new();

        public async Task<List<PrescriptionModel>> GetAllAsync() {
            // 返回所有处方
            return await Task.FromResult(_prescriptions.ToList());
        }

        public async Task<PrescriptionModel> GetByIdAsync(string id) {
            // 根据ID查找处方
            var item = _prescriptions.FirstOrDefault(x => x.PrescriptionId == id);
            return await Task.FromResult(item);
        }

        public async Task<bool> CreateAsync(PrescriptionModel prescription) {
            // 新增处方
            prescription.PrescriptionId = Guid.NewGuid().ToString();
            _prescriptions.Add(prescription);
            return await Task.FromResult(true);
        }

        public async Task<bool> UpdateAsync(PrescriptionModel prescription) {
            // 更新处方
            var index = _prescriptions.FindIndex(x => x.PrescriptionId == prescription.PrescriptionId);
            if (index < 0)
                return await Task.FromResult(false);
            _prescriptions[index] = prescription;
            return await Task.FromResult(true);
        }

        public async Task<bool> DeleteAsync(string id) {
            // 删除处方
            var item = _prescriptions.FirstOrDefault(x => x.PrescriptionId == id);
            if (item == null)
                return await Task.FromResult(false);
            _prescriptions.Remove(item);
            return await Task.FromResult(true);
        }
    }
}