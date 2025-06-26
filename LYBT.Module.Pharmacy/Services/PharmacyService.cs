using AutoMapper;
using LYBT.Common.Enums;
using LYBT.Models.Pharmacy;
using LYBT.Module.Pharmacy.Dtos;
using LYBT.Module.Pharmacy.Interfaces;

namespace LYBT.Module.Pharmacy.Services {

    /// <summary>
    /// 药房业务服务实现类，封装药房相关业务逻辑
    /// </summary>
    public class PharmacyService : IPharmacyService {
        private readonly IPharmacyRepository _pharmacyRepository;
        private readonly IMapper _mapper;

        /// <summary>
        /// 构造方法，注入仓储和映射服务
        /// </summary>
        public PharmacyService(IPharmacyRepository pharmacyRepository, IMapper mapper) {
            _pharmacyRepository = pharmacyRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// 根据ID获取药房单详情
        /// </summary>
        public async Task<PharmacyDetailDto?> GetByIdAsync(Guid id) {
            var model = await _pharmacyRepository.GetByIdAsync(id);
            return model == null ? null : _mapper.Map<PharmacyDetailDto>(model);
        }

        /// <summary>
        /// 获取药房单列表
        /// </summary>
        public async Task<List<PharmacyDto>> GetListAsync() {
            var list = await _pharmacyRepository.GetListAsync();
            return _mapper.Map<List<PharmacyDto>>(list);
        }

        /// <summary>
        /// 新增药房单
        /// </summary>
        public async Task<bool> AddAsync(PharmacyCreateDto pharmacyCreateDto) {
            var model = _mapper.Map<PharmacyModel>(pharmacyCreateDto);
            model.Id = Guid.NewGuid();
            model.DispenseTime = DateTime.Now;
            return await _pharmacyRepository.AddAsync(model);
        }

        /// <summary>
        /// 编辑药房单
        /// </summary>
        public async Task<bool> UpdateAsync(PharmacyEditDto pharmacyEditDto) {
            var model = await _pharmacyRepository.GetByIdAsync(pharmacyEditDto.Id);
            if (model == null)
                return false;
            model.OperatorId = pharmacyEditDto.OperatorId;
            model.DispenseTime = pharmacyEditDto.DispenseTime;
            model.Status = pharmacyEditDto.Status;
            model.Remark = pharmacyEditDto.Remark;
            return await _pharmacyRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 删除药房单
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            return await _pharmacyRepository.DeleteAsync(id);
        }

        /// <summary>
        /// 获取待抓药处方列表
        /// </summary>
        public async Task<List<PharmacyDto>> GetWaitingListAsync() {
            var list = await _pharmacyRepository.GetByStatusAsync(PharmacyStatus.Waiting);
            return _mapper.Map<List<PharmacyDto>>(list);
        }

        /// <summary>
        /// 将处方标记为已抓药
        /// </summary>
        public async Task<bool> MarkAsPreparedAsync(Guid id) {
            var model = await _pharmacyRepository.GetByIdAsync(id);
            if (model == null)
                return false;
            model.Status = PharmacyStatus.Prepared;
            return await _pharmacyRepository.UpdateAsync(model);
        }
    }
}