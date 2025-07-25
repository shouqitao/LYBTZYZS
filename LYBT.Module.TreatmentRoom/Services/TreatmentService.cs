using AutoMapper;
using LYBT.Module.TreatmentRoom.Interfaces;
using LYBT.Module.TreatmentRoom.Models;
using LYBT.Module.TreatmentRoom.Models.Dtos;

namespace LYBT.Module.TreatmentRoom.Services {

    /// <summary>
    /// 治疗室业务服务实现类，封装治疗室相关业务逻辑
    /// </summary>
    public class TreatmentRoomService : ITreatmentRoomService {
        private readonly ITreatmentRoomRepository _treatmentRoomRepository;
        private readonly IMapper _mapper;

        /// <summary>
        /// 构造方法，注入仓储和映射服务
        /// </summary>
        public TreatmentRoomService(ITreatmentRoomRepository treatmentRoomRepository, IMapper mapper) {
            _treatmentRoomRepository = treatmentRoomRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// 根据ID获取治疗室详情
        /// </summary>
        public async Task<TreatmentRoomDetailDto?> GetByIdAsync(Guid id) {
            var model = await _treatmentRoomRepository.GetByIdAsync(id);
            return model == null ? null : _mapper.Map<TreatmentRoomDetailDto>(model);
        }

        /// <summary>
        /// 获取治疗室单列表
        /// </summary>
        public async Task<List<TreatmentRoomDto>> GetListAsync() {
            var list = await _treatmentRoomRepository.GetListAsync();
            return _mapper.Map<List<TreatmentRoomDto>>(list);
        }

        /// <summary>
        /// 新增治疗室单
        /// </summary>
        public async Task<bool> AddAsync(TreatmentRoomCreateDto treatmentRoomCreateDto) {
            var model = _mapper.Map<TreatmentRoomModel>(treatmentRoomCreateDto);
            model.Id = Guid.NewGuid();
            model.StartTime = DateTime.Now;
            return await _treatmentRoomRepository.AddAsync(model);
        }

        /// <summary>
        /// 编辑治疗室单
        /// </summary>
        public async Task<bool> UpdateAsync(TreatmentRoomEditDto treatmentRoomEditDto) {
            var model = await _treatmentRoomRepository.GetByIdAsync(treatmentRoomEditDto.Id);
            if (model == null)
                return false;
            model.TreatmentItem = treatmentRoomEditDto.TreatmentItem;
            model.Count = treatmentRoomEditDto.Count;
            model.Status = treatmentRoomEditDto.Status;
            model.EndTime = treatmentRoomEditDto.EndTime;
            model.Remark = treatmentRoomEditDto.Remark;
            return await _treatmentRoomRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 删除治疗室单
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            return await _treatmentRoomRepository.DeleteAsync(id);
        }

        /// <summary>
        /// 执行GetByStatusAsync操作。
        /// </summary>
        /// <param name="status">参数status</param>
        /// <returns>返回值</returns>
        public async Task<List<TreatmentRoomDto>> GetByStatusAsync(string status) {
            var list = await _treatmentRoomRepository.GetByStatusAsync(status);
            return _mapper.Map<List<TreatmentRoomDto>>(list);
        }
    }
}