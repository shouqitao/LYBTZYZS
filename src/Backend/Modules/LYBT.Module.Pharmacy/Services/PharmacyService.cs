using AutoMapper;
using LYBT.Models.Pharmacy;
using LYBT.Module.Pharmacy.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Pharmacy;
using LYBT.Shared.Models.Enums;

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
        /// 分页获取药房单列表
        /// </summary>
        public async Task<PaginatedResult<PharmacyDto>> GetPagedAsync(PaginationRequest query, UserRole operatorRole) {
            var allList = await _pharmacyRepository.GetListAsync();
            var dtoList = _mapper.Map<List<PharmacyDto>>(allList);

            var filteredList = dtoList.AsQueryable();

            if (!string.IsNullOrEmpty(query.SearchKeyword)) {
                filteredList = filteredList.Where(x =>
                    x.Id.ToString().Contains(query.SearchKeyword) ||
                    (x.PatientName != null && x.PatientName.Contains(query.SearchKeyword))
                );
            }

            var total = filteredList.Count();
            var pagedList = filteredList
                .Skip((query.CurrentPage - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PaginatedResult<PharmacyDto>(pagedList, total, query.CurrentPage, query.PageSize);
        }

        /// <summary>
        /// 新增药房单
        /// </summary>
        public async Task<PharmacyDto?> AddAsync(PharmacyCreateDto pharmacyCreateDto) {
            var model = _mapper.Map<PharmacyModel>(pharmacyCreateDto);
            model.Id = Guid.NewGuid();
            model.DispenseTime = DateTime.Now;
            var result = await _pharmacyRepository.AddAsync(model);
            if (!result)
                return null;
            
            // 返回创建的对象
            return _mapper.Map<PharmacyDto>(model);
        }

        /// <summary>
        /// 编辑药房单
        /// </summary>
        public async Task<bool> UpdateAsync(PharmacyEditDto pharmacyEditDto) {
            var model = await _pharmacyRepository.GetByIdAsync(pharmacyEditDto.Id);
            if (model == null)
                return false;
            // 转换 Status 字符串到枚举
            if (Enum.TryParse<Models.Pharmacy.PharmacyStatus>(pharmacyEditDto.Status, out var status))
            {
                model.Status = status;
            }
            model.Remark = pharmacyEditDto.Remark;
            model.UpdateTime = DateTime.Now;
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
            var list = await _pharmacyRepository.GetByStatusAsync(Models.Pharmacy.PharmacyStatus.Pending);
            return _mapper.Map<List<PharmacyDto>>(list);
        }

        /// <summary>
        /// 将处方标记为已抓药
        /// </summary>
        public async Task<bool> MarkAsPreparedAsync(Guid id) {
            var model = await _pharmacyRepository.GetByIdAsync(id);
            if (model == null)
                return false;
            model.Status = Models.Pharmacy.PharmacyStatus.Dispensed;
            return await _pharmacyRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 获取待配药列表
        /// </summary>
        public async Task<List<PharmacyQueueDto>> GetPendingListAsync() {
            var list = await _pharmacyRepository.GetByStatusAsync(Models.Pharmacy.PharmacyStatus.Pending);
            return _mapper.Map<List<PharmacyQueueDto>>(list);
        }

        /// <summary>
        /// 开始配药
        /// </summary>
        public async Task<bool> StartDispensingAsync(Guid id) {
            var model = await _pharmacyRepository.GetByIdAsync(id);
            if (model == null)
                return false;
            model.Status = Models.Pharmacy.PharmacyStatus.Dispensing;
            model.DispensingTime = DateTime.Now;
            return await _pharmacyRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 完成配药
        /// </summary>
        public async Task<bool> CompleteDispensingAsync(Guid id) {
            var model = await _pharmacyRepository.GetByIdAsync(id);
            if (model == null)
                return false;
            model.Status = Models.Pharmacy.PharmacyStatus.Dispensed;
            return await _pharmacyRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 取消配药
        /// </summary>
        public async Task<bool> CancelDispensingAsync(Guid id, string reason) {
            var model = await _pharmacyRepository.GetByIdAsync(id);
            if (model == null)
                return false;
            model.Status = Models.Pharmacy.PharmacyStatus.Cancelled;
            model.Remark = reason;
            return await _pharmacyRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 根据医疗案例ID获取配药记录
        /// </summary>
        public async Task<PharmacyDetailDto?> GetByMedicalCaseIdAsync(Guid medicalCaseId) {
            var list = await _pharmacyRepository.GetListAsync();
            var model = list.FirstOrDefault(p => p.MedicalCaseId == medicalCaseId);
            return model == null ? null : _mapper.Map<PharmacyDetailDto>(model);
        }

        /// <summary>
        /// 根据处方ID获取配药记录
        /// </summary>
        public async Task<PharmacyDetailDto?> GetByPrescriptionIdAsync(Guid prescriptionId) {
            var list = await _pharmacyRepository.GetListAsync();
            var model = list.FirstOrDefault(p => p.PrescriptionId == prescriptionId);
            return model == null ? null : _mapper.Map<PharmacyDetailDto>(model);
        }

        /// <summary>
        /// 根据患者ID获取配药历史
        /// </summary>
        public async Task<List<PharmacyDto>> GetByPatientIdAsync(Guid patientId) {
            var list = await _pharmacyRepository.GetListAsync();
            var filtered = list.Where(p => p.PatientId == patientId).ToList();
            return _mapper.Map<List<PharmacyDto>>(filtered);
        }

        /// <summary>
        /// 获取今日配药记录
        /// </summary>
        public async Task<List<PharmacyDto>> GetTodayRecordsAsync() {
            var list = await _pharmacyRepository.GetListAsync();
            var today = DateTime.Today;
            var filtered = list.Where(p => p.CreateTime.Date == today).ToList();
            return _mapper.Map<List<PharmacyDto>>(filtered);
        }

        /// <summary>
        /// 发药确认
        /// </summary>
        public async Task<bool> ConfirmDispenseAsync(Guid id, string receiverName, string receiverPhone) {
            var model = await _pharmacyRepository.GetByIdAsync(id);
            if (model == null)
                return false;
            model.Status = Models.Pharmacy.PharmacyStatus.Issued;
            model.ReceiverName = receiverName;
            model.ReceiverPhone = receiverPhone;
            model.DispenseTime = DateTime.Now;
            return await _pharmacyRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 药品库存检查
        /// </summary>
        public async Task<StockCheckResultDto> CheckStockAsync(Guid prescriptionId) {
            // 这里简化实现，实际需要检查处方中每个药材的库存
            return await Task.FromResult(new StockCheckResultDto {
                HasSufficientStock = true,
                ShortageItems = []
            });
        }

        /// <summary>
        /// 获取配药统计
        /// </summary>
        public async Task<PharmacyStatisticsDto> GetStatisticsAsync(DateTime startDate, DateTime endDate) {
            var list = await _pharmacyRepository.GetListAsync();
            var filtered = list.Where(p => p.CreateTime >= startDate && p.CreateTime <= endDate).ToList();
            
            return new PharmacyStatisticsDto {
                TotalPrescriptions = filtered.Count,
                PendingCount = filtered.Count(p => p.Status == Models.Pharmacy.PharmacyStatus.Pending),
                DispensedCount = filtered.Count(p => p.Status == Models.Pharmacy.PharmacyStatus.Dispensed || p.Status == Models.Pharmacy.PharmacyStatus.Issued),
                CancelledCount = filtered.Count(p => p.Status == Models.Pharmacy.PharmacyStatus.Cancelled),
                StartDate = startDate,
                EndDate = endDate
            };
        }
    }
}