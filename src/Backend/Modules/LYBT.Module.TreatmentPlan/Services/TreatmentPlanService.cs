using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Module.TreatmentPlan.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.TreatmentPlan;
using LYBT.Models.TreatmentPlan;

namespace LYBT.Module.TreatmentPlan.Services
{
    /// <summary>
    /// 治疗方案服务实现
    /// </summary>
    public class TreatmentPlanService : ITreatmentPlanService
    {
        private readonly ITreatmentPlanRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<TreatmentPlanService> _logger;

        public TreatmentPlanService(
            ITreatmentPlanRepository repository,
            IMapper mapper,
            ILogger<TreatmentPlanService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// 获取治疗方案列表
        /// </summary>
        public async Task<List<TreatmentPlanDto>> GetListAsync()
        {
            try
            {
                var models = await _repository.GetListAsync();
                return _mapper.Map<List<TreatmentPlanDto>>(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取治疗方案列表失败");
                throw;
            }
        }

        /// <summary>
        /// 分页获取治疗方案列表
        /// </summary>
        public async Task<PaginatedResult<TreatmentPlanDto>> GetPagedAsync(PaginationRequest request)
        {
            try
            {
                var models = await _repository.GetListAsync();
                var dtos = _mapper.Map<List<TreatmentPlanDto>>(models);

                // 搜索过滤
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    dtos = dtos.Where(x =>
                        x.PatientName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        x.DoctorName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                // 排序
                dtos = request.SortBy?.ToLower() switch
                {
                    "patientname" => request.SortDesc ? dtos.OrderByDescending(x => x.PatientName).ToList() : dtos.OrderBy(x => x.PatientName).ToList(),
                    "doctorname" => request.SortDesc ? dtos.OrderByDescending(x => x.DoctorName).ToList() : dtos.OrderBy(x => x.DoctorName).ToList(),
                    "createtime" => request.SortDesc ? dtos.OrderByDescending(x => x.CreateTime).ToList() : dtos.OrderBy(x => x.CreateTime).ToList(),
                    _ => dtos.OrderByDescending(x => x.CreateTime).ToList()
                };

                // 分页
                var total = dtos.Count;
                var items = dtos
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                return new PaginatedResult<TreatmentPlanDto>
                {
                    Items = items,
                    TotalCount = total,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页获取治疗方案列表失败");
                throw;
            }
        }

        /// <summary>
        /// 获取治疗方案详情
        /// </summary>
        public async Task<TreatmentPlanDetailDto?> GetByIdAsync(Guid id)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                return model == null ? null : _mapper.Map<TreatmentPlanDetailDto>(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取治疗方案详情失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 创建治疗方案
        /// </summary>
        public async Task<TreatmentPlanDetailDto> CreateAsync(TreatmentPlanCreateDto dto)
        {
            try
            {
                var model = _mapper.Map<TreatmentPlanModel>(dto);
                model.Id = Guid.NewGuid();
                model.CreateTime = DateTime.Now;
                model.IsActive = true;
                model.TotalAmount = CalculateTotalAmount(model);

                var created = await _repository.CreateAsync(model);

                // TODO: 更新医疗案例状态为治疗方案制定中

                return _mapper.Map<TreatmentPlanDetailDto>(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建治疗方案失败");
                throw;
            }
        }

        /// <summary>
        /// 更新治疗方案
        /// </summary>
        public async Task<bool> UpdateAsync(Guid id, TreatmentPlanUpdateDto dto)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                {
                    _logger.LogWarning("治疗方案不存在，ID: {Id}", id);
                    return false;
                }

                // 更新字段
                if (!string.IsNullOrWhiteSpace(dto.Remark))
                    model.Remark = dto.Remark;

                model.UpdateTime = DateTime.Now;
                model.TotalAmount = CalculateTotalAmount(model);

                return await _repository.UpdateAsync(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新治疗方案失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 删除治疗方案（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                {
                    _logger.LogWarning("治疗方案不存在，ID: {Id}", id);
                    return false;
                }

                model.IsActive = false;
                model.UpdateTime = DateTime.Now;

                return await _repository.UpdateAsync(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除治疗方案失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 根据医疗案例ID获取治疗方案
        /// </summary>
        public async Task<TreatmentPlanDetailDto?> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                var model = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);
                return model == null ? null : _mapper.Map<TreatmentPlanDetailDto>(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医疗案例ID获取治疗方案失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 添加处方到治疗方案
        /// </summary>
        public async Task<bool> AddPrescriptionAsync(Guid id, PrescriptionDto prescription)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                {
                    _logger.LogWarning("治疗方案不存在，ID: {Id}", id);
                    return false;
                }

                model.Prescription = _mapper.Map<PrescriptionModel>(prescription);
                model.UpdateTime = DateTime.Now;
                model.TotalAmount = CalculateTotalAmount(model);

                return await _repository.UpdateAsync(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加处方到治疗方案失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 添加理疗项目到治疗方案
        /// </summary>
        public async Task<bool> AddPhysiotherapyItemAsync(Guid id, PhysiotherapyItemDto item)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                {
                    _logger.LogWarning("治疗方案不存在，ID: {Id}", id);
                    return false;
                }

                var physiotherapyItem = _mapper.Map<PhysiotherapyItemModel>(item);
                physiotherapyItem.Id = Guid.NewGuid();
                
                if (model.PhysiotherapyItems == null)
                    model.PhysiotherapyItems = new List<PhysiotherapyItemModel>();
                
                model.PhysiotherapyItems.Add(physiotherapyItem);
                model.UpdateTime = DateTime.Now;
                model.TotalAmount = CalculateTotalAmount(model);

                return await _repository.UpdateAsync(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加理疗项目到治疗方案失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 移除处方
        /// </summary>
        public async Task<bool> RemovePrescriptionAsync(Guid id)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                {
                    _logger.LogWarning("治疗方案不存在，ID: {Id}", id);
                    return false;
                }

                model.Prescription = null;
                model.UpdateTime = DateTime.Now;
                model.TotalAmount = CalculateTotalAmount(model);

                return await _repository.UpdateAsync(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除处方失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 移除理疗项目
        /// </summary>
        public async Task<bool> RemovePhysiotherapyItemAsync(Guid id, Guid itemId)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                {
                    _logger.LogWarning("治疗方案不存在，ID: {Id}", id);
                    return false;
                }

                if (model.PhysiotherapyItems != null)
                {
                    var item = model.PhysiotherapyItems.FirstOrDefault(x => x.Id == itemId);
                    if (item != null)
                    {
                        model.PhysiotherapyItems.Remove(item);
                        model.UpdateTime = DateTime.Now;
                        model.TotalAmount = CalculateTotalAmount(model);
                        return await _repository.UpdateAsync(model);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除理疗项目失败，ID: {Id}, ItemId: {ItemId}", id, itemId);
                throw;
            }
        }

        /// <summary>
        /// 确认治疗方案
        /// </summary>
        public async Task<bool> ConfirmPlanAsync(Guid id)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                {
                    _logger.LogWarning("治疗方案不存在，ID: {Id}", id);
                    return false;
                }

                model.UpdateTime = DateTime.Now;
                var result = await _repository.UpdateAsync(model);

                // TODO: 更新医疗案例状态为待付费

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "确认治疗方案失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 计算总金额
        /// </summary>
        private decimal CalculateTotalAmount(TreatmentPlanModel model)
        {
            decimal total = 0;

            // 计算处方费用
            if (model.Prescription != null)
            {
                total += model.Prescription.TotalAmount;
            }

            // 计算理疗项目费用
            if (model.PhysiotherapyItems != null)
            {
                foreach (var item in model.PhysiotherapyItems)
                {
                    total += item.Price * item.Quantity;
                }
            }

            return total;
        }
    }
}