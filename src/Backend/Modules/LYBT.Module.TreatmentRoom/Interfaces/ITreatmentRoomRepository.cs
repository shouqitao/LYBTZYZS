using LYBT.Models.TreatmentRoom;

namespace LYBT.Module.TreatmentRoom.Interfaces {

    /// <summary>
    /// 治疗室仓储接口，定义治疗室数据操作方法
    /// </summary>
    public interface ITreatmentRoomRepository {

        /// <summary>
        /// 根据治疗室单ID获取记录
        /// </summary>
        Task<TreatmentRoomModel?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有治疗室记录列表
        /// </summary>
        Task<List<TreatmentRoomModel>> GetListAsync();

        /// <summary>
        /// 新增治疗室记录
        /// </summary>
        Task<bool> AddAsync(TreatmentRoomModel treatmentRoomModel);

        /// <summary>
        /// 更新治疗室记录
        /// </summary>
        Task<bool> UpdateAsync(TreatmentRoomModel treatmentRoomModel);

        /// <summary>
        /// 删除治疗室记录
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 根据状态获取治疗室记录
        /// </summary>
        Task<List<TreatmentRoomModel>> GetByStatusAsync(string status);
    }
}