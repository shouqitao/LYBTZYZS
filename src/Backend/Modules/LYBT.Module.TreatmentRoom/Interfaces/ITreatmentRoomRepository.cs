using LYBT.Models.TreatmentRoom;

namespace LYBT.Module.TreatmentRoom.Interfaces {

    /// <summary>
    /// 治疗室任务仓储接口，定义治疗室任务数据操作方法
    /// </summary>
    public interface ITreatmentRoomRepository {

        /// <summary>
        /// 根据治疗任务ID获取记录
        /// </summary>
        Task<TreatmentTaskModel?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有治疗任务记录列表
        /// </summary>
        Task<List<TreatmentTaskModel>> GetListAsync();

        /// <summary>
        /// 新增治疗任务记录
        /// </summary>
        Task<bool> AddAsync(TreatmentTaskModel treatmentTaskModel);

        /// <summary>
        /// 更新治疗任务记录
        /// </summary>
        Task<bool> UpdateAsync(TreatmentTaskModel treatmentTaskModel);

        /// <summary>
        /// 删除治疗任务记录
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 根据状态获取治疗任务记录
        /// </summary>
        Task<List<TreatmentTaskModel>> GetByStatusAsync(string status);
    }
}