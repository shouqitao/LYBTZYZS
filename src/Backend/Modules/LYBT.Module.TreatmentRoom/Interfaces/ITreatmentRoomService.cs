using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.TreatmentRoom;
using LYBT.Shared.Models.Enums;

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
        /// 分页获取治疗室列表
        /// </summary>
        Task<PaginatedResult<TreatmentRoomDto>> GetPagedAsync(PaginationRequest query, UserRole operatorRole);

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