using AutoMapper;
using LYBT.Entities.Compatibility;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Compatibility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Services
{
    /// <summary>
    /// 配伍记录服务实现 - Record-Only模式已移除复杂配伍检查逻辑
    /// </summary>
    [Obsolete("Compatibility checking feature removed in Record-Only mode. Use manual notes instead.", false)]
    public class CompatibilityNoteService : ICompatibilityNoteService
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

        /// <inheritdoc/>
        [Obsolete("Compatibility checking feature removed in Record-Only mode. Use manual notes instead.", false)]
        public Task<ServiceResult<CompatibilityNoteDto>> CreateAsync(
            Guid prescriptionId,
            CompatibilityNoteCreateDto createDto,
            Guid currentUserId)
        {
            var emptyNote = new CompatibilityNoteDto
            {
                Id = Guid.NewGuid(),
                PrescriptionId = prescriptionId,
                HerbCombination = createDto.HerbCombination,
                CompatibilityType = createDto.CompatibilityType,
                SeverityLevel = createDto.SeverityLevel,
                CompatibilityNote = "配伍检查功能在 Record-Only 模式下已移除",
                DoctorRecommendation = "请手动记录配伍注意事项"
            };

            return Task.FromResult(ServiceResult<CompatibilityNoteDto>.Success(
                emptyNote,
                "配伍检查功能已在 Record-Only 模式下移除，请使用手动记录"));
        }

        /// <inheritdoc/>
        [Obsolete("Compatibility checking feature removed in Record-Only mode. Use manual notes instead.", false)]
        public Task<ServiceResult<List<CompatibilityNoteDto>>> GetByPrescriptionIdAsync(Guid prescriptionId)
        {
            var emptyList = new List<CompatibilityNoteDto>();
            return Task.FromResult(ServiceResult<List<CompatibilityNoteDto>>.Success(
                emptyList,
                "配伍检查功能已在 Record-Only 模式下移除"));
        }

        /// <inheritdoc/>
        [Obsolete("Compatibility checking feature removed in Record-Only mode. Use manual notes instead.", false)]
        public Task<ServiceResult<CompatibilityNoteDto>> UpdateAsync(
            Guid prescriptionId,
            Guid noteId,
            CompatibilityNoteUpdateDto updateDto,
            Guid currentUserId)
        {
            return Task.FromResult(ServiceResult<CompatibilityNoteDto>.Failure(
                "配伍检查功能已在 Record-Only 模式下移除，无法更新记录"));
        }

        /// <inheritdoc/>
        [Obsolete("Compatibility checking feature removed in Record-Only mode. Use manual notes instead.", false)]
        public Task<ServiceResult<bool>> DeleteAsync(Guid prescriptionId, Guid noteId, Guid currentUserId)
        {
            return Task.FromResult(ServiceResult<bool>.Failure(
                "配伍检查功能已在 Record-Only 模式下移除，无法删除记录"));
        }

        /// <inheritdoc/>
        [Obsolete("Compatibility checking feature removed in Record-Only mode. Use manual notes instead.", false)]
        public Task<ServiceResult<CompatibilityNoteDto>> GetByIdAsync(Guid prescriptionId, Guid noteId)
        {
            return Task.FromResult(ServiceResult<CompatibilityNoteDto>.Failure(
                "配伍检查功能已在 Record-Only 模式下移除"));
        }
    }
}
