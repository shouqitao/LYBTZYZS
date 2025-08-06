using System.Threading.Tasks;
using System.Linq;
using System;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Module.Cashier.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Cashier;
using LYBT.Models.Cashier;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LYBT.Module.Cashier.Services
{
    /// <summary>
    /// 收银服务实现 - 核心业务方法
    /// </summary>
    public partial class CashierService : ICashierService
    {
        public async Task<CashierRecordDetailDto?> CreateAsync(CashierRecordCreateDto dto, Guid operatorId, string operatorName)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 创建收银记录
                var cashierRecord = new CashierRecord
                {
                    Id = Guid.NewGuid(),
                    MedicalCaseId = dto.MedicalCaseId,
                    PatientId = dto.PatientId,
                    CashierId = operatorId,
                    TotalAmount = dto.Items.Sum(i => i.UnitPrice * i.Quantity),
                    PaidAmount = dto.Payments.Sum(p => p.Amount),
                    ChangeAmount = dto.Payments.Sum(p => p.Amount) - dto.Items.Sum(i => i.UnitPrice * i.Quantity),
                    PaymentMethod = dto.Payments.Count == 1 ? dto.Payments[0].PaymentMethod : "混合支付",
                    Status = "已支付",
                    CreateTime = DateTime.Now,
                    InvoiceNumber = dto.PrintInvoice ? GenerateInvoiceNumber() : null,
                    Remark = dto.Remark
                };

                _context.CashierRecords.Add(cashierRecord);

                // 添加收银项目
                foreach (var itemDto in dto.Items)
                {
                    var item = new CashierItem
                    {
                        Id = Guid.NewGuid(),
                        CashierRecordId = cashierRecord.Id,
                        ItemType = itemDto.ItemType,
                        ItemName = itemDto.ItemName,
                        UnitPrice = itemDto.UnitPrice,
                        Quantity = itemDto.Quantity,
                        Amount = itemDto.UnitPrice * itemDto.Quantity,
                        SourceId = itemDto.SourceId,
                        SourceType = itemDto.SourceType,
                        Description = itemDto.Description
                    };
                    _context.CashierItems.Add(item);
                }

                // 添加支付记录
                foreach (var paymentDto in dto.Payments)
                {
                    var payment = new CashierPayment
                    {
                        Id = Guid.NewGuid(),
                        CashierRecordId = cashierRecord.Id,
                        PaymentMethod = paymentDto.PaymentMethod,
                        Amount = paymentDto.Amount,
                        TransactionId = paymentDto.TransactionId,
                        PaymentAccount = paymentDto.PaymentAccount,
                        PaymentTime = DateTime.Now,
                        Status = "成功"
                    };
                    _context.CashierPayments.Add(payment);
                }

                await _context.SaveChangesAsync();

                // 更新医疗案例状态为已缴费
                var medicalCase = await _context.MedicalCases.FirstOrDefaultAsync(mc => mc.Id == dto.MedicalCaseId);
                if (medicalCase != null)
                {
                    medicalCase.Status = "Paid";
                    await _context.SaveChangesAsync();
                }

                // 如果需要打印发票
                if (dto.PrintInvoice && !string.IsNullOrEmpty(cashierRecord.InvoiceNumber))
                {
                    await CreateInvoiceAsync(cashierRecord.Id, operatorId, operatorName);
                }

                await transaction.CommitAsync();

                _logger.LogInformation("创建收银记录成功 - 记录ID: {RecordId}, 患者ID: {PatientId}, 总金额: {TotalAmount}, 操作员: {Operator}",
                    cashierRecord.Id, dto.PatientId, cashierRecord.TotalAmount, operatorName);

                return await GetByIdAsync(cashierRecord.Id);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> RefundAsync(RefundRequestDto dto, Guid operatorId, string operatorName)
        {
            var cashierRecord = await _context.CashierRecords
                .Include(cr => cr.Payments)
                .FirstOrDefaultAsync(cr => cr.Id == dto.CashierRecordId);

            if (cashierRecord == null)
                return false;

            if (cashierRecord.Status == "已退费" || cashierRecord.RefundAmount > 0)
                throw new InvalidOperationException("该收银记录已经退费");

            if (dto.RefundAmount > cashierRecord.PaidAmount)
                throw new InvalidOperationException("退费金额不能超过实付金额");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 更新收银记录
                cashierRecord.Status = dto.RefundAmount >= cashierRecord.PaidAmount ? "已退费" : "部分退费";
                cashierRecord.RefundAmount = dto.RefundAmount;
                cashierRecord.RefundReason = dto.RefundReason;
                cashierRecord.RefundTime = DateTime.Now;
                cashierRecord.RefundOperator = operatorName;
                cashierRecord.UpdateTime = DateTime.Now;

                // 更新支付记录状态
                foreach (var payment in cashierRecord.Payments)
                {
                    payment.Status = "退款";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("退费处理成功 - 记录ID: {RecordId}, 退费金额: {RefundAmount}, 操作员: {Operator}",
                    dto.CashierRecordId, dto.RefundAmount, operatorName);

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<CashierRecordDetailDto?> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            var cashierRecord = await _context.CashierRecords
                .Include(cr => cr.Items)
                .Include(cr => cr.Payments)
                .FirstOrDefaultAsync(cr => cr.MedicalCaseId == medicalCaseId);

            if (cashierRecord == null)
                return null;

            return await GetByIdAsync(cashierRecord.Id);
        }

        public async Task<List<CashierRecordDto>> GetByPatientIdAsync(Guid patientId)
        {
            var cashierRecords = await _context.CashierRecords
                .Include(cr => cr.Items)
                .Include(cr => cr.Payments)
                .Where(cr => cr.PatientId == patientId)
                .OrderByDescending(cr => cr.CreateTime)
                .ToListAsync();

            var dtos = _mapper.Map<List<CashierRecordDto>>(cashierRecords);

            // 获取患者和收银员信息
            var cashierIds = cashierRecords.Select(cr => cr.CashierId).Distinct().ToList();
            var cashiers = await _context.Users
                .Where(u => cashierIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u);

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == patientId);

            foreach (var dto in dtos)
            {
                var record = cashierRecords.First(cr => cr.Id == dto.Id);
                dto.PatientName = patient?.Name ?? "未知患者";
                dto.CashierName = cashiers.GetValueOrDefault(record.CashierId)?.RealName ?? "未知收银员";
            }

            return dtos;
        }

        public async Task<List<CashierRecordDto>> GetByCashierIdAsync(Guid cashierId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var queryable = _context.CashierRecords
                .Include(cr => cr.Items)
                .Include(cr => cr.Payments)
                .Where(cr => cr.CashierId == cashierId);

            if (startDate.HasValue)
                queryable = queryable.Where(cr => cr.CreateTime >= startDate.Value);

            if (endDate.HasValue)
                queryable = queryable.Where(cr => cr.CreateTime <= endDate.Value);

            var cashierRecords = await queryable
                .OrderByDescending(cr => cr.CreateTime)
                .ToListAsync();

            var dtos = _mapper.Map<List<CashierRecordDto>>(cashierRecords);

            // 批量获取患者信息
            var patientIds = cashierRecords.Select(cr => cr.PatientId).Distinct().ToList();
            var patients = await _context.Patients
                .Where(p => patientIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p);

            var cashier = await _context.Users.FirstOrDefaultAsync(u => u.Id == cashierId);

            foreach (var dto in dtos)
            {
                var record = cashierRecords.First(cr => cr.Id == dto.Id);
                dto.PatientName = patients.GetValueOrDefault(record.PatientId)?.Name ?? "未知患者";
                dto.CashierName = cashier?.RealName ?? "未知收银员";
            }

            return dtos;
        }

        public async Task<CashierStatisticsDto> GetStatisticsAsync(DateTime startDate, DateTime endDate, Guid? cashierId = null)
        {
            var queryable = _context.CashierRecords
                .Include(cr => cr.Items)
                .Include(cr => cr.Payments)
                .Where(cr => cr.CreateTime >= startDate && cr.CreateTime <= endDate);

            if (cashierId.HasValue)
                queryable = queryable.Where(cr => cr.CashierId == cashierId.Value);

            var records = await queryable.ToListAsync();

            var statistics = new CashierStatisticsDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalRecords = records.Count,
                TotalAmount = records.Where(r => r.Status == "已支付" || r.Status == "部分退费").Sum(r => r.TotalAmount),
                RefundAmount = records.Sum(r => r.RefundAmount),
                NetAmount = records.Where(r => r.Status == "已支付" || r.Status == "部分退费").Sum(r => r.TotalAmount) - records.Sum(r => r.RefundAmount)
            };

            // 支付方式统计
            statistics.PaymentMethodStats = records
                .Where(r => r.Status == "已支付" || r.Status == "部分退费")
                .GroupBy(r => r.PaymentMethod)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.PaidAmount));

            // 项目类型统计
            var allItems = records.SelectMany(r => r.Items).ToList();
            statistics.ItemTypeStats = allItems
                .GroupBy(i => i.ItemType)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));

            // 日期统计
            statistics.DailyStats = records
                .Where(r => r.Status == "已支付" || r.Status == "部分退费")
                .GroupBy(r => r.CreateTime.Date.ToString("yyyy-MM-dd"))
                .ToDictionary(g => g.Key, g => g.Count());

            return statistics;
        }

        // 私有辅助方法
        private string GenerateInvoiceNumber()
        {
            return $"FP{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }

        private async Task<Invoice?> CreateInvoiceAsync(Guid cashierRecordId, Guid operatorId, string operatorName)
        {
            var cashierRecord = await _context.CashierRecords
                .Include(cr => cr.Items)
                .FirstOrDefaultAsync(cr => cr.Id == cashierRecordId);

            if (cashierRecord == null || string.IsNullOrEmpty(cashierRecord.InvoiceNumber))
                return null;

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == cashierRecord.PatientId);

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                CashierRecordId = cashierRecordId,
                InvoiceNumber = cashierRecord.InvoiceNumber,
                InvoiceType = "普通发票",
                BuyerInfo = patient?.Name ?? "患者",
                SellerInfo = "凌隐宝堂中医诊所",
                TotalAmount = cashierRecord.TotalAmount,
                TaxAmount = 0, // 医疗服务通常免税
                IssueTime = DateTime.Now,
                Status = "正常",
                ItemsJson = JsonSerializer.Serialize(cashierRecord.Items.Select(i => new InvoiceItemDto
                {
                    ItemName = i.ItemName,
                    Specification = i.Description ?? "",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Amount = i.Amount,
                    TaxRate = 0,
                    TaxAmount = 0
                }).ToList())
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return invoice;
        }
    }
}