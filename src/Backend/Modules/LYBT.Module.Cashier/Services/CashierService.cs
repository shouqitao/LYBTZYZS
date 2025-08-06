using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Module.Cashier.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Cashier;
using LYBT.Models.Cashier;

namespace LYBT.Module.Cashier.Services
{
    /// <summary>
    /// 收银服务实现（替代BillingService）
    /// </summary>
    public class CashierService : ICashierService
    {
        private readonly ICashierRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<CashierService> _logger;

        public CashierService(
            ICashierRepository repository,
            IMapper mapper,
            ILogger<CashierService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// 获取收费记录列表
        /// </summary>
        public async Task<List<CashierDto>> GetListAsync()
        {
            try
            {
                var models = await _repository.GetListAsync();
                return _mapper.Map<List<CashierDto>>(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取收费记录列表失败");
                throw;
            }
        }

        /// <summary>
        /// 分页获取收费记录列表
        /// </summary>
        public async Task<PaginatedResult<CashierDto>> GetPagedAsync(PaginationRequest request)
        {
            try
            {
                var models = await _repository.GetListAsync();
                var dtos = _mapper.Map<List<CashierDto>>(models);

                // 搜索过滤
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    dtos = dtos.Where(x =>
                        x.PatientName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        x.InvoiceNumber.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                // 排序
                dtos = request.SortBy?.ToLower() switch
                {
                    "patientname" => request.SortDesc ? dtos.OrderByDescending(x => x.PatientName).ToList() : dtos.OrderBy(x => x.PatientName).ToList(),
                    "totalamount" => request.SortDesc ? dtos.OrderByDescending(x => x.TotalAmount).ToList() : dtos.OrderBy(x => x.TotalAmount).ToList(),
                    "paymenttime" => request.SortDesc ? dtos.OrderByDescending(x => x.PaymentTime).ToList() : dtos.OrderBy(x => x.PaymentTime).ToList(),
                    _ => dtos.OrderByDescending(x => x.CreateTime).ToList()
                };

                // 分页
                var total = dtos.Count;
                var items = dtos
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                return new PaginatedResult<CashierDto>
                {
                    Items = items,
                    TotalCount = total,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页获取收费记录列表失败");
                throw;
            }
        }

        /// <summary>
        /// 获取收费详情
        /// </summary>
        public async Task<CashierDetailDto?> GetByIdAsync(Guid id)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                return model == null ? null : _mapper.Map<CashierDetailDto>(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取收费详情失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 创建收费记录
        /// </summary>
        public async Task<CashierDetailDto> CreateAsync(CashierCreateDto dto)
        {
            try
            {
                var model = _mapper.Map<CashierModel>(dto);
                model.Id = Guid.NewGuid();
                model.InvoiceNumber = GenerateInvoiceNumber();
                model.CreateTime = DateTime.Now;
                model.PaymentStatus = PaymentStatus.Unpaid;
                model.IsActive = true;

                var created = await _repository.CreateAsync(model);
                return _mapper.Map<CashierDetailDto>(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建收费记录失败");
                throw;
            }
        }

        /// <summary>
        /// 更新收费记录
        /// </summary>
        public async Task<bool> UpdateAsync(Guid id, CashierUpdateDto dto)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                {
                    _logger.LogWarning("收费记录不存在，ID: {Id}", id);
                    return false;
                }

                // 更新字段
                if (dto.DiscountAmount.HasValue)
                    model.DiscountAmount = dto.DiscountAmount.Value;
                if (!string.IsNullOrWhiteSpace(dto.Remark))
                    model.Remark = dto.Remark;

                model.UpdateTime = DateTime.Now;
                model.ActualAmount = model.TotalAmount - model.DiscountAmount;

                return await _repository.UpdateAsync(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新收费记录失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 作废收费记录
        /// </summary>
        public async Task<bool> VoidAsync(Guid id, string reason)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                {
                    _logger.LogWarning("收费记录不存在，ID: {Id}", id);
                    return false;
                }

                model.PaymentStatus = PaymentStatus.Voided;
                model.Remark = $"作废原因: {reason}";
                model.UpdateTime = DateTime.Now;

                return await _repository.UpdateAsync(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "作废收费记录失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 根据医疗案例ID获取收费记录
        /// </summary>
        public async Task<CashierDetailDto?> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                var model = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);
                return model == null ? null : _mapper.Map<CashierDetailDto>(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医疗案例ID获取收费记录失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 根据患者ID获取收费记录
        /// </summary>
        public async Task<List<CashierDto>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                var models = await _repository.GetByPatientIdAsync(patientId);
                return _mapper.Map<List<CashierDto>>(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据患者ID获取收费记录失败，PatientId: {PatientId}", patientId);
                throw;
            }
        }

        /// <summary>
        /// 获取今日收费记录
        /// </summary>
        public async Task<List<CashierDto>> GetTodayBillsAsync()
        {
            try
            {
                var today = DateTime.Today;
                var models = await _repository.GetByDateRangeAsync(today, today.AddDays(1));
                return _mapper.Map<List<CashierDto>>(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取今日收费记录失败");
                throw;
            }
        }

        /// <summary>
        /// 获取日期范围内的收费记录
        /// </summary>
        public async Task<List<CashierDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var models = await _repository.GetByDateRangeAsync(startDate, endDate);
                return _mapper.Map<List<CashierDto>>(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取日期范围内的收费记录失败");
                throw;
            }
        }

        /// <summary>
        /// 计算收费金额
        /// </summary>
        public async Task<decimal> CalculateAmountAsync(Guid medicalCaseId)
        {
            try
            {
                // TODO: 从医疗案例中获取治疗方案，计算总金额
                return await Task.FromResult(0m);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算收费金额失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 执行付款
        /// </summary>
        public async Task<PaymentResultDto> ProcessPaymentAsync(Guid id, PaymentDto payment)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                {
                    return new PaymentResultDto
                    {
                        Success = false,
                        Message = "收费记录不存在"
                    };
                }

                if (model.PaymentStatus == PaymentStatus.Paid)
                {
                    return new PaymentResultDto
                    {
                        Success = false,
                        Message = "该账单已支付"
                    };
                }

                model.PaymentMethod = payment.PaymentMethod;
                model.PaymentTime = DateTime.Now;
                model.PaymentStatus = PaymentStatus.Paid;
                model.UpdateTime = DateTime.Now;

                var result = await _repository.UpdateAsync(model);

                if (result)
                {
                    // TODO: 更新医疗案例状态

                    return new PaymentResultDto
                    {
                        Success = true,
                        Message = "支付成功",
                        InvoiceNumber = model.InvoiceNumber,
                        PaymentTime = model.PaymentTime.Value
                    };
                }

                return new PaymentResultDto
                {
                    Success = false,
                    Message = "支付失败"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行付款失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 打印收费单据
        /// </summary>
        public async Task<byte[]> PrintReceiptAsync(Guid id)
        {
            try
            {
                // TODO: 实现打印逻辑
                return await Task.FromResult(new byte[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打印收费单据失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 退费处理
        /// </summary>
        public async Task<RefundResultDto> ProcessRefundAsync(Guid id, RefundDto refund)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                {
                    return new RefundResultDto
                    {
                        Success = false,
                        Message = "收费记录不存在"
                    };
                }

                if (model.PaymentStatus != PaymentStatus.Paid)
                {
                    return new RefundResultDto
                    {
                        Success = false,
                        Message = "该账单未支付，无法退费"
                    };
                }

                model.PaymentStatus = PaymentStatus.Refunded;
                model.RefundAmount = refund.RefundAmount;
                model.RefundReason = refund.RefundReason;
                model.RefundTime = DateTime.Now;
                model.UpdateTime = DateTime.Now;

                var result = await _repository.UpdateAsync(model);

                return new RefundResultDto
                {
                    Success = result,
                    Message = result ? "退费成功" : "退费失败",
                    RefundAmount = refund.RefundAmount,
                    RefundTime = model.RefundTime.Value
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "退费处理失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 获取收费统计
        /// </summary>
        public async Task<CashierStatisticsDto> GetStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var models = await _repository.GetByDateRangeAsync(startDate, endDate);
                
                var statistics = new CashierStatisticsDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalCount = models.Count,
                    TotalAmount = models.Where(x => x.PaymentStatus == PaymentStatus.Paid).Sum(x => x.ActualAmount),
                    CashAmount = models.Where(x => x.PaymentStatus == PaymentStatus.Paid && x.PaymentMethod == PaymentMethod.Cash).Sum(x => x.ActualAmount),
                    AlipayAmount = models.Where(x => x.PaymentStatus == PaymentStatus.Paid && x.PaymentMethod == PaymentMethod.Alipay).Sum(x => x.ActualAmount),
                    WeChatAmount = models.Where(x => x.PaymentStatus == PaymentStatus.Paid && x.PaymentMethod == PaymentMethod.WeChat).Sum(x => x.ActualAmount),
                    BankCardAmount = models.Where(x => x.PaymentStatus == PaymentStatus.Paid && x.PaymentMethod == PaymentMethod.BankCard).Sum(x => x.ActualAmount),
                    MedicalInsuranceAmount = models.Where(x => x.PaymentStatus == PaymentStatus.Paid && x.PaymentMethod == PaymentMethod.MedicalInsurance).Sum(x => x.ActualAmount),
                    RefundAmount = models.Where(x => x.PaymentStatus == PaymentStatus.Refunded).Sum(x => x.RefundAmount ?? 0)
                };

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取收费统计失败");
                throw;
            }
        }

        /// <summary>
        /// 生成发票号
        /// </summary>
        private string GenerateInvoiceNumber()
        {
            return $"INV{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }
    }
}