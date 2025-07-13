using LYBT.Module.TreatmentRoom.Dtos;

namespace LYBT.Module.TreatmentRoom.Interfaces {

    /// <summary>
    /// 治疗室业务服务接口
    /// </summary>
    public interface ITreatmentRoomService {

        /// <summary>
        /// 根据ID获取治疗室详情
        /// </summary>
        Task<TreatmentRoomDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取治疗室列表
        /// </summary>
        Task<List<TreatmentRoomDto>> GetListAsync();

        /// <summary>
        /// 新增治疗室单
        /// </summary>
        Task<bool> AddAsync(TreatmentRoomCreateDto treatmentRoomCreateDto);

        /// <summary>
        /// 编辑治疗室单
        /// </summary>
        Task<bool> UpdateAsync(TreatmentRoomEditDto treatmentRoomEditDto);

        /// <summary>
        /// 删除治疗室单
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 根据状态获取治疗室单
        /// </summary>
        Task<List<TreatmentRoomDto>> GetByStatusAsync(string status);
    }
}