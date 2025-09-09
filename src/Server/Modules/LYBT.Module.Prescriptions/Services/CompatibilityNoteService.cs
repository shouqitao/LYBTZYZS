using AutoMapper;
using LYBT.Entities.Compatibility;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Compatibility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Services
{
    /// <summary>
    /// 配伍记录服务实现
    /// </summary>
    public class CompatibilityNoteService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<CompatibilityNoteService> _logger;

        public CompatibilityNoteService(
            AppDbContext context,
            IMapper mapper,
            ILogger<CompatibilityNoteService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ServiceResult<CompatibilityNoteDto>> CreateAsync(
            Guid prescriptionId,
            CompatibilityNoteCreateDto createDto,
            Guid currentUserId)
        {
            try
            {
                // 验证处方存在性
                var prescriptionExists = await _context.Prescriptions
                    .AnyAsync(p => p.Id == prescriptionId);

                if (!prescriptionExists)
                {
                    return ServiceResult<CompatibilityNoteDto>.Failure("处方不存在");
                }

                // 创建实体
                var entity = _mapper.Map<HerbCompatibilityNote>(createDto);
                entity.PrescriptionId = prescriptionId;
                entity.CreatedBy = currentUserId;
                entity.CreateTime = DateTime.Now;

                _context.HerbCompatibilityNotes.Add(entity);
                await _context.SaveChangesAsync();

                var dto = _mapper.Map<CompatibilityNoteDto>(entity);
                return ServiceResult<CompatibilityNoteDto>.Success(dto, "配伍记录创建成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建配伍记录失败: {PrescriptionId}", prescriptionId);
                return ServiceResult<CompatibilityNoteDto>.Failure("创建配伍记录失败");
            }
        }

        public async Task<ServiceResult<List<CompatibilityNoteDto>>> GetByPrescriptionIdAsync(Guid prescriptionId)
        {
            try
            {
                var entities = await _context.HerbCompatibilityNotes
                    .Where(n => n.PrescriptionId == prescriptionId && !n.IsDeleted)
                    .OrderByDescending(n => n.CreateTime)
                    .ToListAsync();

                var dtos = _mapper.Map<List<CompatibilityNoteDto>>(entities);
                return ServiceResult<List<CompatibilityNoteDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询配伍记录失败: {PrescriptionId}", prescriptionId);
                return ServiceResult<List<CompatibilityNoteDto>>.Failure("查询配伍记录失败");
            }
        }

        public async Task<ServiceResult<CompatibilityNoteDto>> UpdateAsync(
            Guid prescriptionId,
            Guid noteId,
            CompatibilityNoteUpdateDto updateDto,
            Guid currentUserId)
        {
            try
            {
                var entity = await _context.HerbCompatibilityNotes
                    .FirstOrDefaultAsync(n => n.Id == noteId && n.PrescriptionId == prescriptionId && !n.IsDeleted);

                if (entity == null)
                {
                    return ServiceResult<CompatibilityNoteDto>.Failure("配伍记录不存在");
                }

                // 更新字段
                if (!string.IsNullOrEmpty(updateDto.CompatibilityNote))
                {
                    entity.CompatibilityNote = updateDto.CompatibilityNote;
                }

                if (!string.IsNullOrEmpty(updateDto.DoctorRecommendation))
                {
                    entity.DoctorRecommendation = updateDto.DoctorRecommendation;
                }

                entity.UpdateTime = DateTime.Now;

                await _context.SaveChangesAsync();

                var dto = _mapper.Map<CompatibilityNoteDto>(entity);
                return ServiceResult<CompatibilityNoteDto>.Success(dto, "配伍记录更新成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新配伍记录失败: {NoteId}", noteId);
                return ServiceResult<CompatibilityNoteDto>.Failure("更新配伍记录失败");
            }
        }

        public async Task<ServiceResult<bool>> DeleteAsync(Guid prescriptionId, Guid noteId, Guid currentUserId)
        {
            try
            {
                var entity = await _context.HerbCompatibilityNotes
                    .FirstOrDefaultAsync(n => n.Id == noteId && n.PrescriptionId == prescriptionId && !n.IsDeleted);

                if (entity == null)
                {
                    return ServiceResult<bool>.Failure("配伍记录不存在");
                }

                // 软删除
                entity.IsDeleted = true;
                entity.UpdateTime = DateTime.Now;

                await _context.SaveChangesAsync();

                return ServiceResult<bool>.Success(true, "配伍记录删除成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除配伍记录失败: {NoteId}", noteId);
                return ServiceResult<bool>.Failure("删除配伍记录失败");
            }
        }

        public async Task<ServiceResult<CompatibilityNoteDto>> GetByIdAsync(Guid prescriptionId, Guid noteId)
        {
            try
            {
                var entity = await _context.HerbCompatibilityNotes
                    .FirstOrDefaultAsync(n => n.Id == noteId && n.PrescriptionId == prescriptionId && !n.IsDeleted);

                if (entity == null)
                {
                    return ServiceResult<CompatibilityNoteDto>.Failure("配伍记录不存在");
                }

                var dto = _mapper.Map<CompatibilityNoteDto>(entity);
                return ServiceResult<CompatibilityNoteDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取配伍记录失败: {NoteId}", noteId);
                return ServiceResult<CompatibilityNoteDto>.Failure("获取配伍记录失败");
            }
        }
    }
}
