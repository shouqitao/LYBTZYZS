using AutoMapper;
using LYBT.Models.Queueing;
using LYBT.Models.Registration;
using LYBT.Module.Queueing.Interfaces;
using LYBT.Module.Registration.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Registration.Services {

    /// <summary>
    /// 挂号业务服务实现类
    /// </summary>
    public class RegistrationService : IRegistrationService {
        private readonly IRegistrationRepository _registrationRepository;
        private readonly IQueueingRepository _queueingRepository;
        private readonly IMapper _mapper;

        /// <summary>
        /// 构造方法，注入仓储与对象映射
        /// </summary>
        public RegistrationService(IRegistrationRepository registrationRepository, IQueueingRepository queueingRepository, IMapper mapper) {
            _registrationRepository = registrationRepository;
            _queueingRepository = queueingRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// 获取挂号详情
        /// </summary>
        public async Task<RegistrationDetailDto?> GetByIdAsync(Guid id) {
            var model = await _registrationRepository.GetByIdAsync(id);
            return model == null ? null : _mapper.Map<RegistrationDetailDto>(model);
        }

        /// <summary>
        /// 获取挂号列表
        /// </summary>
        public async Task<List<RegistrationDto>> GetListAsync() {
            var list = await _registrationRepository.GetListAsync();
            return _mapper.Map<List<RegistrationDto>>(list);
        }

        /// <summary>
        /// 分页查询挂号列表
        /// </summary>
        public async Task<PaginatedResult<RegistrationDto>> GetPagedAsync(PaginationRequest query, UserRole operatorRole) {
            var (list, total) = await _registrationRepository.GetPagedAsync(query, operatorRole);
            var dtos = _mapper.Map<List<RegistrationDto>>(list);
            return new PaginatedResult<RegistrationDto> {
                Items = dtos,
                TotalCount = total,
                CurrentPage = query.CurrentPage,
                PageSize = query.PageSize
            };
        }

        /// <summary>
        /// 新增挂号
        /// </summary>
        public async Task<bool> AddAsync(RegistrationCreateDto dto) {
            var model = _mapper.Map<RegistrationModel>(dto);
            model.Id = Guid.NewGuid();
            model.RegistrationTime = DateTime.Now;
            model.Status = RegistrationStatus.Scheduled;
            model.PatientId = dto.PatientId;
            model.DoctorId = dto.DoctorId;
            var result = await _registrationRepository.AddAsync(model);
            if (!result)
                return false;

            var queue = new QueueingModel {
                Id = Guid.NewGuid(),
                PatientId = dto.PatientId,
                PatientName = model.PatientName,
                DoctorId = dto.DoctorId,
                DoctorName = model.DoctorName,
                QueueType = dto.RegistrationType.ToString(),
                QueueTime = DateTime.Now,
                Status = QueueStatus.Waiting,
                Remark = "自动排队"
            };
            await _queueingRepository.AddAsync(queue);
            return true;
        }

        /// <summary>
        /// 编辑挂号
        /// </summary>
        public async Task<bool> UpdateAsync(RegistrationEditDto dto) {
            var model = await _registrationRepository.GetByIdAsync(dto.Id);
            if (model == null)
                return false;
            model.RegistrationType = dto.RegistrationType;
            model.Remark = dto.Remark;
            return await _registrationRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 删除挂号（物理删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            return await _registrationRepository.DeleteAsync(id);
        }

        /// <summary>
        /// 取消挂号，更新注册和队列状态
        /// </summary>
        public async Task<bool> CancelAsync(Guid id) {
            var result = await _registrationRepository.CancelAsync(id);
            if (result)
                await _queueingRepository.CancelAsync(id);
            return result;
        }
    }
}