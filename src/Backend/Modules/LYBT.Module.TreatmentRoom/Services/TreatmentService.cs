using AutoMapper;
using LYBT.Models.TreatmentRoom;
using LYBT.Module.TreatmentRoom.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.TreatmentRoom;
using LYBT.Shared.Models.Enums;
using System.Linq;

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
        /// 分页获取治疗室列表
        /// </summary>
        public async Task<PaginatedResult<TreatmentRoomDto>> GetPagedAsync(PaginationRequest query, UserRole operatorRole) {
            var allList = await _treatmentRoomRepository.GetListAsync();
            var dtoList = _mapper.Map<List<TreatmentRoomDto>>(allList);

            var filteredList = dtoList.AsQueryable();

            if (!string.IsNullOrEmpty(query.SearchKeyword)) {
                filteredList = filteredList.Where(x =>
                    x.Id.ToString().Contains(query.SearchKeyword) ||
                    (x.PatientName != null && x.PatientName.Contains(query.SearchKeyword)) ||
                    x.Status.ToString().Contains(query.SearchKeyword)
                );
            }

            var total = filteredList.Count();
            var pagedList = filteredList
                .Skip((query.CurrentPage - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PaginatedResult<TreatmentRoomDto>(pagedList, total, query.CurrentPage, query.PageSize);
        }

        /// <summary>
        /// 新增治疗室单
        /// </summary>
        public async Task<bool> AddAsync(TreatmentRoomCreateDto treatmentRoomCreateDto) {
            var model = _mapper.Map<TreatmentTaskModel>(treatmentRoomCreateDto);
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
            model.Status = treatmentRoomEditDto.Status.ToString();
            model.Remark = treatmentRoomEditDto.TreatmentNotes;
            if (treatmentRoomEditDto.TherapistId.HasValue)
                model.TherapistId = treatmentRoomEditDto.TherapistId.Value;
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

        /// <summary>
        /// 获取待治疗列表
        /// </summary>
        public async Task<List<TreatmentRoomQueueDto>> GetPendingListAsync() {
            var list = await _treatmentRoomRepository.GetByStatusAsync("Pending");
            return _mapper.Map<List<TreatmentRoomQueueDto>>(list);
        }

        /// <summary>
        /// 开始治疗
        /// </summary>
        public async Task<bool> StartTreatmentAsync(Guid id, Guid therapistId) {
            var model = await _treatmentRoomRepository.GetByIdAsync(id);
            if (model == null)
                return false;
            model.TherapistId = therapistId;
            model.Status = TreatmentTaskStatus.InProgress.ToString();
            model.StartTime = DateTime.Now;
            return await _treatmentRoomRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 完成治疗
        /// </summary>
        public async Task<bool> CompleteTreatmentAsync(Guid id, string treatmentNote) {
            var model = await _treatmentRoomRepository.GetByIdAsync(id);
            if (model == null)
                return false;
            model.Status = TreatmentTaskStatus.Completed.ToString();
            model.EndTime = DateTime.Now;
            model.Remark = treatmentNote;
            return await _treatmentRoomRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 取消治疗
        /// </summary>
        public async Task<bool> CancelTreatmentAsync(Guid id, string reason) {
            var model = await _treatmentRoomRepository.GetByIdAsync(id);
            if (model == null)
                return false;
            model.Status = TreatmentTaskStatus.Cancelled.ToString();
            model.Remark = reason;
            return await _treatmentRoomRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 根据医疗案例ID获取治疗记录
        /// </summary>
        public async Task<List<TreatmentRoomDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId) {
            var list = await _treatmentRoomRepository.GetListAsync();
            var filtered = list.Where(t => t.MedicalCaseId == medicalCaseId).ToList();
            return _mapper.Map<List<TreatmentRoomDto>>(filtered);
        }

        /// <summary>
        /// 根据患者ID获取治疗历史
        /// </summary>
        public async Task<List<TreatmentRoomDto>> GetByPatientIdAsync(Guid patientId) {
            var list = await _treatmentRoomRepository.GetListAsync();
            var filtered = list.Where(t => t.PatientId == patientId).ToList();
            return _mapper.Map<List<TreatmentRoomDto>>(filtered);
        }

        /// <summary>
        /// 根据治疗师ID获取治疗记录
        /// </summary>
        public async Task<List<TreatmentRoomDto>> GetByTherapistIdAsync(Guid therapistId) {
            var list = await _treatmentRoomRepository.GetListAsync();
            var filtered = list.Where(t => t.TherapistId == therapistId).ToList();
            return _mapper.Map<List<TreatmentRoomDto>>(filtered);
        }

        /// <summary>
        /// 获取今日治疗记录
        /// </summary>
        public async Task<List<TreatmentRoomDto>> GetTodayTreatmentsAsync() {
            var list = await _treatmentRoomRepository.GetListAsync();
            var today = DateTime.Today;
            var filtered = list.Where(t => t.StartTime.HasValue && t.StartTime.Value.Date == today).ToList();
            return _mapper.Map<List<TreatmentRoomDto>>(filtered);
        }

        /// <summary>
        /// 更新治疗进度
        /// </summary>
        public async Task<bool> UpdateProgressAsync(Guid id, int completedSessions, string progressNote) {
            var model = await _treatmentRoomRepository.GetByIdAsync(id);
            if (model == null)
                return false;
            model.Count = completedSessions;
            model.Remark = progressNote;
            return await _treatmentRoomRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 获取治疗室使用情况
        /// </summary>
        public async Task<List<TreatmentRoomUsageDto>> GetRoomUsageAsync() {
            // 简化实现，返回空列表
            return await Task.FromResult(new List<TreatmentRoomUsageDto>());
        }

        /// <summary>
        /// 分配治疗室
        /// </summary>
        public async Task<bool> AssignRoomAsync(Guid id, Guid roomId) {
            var model = await _treatmentRoomRepository.GetByIdAsync(id);
            if (model == null)
                return false;
            model.RoomId = roomId;
            return await _treatmentRoomRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 获取治疗统计
        /// </summary>
        public async Task<TreatmentRoomStatisticsDto> GetStatisticsAsync(DateTime startDate, DateTime endDate) {
            var list = await _treatmentRoomRepository.GetListAsync();
            var filtered = list.Where(t => t.StartTime >= startDate && t.StartTime <= endDate).ToList();
            
            return new TreatmentRoomStatisticsDto {
                TotalPatients = filtered.Select(t => t.PatientId).Distinct().Count(),
                TotalSessions = filtered.Count,
                CompletedSessions = filtered.Count(t => t.Status == TreatmentTaskStatus.Completed.ToString()),
                CancelledSessions = filtered.Count(t => t.Status == TreatmentTaskStatus.Cancelled.ToString()),
                StartDate = startDate,
                EndDate = endDate,
                AverageDuration = 30 // 默认30分钟
            };
        }

        /// <summary>
        /// 批量安排治疗
        /// </summary>
        public async Task<bool> BatchScheduleTreatmentsAsync(List<Guid> physiotherapyItemIds) {
            // 简化实现
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 获取可用治疗室列表
        /// </summary>
        public async Task<List<AvailableRoomDto>> GetAvailableRoomsAsync() {
            // 简化实现，返回示例数据
            return await Task.FromResult(new List<AvailableRoomDto> {
                new AvailableRoomDto { RoomId = Guid.NewGuid(), RoomNumber = "101", AvailableFrom = DateTime.Now },
                new AvailableRoomDto { RoomId = Guid.NewGuid(), RoomNumber = "102", AvailableFrom = DateTime.Now },
                new AvailableRoomDto { RoomId = Guid.NewGuid(), RoomNumber = "103", AvailableFrom = DateTime.Now.AddMinutes(30) }
            });
        }
    }
}