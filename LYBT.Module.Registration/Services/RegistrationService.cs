using AutoMapper;
using LYBT.Common.Enums;
using LYBT.Module.Queueing.Interfaces;
using LYBT.Module.Registration.Interfaces;
using LYBT.Module.Registration.Models;
using LYBT.Module.Registration.Models.Dtos;

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
        /// 新增挂号
        /// </summary>
        public async Task<bool> AddAsync(RegistrationCreateDto dto) {
            var model = _mapper.Map<RegistrationModel>(dto);
            model.Id = Guid.NewGuid();
            model.RegistrationTime = DateTime.Now;
            model.Status = RegistrationStatus.Pending;
            if (Guid.TryParse(dto.PatientId, out var patId))
                model.PatientId = patId;
            if (Guid.TryParse(dto.DoctorId, out var docId))
                model.DoctorId = docId;
            var result = await _registrationRepository.AddAsync(model);
            if (!result)
                return false;

            var queue = new QueueingModel {
                Id = Guid.NewGuid(),
                PatientId = Guid.TryParse(dto.PatientId, out var pid) ? pid : Guid.Empty,
                PatientName = model.PatientName,
                DoctorId = Guid.TryParse(dto.DoctorId, out var did) ? did : Guid.Empty,
                DoctorName = model.DoctorName,
                QueueType = dto.RegistrationType,
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