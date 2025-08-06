using System.Threading.Tasks;
using System.Linq;
using System;
﻿using AutoMapper;
using LYBT.Models.Queueing;
using LYBT.Models.Registration;
using LYBT.Module.Queueing.Interfaces;
using LYBT.Module.Registration.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Registration.Services {

    /// <summary>
    /// 挂号业务服务实现类（现场挂号模式）
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
        /// 新增挂号（现场挂号）
        /// </summary>
        public async Task<RegistrationDto?> AddAsync(RegistrationCreateDto dto) {
            // 检查医生是否休息
            // TODO: 调用医生服务检查休息状态
            
            // 获取今日挂号顺序号
            var queueNumber = await GetNextQueueNumberAsync(dto.DoctorId);
            
            var model = _mapper.Map<RegistrationModel>(dto);
            model.Id = Guid.NewGuid();
            model.RegistrationTime = DateTime.Now;
            model.Status = RegistrationStatus.Scheduled; // 现场挂号默认等待状态
            model.PatientId = dto.PatientId;
            model.DoctorId = dto.DoctorId;
            
            var result = await _registrationRepository.AddAsync(model);
            if (!result)
                return null;

            // 创建排队记录
            var queue = new QueueingModel {
                Id = Guid.NewGuid(),
                RegistrationId = model.Id, // 关联挂号记录
                PatientId = dto.PatientId,
                PatientName = model.PatientName,
                DoctorId = dto.DoctorId,
                DoctorName = model.DoctorName,
                QueueNumber = queueNumber,
                QueueType = dto.RegistrationType.ToString(),
                QueueTime = DateTime.Now,
                Status = QueueStatus.Waiting,
                Remark = "现场挂号"
            };
            await _queueingRepository.AddAsync(queue);
            
            var returnDto = _mapper.Map<RegistrationDto>(model);
            returnDto.QueueNumber = queueNumber; // 返回排队号
            return returnDto;
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

        #region 现场挂号特有功能

        /// <summary>
        /// 获取下一个排队号
        /// </summary>
        private async Task<int> GetNextQueueNumberAsync(Guid doctorId) {
            var today = DateTime.Today;
            var todayRegistrations = await _registrationRepository.GetTodayRegistrationsAsync(doctorId);
            return todayRegistrations.Count + 1;
        }

        /// <summary>
        /// 获取今日挂号列表
        /// </summary>
        public async Task<List<RegistrationDto>> GetTodayRegistrationsAsync(Guid? doctorId = null) {
            var registrations = await _registrationRepository.GetTodayRegistrationsAsync(doctorId);
            return _mapper.Map<List<RegistrationDto>>(registrations);
        }

        /// <summary>
        /// 获取医生今日挂号统计
        /// </summary>
        public async Task<DoctorRegistrationStatDto> GetDoctorTodayStatAsync(Guid doctorId) {
            var registrations = await _registrationRepository.GetTodayRegistrationsAsync(doctorId);
            return new DoctorRegistrationStatDto {
                DoctorId = doctorId,
                TotalCount = registrations.Count,
                WaitingCount = registrations.Count(r => r.Status == RegistrationStatus.Scheduled),
                InProgressCount = registrations.Count(r => r.Status == RegistrationStatus.InConsultation),
                CompletedCount = registrations.Count(r => r.Status == RegistrationStatus.Completed),
                CanceledCount = registrations.Count(r => r.Status == RegistrationStatus.Cancelled)
            };
        }

        /// <summary>
        /// 开始就诊
        /// </summary>
        public async Task<bool> StartConsultationAsync(Guid registrationId, Guid operatorId, string operatorName) {
            var registration = await _registrationRepository.GetByIdAsync(registrationId);
            if (registration == null || registration.Status != RegistrationStatus.Scheduled) {
                return false;
            }
            
            registration.Status = RegistrationStatus.InConsultation;
            registration.UpdateTime = DateTime.Now;
            
            var result = await _registrationRepository.UpdateAsync(registration);
            if (result) {
                // 更新排队状态
                await _queueingRepository.UpdateStatusAsync(registration.Id, QueueStatus.InProgress);
            }
            return result;
        }

        /// <summary>
        /// 完成就诊
        /// </summary>
        public async Task<bool> CompleteConsultationAsync(Guid registrationId, Guid operatorId, string operatorName) {
            var registration = await _registrationRepository.GetByIdAsync(registrationId);
            if (registration == null || registration.Status != RegistrationStatus.InConsultation) {
                return false;
            }
            
            registration.Status = RegistrationStatus.Completed;
            registration.UpdateTime = DateTime.Now;
            
            var result = await _registrationRepository.UpdateAsync(registration);
            if (result) {
                // 更新排队状态
                await _queueingRepository.UpdateStatusAsync(registration.Id, QueueStatus.Completed);
            }
            return result;
        }

        /// <summary>
        /// 获取当前正在就诊的挂号
        /// </summary>
        public async Task<RegistrationDto?> GetCurrentConsultationAsync(Guid doctorId) {
            var registrations = await _registrationRepository.GetTodayRegistrationsAsync(doctorId);
            var current = registrations.FirstOrDefault(r => r.Status == RegistrationStatus.InConsultation);
            return current == null ? null : _mapper.Map<RegistrationDto>(current);
        }

        /// <summary>
        /// 获取下一个等待就诊的挂号
        /// </summary>
        public async Task<RegistrationDto?> GetNextWaitingAsync(Guid doctorId) {
            var registrations = await _registrationRepository.GetTodayRegistrationsAsync(doctorId);
            var next = registrations
                .Where(r => r.Status == RegistrationStatus.Scheduled)
                .OrderBy(r => r.RegistrationTime)
                .FirstOrDefault();
            return next == null ? null : _mapper.Map<RegistrationDto>(next);
        }

        #endregion
    }
}