using AutoMapper;
using LYBT.Entities.Formulas;
using LYBT.Infrastructure.Services;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace LYBT.Module.Formulas.Services
{
    /// <summary>
    /// 验方服务 - 简化版，只包含基础CRUD
    /// OpenSpec: decouple-server-modules - 使用ICrossModuleQueryService替代IHerbRepository
    /// </summary>
    public class FormulaService : IFormulaService
    {
        private readonly IFormulaRepository _repository;
        private readonly ICrossModuleQueryService _crossModuleQuery;
        private readonly IMapper _mapper;
        private readonly ILogger<FormulaService> _logger;

        public FormulaService(
            IFormulaRepository repository,
            ICrossModuleQueryService crossModuleQuery,
            IMapper mapper,
            ILogger<FormulaService> logger)
        {
            _repository = repository;
            _crossModuleQuery = crossModuleQuery;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PagedResult<FormulaListDto>>> GetPagedAsync(
            int page = 1,
            int pageSize = 20,
            string? keyword = null,
            string? category = null,
            Guid? currentUserId = null,
            bool isAdmin = false)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // 使用优化后的查询方法，包含Herbs集合
            var pagedResult = await _repository.GetPagedWithDetailsAsync(page, pageSize, keyword);

            // optimize-api-permissions: 应用角色过滤
            // Admin/SuperAdmin可以看到所有Formula
            // Doctor只能看到自己创建的或共享的Formula
            var filteredItems = pagedResult.Items.AsEnumerable();

            if (!isAdmin && currentUserId.HasValue)
            {
                // 过滤条件：用户可以看到:
                // 1. UserId匹配的验方（优先）
                // 2. CreatedBy匹配的验方（当UserId为NULL时的回退）
                // 3. IsShared=true的共享验方
                filteredItems = filteredItems.Where(f =>
                    f.UserId == currentUserId.Value ||
                    f.CreatedBy == currentUserId.Value ||
                    f.IsShared);

                _logger.LogDebug(
                    "应用角色过滤: UserId={UserId}, 原数量={OriginalCount}",
                    currentUserId.Value, pagedResult.Items.Count);
            }

            // Issue #1164: 应用分类筛选（MVP阶段内存过滤，Formula实体有Category字段）
            if (!string.IsNullOrWhiteSpace(category))
            {
                filteredItems = filteredItems.Where(f =>
                    !string.IsNullOrEmpty(f.Category) &&
                    f.Category.Contains(category, StringComparison.OrdinalIgnoreCase));
            }

            var filteredList = filteredItems.ToList();

            // 注意: 当应用了角色过滤或分类过滤时，TotalCount需要更新
            var needsRecalculateTotal = (!isAdmin && currentUserId.HasValue) || !string.IsNullOrWhiteSpace(category);

            // AutoMapper配置: HerbCount从Herbs.Count映射，TotalPrice标记为Ignore（无法计算）
            var items = _mapper.Map<List<FormulaListDto>>(filteredList);

            var dto = new PagedResult<FormulaListDto>
            {
                Items = items,
                TotalCount = needsRecalculateTotal ? filteredList.Count : pagedResult.TotalCount,
                CurrentPage = pagedResult.CurrentPage,
                PageSize = pagedResult.PageSize
            };
            return Result<PagedResult<FormulaListDto>>.Success(dto);
        }

        public async Task<Result<FormulaDetailDto>> GetByIdAsync(Guid id)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // 使用优化后的查询方法，包含所有药材配伍
            var entity = await _repository.GetByIdWithHerbsAsync(id);
            if (entity == null)
                return Result<FormulaDetailDto>.Failure("验方不存在");

            var dto = _mapper.Map<FormulaDetailDto>(entity);
            return Result<FormulaDetailDto>.Success(dto);
        }

        public async Task<Result<FormulaDetailDto>> CreateAsync(FormulaInputDto dto, Guid? creatorId = null)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // Issue #2014: 手动创建entity（不依赖AutoMapper处理Herbs集合）
            // OpenSpec: implement-formula-copy-flow - 设置UserId用于所有权过滤
            var entity = new Formula
            {
                Name = dto.Name,
                Effect = dto.Effect,
                Indication = dto.Indications, // Issue #2014: DTO.Indications → Entity.Indication
                Usage = dto.Usage,
                Remark = dto.Remark,
                Property = dto.Property,
                Category = dto.Category,
                FormulaType = FormulaType.Experience, // 默认经验方（DTO暂无此字段）
                IsShared = dto.IsShared,
                Status = CommonStatus.Enabled,
                ValidationStatus = FormulaValidationStatus.Draft,
                UserId = creatorId, // OpenSpec: implement-formula-copy-flow - 设置创建者ID
                Herbs = dto.Herbs?.Select(h => new FormulaHerbItem
                {
                    HerbId = h.HerbId,
                    HerbName = h.HerbName,
                    Dosage = (int)h.Dosage, // decimal → int
                    Unit = h.Unit,
                    ProcessingMethod = h.ProcessingMethod ?? h.Preparation, // 优先使用ProcessingMethod，回退到Preparation
                    Usage = h.Usage,
                    DecocteMethod = h.DecocteMethod,
                    OriginalHerbName = h.HerbName, // 保存原始名称用于延迟绑定
                    IsValidated = h.HerbId.HasValue // HerbId有值则标记为已验证
                }).ToList() ?? new List<FormulaHerbItem>()
            };

            var result = await _repository.AddAsync(entity);
            var resultDto = _mapper.Map<FormulaDetailDto>(result);
            return Result<FormulaDetailDto>.Success(resultDto);
        }

        public async Task<Result<FormulaDetailDto>> UpdateAsync(Guid id, FormulaInputDto dto)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // Issue #2014: 使用GetByIdWithHerbsAsync（包含Herbs集合）
            var entity = await _repository.GetByIdWithHerbsAsync(id);
            if (entity == null)
                return Result<FormulaDetailDto>.Failure("验方不存在");

            // Issue #2014: 手动更新基础字段（包括新增的Indication）
            entity.Name = dto.Name;
            entity.Effect = dto.Effect;
            entity.Indication = dto.Indications; // Issue #2014: DTO.Indications → Entity.Indication
            entity.Usage = dto.Usage;
            entity.Remark = dto.Remark;
            entity.Property = dto.Property;
            entity.Category = dto.Category;
            // FormulaType保持现有值（DTO暂无此字段）
            entity.IsShared = dto.IsShared;

            // Issue #2014: 粗粒度全量替换Herbs（Formula-Design-Decision-002）
            // 优势：匹配用户工作流（Excel批量保存）、DDD模式、性能可接受
            entity.Herbs.Clear();
            if (dto.Herbs != null && dto.Herbs.Any())
            {
                foreach (var h in dto.Herbs)
                {
                    entity.Herbs.Add(new FormulaHerbItem
                    {
                        HerbId = h.HerbId,
                        HerbName = h.HerbName,
                        Dosage = (int)h.Dosage, // decimal → int
                        Unit = h.Unit,
                        ProcessingMethod = h.ProcessingMethod ?? h.Preparation, // 优先使用ProcessingMethod
                        Usage = h.Usage,
                        DecocteMethod = h.DecocteMethod,
                        OriginalHerbName = h.HerbName, // 保存原始名称
                        IsValidated = h.HerbId.HasValue // HerbId有值则标记为已验证
                    });
                }
            }

            var result = await _repository.UpdateAsync(entity);
            var resultDto = _mapper.Map<FormulaDetailDto>(result);
            return Result<FormulaDetailDto>.Success(resultDto);
        }

        public async Task<Result<List<FormulaDetailDto>>> SearchAsync(string keyword)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // 简化搜索逻辑 - 直接使用分页查询，取前100个结果
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Result<List<FormulaDetailDto>>.Success(new List<FormulaDetailDto>());
            }

            var pagedResult = await _repository.GetPagedWithDetailsAsync(1, 100, keyword);
            var formulaDtos = _mapper.Map<List<FormulaDetailDto>>(pagedResult.Items);

            return Result<List<FormulaDetailDto>>.Success(formulaDtos);
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var result = await _repository.DeleteAsync(id);
            return result ? Result.Success() : Result.Failure("删除失败");
        }


        /// <summary>
        /// 验证验方药材 - 手动绑定药材到系统药材库 (Issue #1348)
        /// </summary>
        public async Task<Result> ValidateFormulaHerbAsync(Guid formulaId, Guid herbItemId, Guid selectedHerbId)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // 1. 查询验方（包含所有药材）
            var formula = await _repository.GetByIdWithHerbsAsync(formulaId);
            if (formula == null)
            {
                return Result.Failure("验方不存在");
            }

            // 2. 查找待验证的药材项
            var herbItem = formula.Herbs.FirstOrDefault(h => h.Id == herbItemId);
            if (herbItem == null)
            {
                return Result.Failure("药材项不存在");
            }

            // 3. 验证是否已校验
            if (herbItem.IsValidated)
            {
                return Result.Failure("该药材已校验，无需重复操作");
            }

            // 4. 查询选定的药材 - OpenSpec: decouple-server-modules 使用ICrossModuleQueryService
            var selectedHerb = await _crossModuleQuery.GetHerbBasicInfoAsync(selectedHerbId);
            if (selectedHerb == null)
            {
                return Result.Failure("所选药材不存在");
            }

            // 5. 更新药材项的验证信息
            herbItem.HerbId = selectedHerbId;
            herbItem.HerbName = selectedHerb.Name;
            herbItem.IsValidated = true;

            // 6. 检查该验方的所有药材是否都已验证
            bool allValidated = formula.Herbs.All(h => h.IsValidated);
            if (allValidated)
            {
                // 所有药材都已验证，更新验方状态
                formula.ValidationStatus = FormulaValidationStatus.Validated;
                _logger.LogInformation("验方 {FormulaId} 所有药材已验证，状态更新为Validated", formulaId);
            }

            // 7. 保存变更
            await _repository.UpdateAsync(formula);
            await _repository.SaveChangesAsync();

            // 8. 返回成功（详细消息通过日志记录）
            if (allValidated)
            {
                _logger.LogInformation("药材\"{OriginalHerbName}\"已映射为\"{HerbName}\"，验方\"{FormulaName}\"所有药材已校验完成",
                    herbItem.OriginalHerbName, selectedHerb.Name, formula.Name);
            }
            else
            {
                _logger.LogInformation("药材\"{OriginalHerbName}\"已映射为\"{HerbName}\"",
                    herbItem.OriginalHerbName, selectedHerb.Name);
            }
            return Result.Success();
        }


        /// <summary>
        /// 获取待验证的验方列表 (Issue #1349)
        /// 查询所有 ValidationStatus = Draft 的验方，包含未验证的药材项
        /// </summary>
        public async Task<Result<List<FormulaDetailDto>>> GetPendingValidationFormulasAsync()
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // 查询所有Draft状态的验方（使用GetAllAsync预加载Herbs避免N+1查询）
            var allFormulas = await _repository.GetAllAsync();

            // 过滤出Draft状态的验方
            var pendingFormulas = allFormulas
                .Where(f => f.ValidationStatus == FormulaValidationStatus.Draft)
                .ToList();

            // 映射为DTO
            var formulaDtos = _mapper.Map<List<FormulaDetailDto>>(pendingFormulas);

            _logger.LogInformation("查询到 {Count} 个待验证验方", formulaDtos.Count);
            return Result<List<FormulaDetailDto>>.Success(formulaDtos);
        }

        /// <summary>
        /// 从结构化数据导入验方 (Issue #1758: 架构重构 - Server端不再依赖Excel格式)
        /// Client端负责Excel解析，Server端只处理业务逻辑
        /// </summary>
        /// <param name="formulas">已解析的验方列表（由Client端从Excel解析）</param>
        /// <param name="fileName">原始文件名（用于日志记录）</param>
        public async Task<Result<FormulaBatchImportResultDto>> ImportFromDataAsync(List<FormulaImportItemDto> formulas, string? fileName = null)
        {
            // eliminate-service-catch-return: 移除外层冗余try-catch，异常由IExceptionHandler统一处理
            var result = new FormulaBatchImportResultDto
            {
                FileName = fileName,
                ImportTime = DateTime.Now,
                StartTime = DateTime.Now,
                TotalCount = formulas.Count
            };

            // 逐个导入验方
            int index = 0;
            foreach (var formulaImportItem in formulas)
            {
                index++;
                try
                {
                    if (string.IsNullOrWhiteSpace(formulaImportItem.Name))
                    {
                        result.FailureCount++;
                        result.Failures.Add(new FormulaImportFailureDto
                        {
                            RowIndex = index,
                            FormulaName = formulaImportItem.Name ?? string.Empty,
                            ErrorMessage = "验方名称不能为空"
                        });
                        continue;
                    }

                    // 创建验方实体（从DTO映射）
                    var formula = new Formula
                    {
                        Name = formulaImportItem.Name,
                        Effect = formulaImportItem.Effect,
                        Usage = formulaImportItem.Usage,
                        Property = formulaImportItem.Property,
                        IsShared = formulaImportItem.IsShared,
                        Remark = formulaImportItem.Remark,
                        // Note: Indications, Contraindications, Preparation, Source exist in DTO but not in Entity
                        Status = CommonStatus.Enabled,
                        ValidationStatus = FormulaValidationStatus.Draft, // 导入的验方初始为Draft
                        CreatedAt = DateTime.Now,
                        Herbs = new List<FormulaHerbItem>()
                    };

                    // 添加药材（从DTO列表）
                    foreach (var herbDto in formulaImportItem.Herbs)
                    {
                        // 尝试自动匹配药材
                        var matchedHerb = await TryMatchHerbAsync(herbDto.HerbName);

                        formula.Herbs.Add(new FormulaHerbItem
                        {
                            Id = Guid.NewGuid(),
                            HerbId = matchedHerb?.Id,
                            HerbName = herbDto.HerbName,
                            OriginalHerbName = herbDto.HerbName, // 保存原始名称
                            IsValidated = matchedHerb != null, // 成功匹配则标记为已验证
                            Dosage = (int)herbDto.Dosage, // DTO是decimal，实体是int
                            Unit = herbDto.Unit ?? string.Empty,
                            Usage = herbDto.Usage,
                            ProcessingMethod = herbDto.Preparation // DTO的Preparation映射到ProcessingMethod
                            // Note: SortOrder exists in DTO but not in Entity
                        });

                        // 统计药材匹配情况
                        if (matchedHerb != null)
                        {
                            result.MatchedHerbsCount++;
                        }
                        else
                        {
                            result.UnmatchedHerbsCount++;
                        }
                    }

                    // 自动判断验证状态：如果所有药材都已验证，则标记为Validated
                    if (formula.Herbs.Any() && formula.Herbs.All(h => h.IsValidated))
                    {
                        formula.ValidationStatus = FormulaValidationStatus.Validated;
                    }

                    var savedFormula = await _repository.AddAsync(formula);
                    var formulaResultDto = _mapper.Map<FormulaDetailDto>(savedFormula);

                    result.SuccessCount++;
                    result.SuccessfulIds.Add(savedFormula.Id);
                    result.SuccessfulFormulas.Add(formulaResultDto);
                }
                catch (Exception ex)
                {
                    // 保留项级错误隔离，ERR-012: 使用安全错误消息
                    result.FailureCount++;
                    result.Failures.Add(new FormulaImportFailureDto
                    {
                        RowIndex = index,
                        FormulaName = formulaImportItem.Name ?? string.Empty,
                        ErrorMessage = "数据处理异常",
                        ErrorDetails = null // ERR-012: 不暴露堆栈信息
                    });
                    _logger.LogError(ex, "导入验方 {FormulaName} 时发生错误", formulaImportItem.Name);
                }
            }

            result.EndTime = DateTime.Now;
            result.IsSuccess = true;
            result.Message = $"导入完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条，药材匹配 {result.MatchedHerbsCount} 个，未匹配 {result.UnmatchedHerbsCount} 个";

            return Result<FormulaBatchImportResultDto>.Success(result);
        }

        // Issue #1758: ParseHerbItems方法已移至Client端 ExcelParseHelper

        /// <summary>
        /// 尝试自动匹配药材（按名称或拼音码）
        /// </summary>
        /// <summary>
        /// 尝试匹配药材 - OpenSpec: decouple-server-modules 使用ICrossModuleQueryService
        /// 返回HerbBasicDto用于只读信息，不再返回完整Entity
        /// </summary>
        private async Task<HerbBasicDto?> TryMatchHerbAsync(string herbName)
        {
            if (string.IsNullOrWhiteSpace(herbName))
                return null;

            try
            {
                // Issue #1469 (FORMULA-8): 使用智能药材匹配
                // 优先精确匹配名称，其次模糊匹配拼音码
                // OpenSpec: decouple-server-modules - 使用ICrossModuleQueryService替代IHerbRepository
                var herb = await _crossModuleQuery.GetHerbByNameOrPinyinAsync(herbName);
                return herb;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "药材匹配失败：{HerbName}", herbName);
                return null;
            }
        }

        // Issue #1758: HerbItemData类已移至Client端 ExcelParseHelper

        /// <summary>
        /// 导出验方数据到Excel (Issue #1166)
        /// </summary>
        public async Task<MemoryStream> ExportAsync(string? category = null)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var formulas = (await _repository.GetAllAsync()).ToList();

            // 应用分类筛选
            if (!string.IsNullOrWhiteSpace(category))
            {
                formulas = formulas.Where(f =>
                    !string.IsNullOrEmpty(f.Category) &&
                    f.Category.Contains(category, StringComparison.OrdinalIgnoreCase))
                .ToList();
            }

            var stream = new MemoryStream();
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.Add("验方列表");

                // 表头
                worksheet.Cells[1, 1].Value = "验方名称";
                worksheet.Cells[1, 2].Value = "分类";
                worksheet.Cells[1, 3].Value = "功效";
                worksheet.Cells[1, 4].Value = "用法";
                worksheet.Cells[1, 5].Value = "性味归经";
                worksheet.Cells[1, 6].Value = "方剂类型";
                worksheet.Cells[1, 7].Value = "是否共享";
                worksheet.Cells[1, 8].Value = "备注";

                using (var range = worksheet.Cells[1, 1, 1, 8])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // 数据行
                for (int i = 0; i < formulas.Count; i++)
                {
                    var formula = formulas[i];
                    int row = i + 2;

                    worksheet.Cells[row, 1].Value = formula.Name;
                    worksheet.Cells[row, 2].Value = formula.Category;
                    worksheet.Cells[row, 3].Value = formula.Effect;
                    worksheet.Cells[row, 4].Value = formula.Usage;
                    worksheet.Cells[row, 5].Value = formula.Property;
                    worksheet.Cells[row, 6].Value = formula.FormulaType == FormulaType.Classic ? "经典方" : "经验方";
                    worksheet.Cells[row, 7].Value = formula.IsShared ? "是" : "否";
                    worksheet.Cells[row, 8].Value = formula.Remark;
                }

                worksheet.Cells.AutoFitColumns();
                package.Save();
            }

            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// 生成验方导入模板 (Issue #1347: 更新为主-从表格式)
        /// 格式：Sheet1=验方信息，Sheet2=药材明细
        /// </summary>
        public MemoryStream GenerateImportTemplate()
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var stream = new MemoryStream();
            using (var package = new ExcelPackage(stream))
            {
                // Sheet1：验方信息
                var formulaSheet = package.Workbook.Worksheets.Add("验方信息");

                // 表头
                formulaSheet.Cells[1, 1].Value = "验方编号*";
                formulaSheet.Cells[1, 2].Value = "验方名称*";
                formulaSheet.Cells[1, 3].Value = "分类";
                formulaSheet.Cells[1, 4].Value = "功效";
                formulaSheet.Cells[1, 5].Value = "用法";
                formulaSheet.Cells[1, 6].Value = "性味归经";
                formulaSheet.Cells[1, 7].Value = "方剂类型";
                formulaSheet.Cells[1, 8].Value = "是否共享";
                formulaSheet.Cells[1, 9].Value = "备注";

                using (var range = formulaSheet.Cells[1, 1, 1, 9])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // 示例数据
                formulaSheet.Cells[2, 1].Value = "F001";
                formulaSheet.Cells[2, 2].Value = "小柴胡汤";
                formulaSheet.Cells[2, 3].Value = "和解剂";
                formulaSheet.Cells[2, 4].Value = "和解少阳，扶正祛邪";
                formulaSheet.Cells[2, 5].Value = "水煎服，日三次";
                formulaSheet.Cells[2, 6].Value = "性平，归肝、胆经";
                formulaSheet.Cells[2, 7].Value = "经典方";
                formulaSheet.Cells[2, 8].Value = "是";
                formulaSheet.Cells[2, 9].Value = "《伤寒论》经典名方";

                formulaSheet.Cells.AutoFitColumns();

                // Sheet2：药材明细
                var herbSheet = package.Workbook.Worksheets.Add("药材明细");

                // 表头
                herbSheet.Cells[1, 1].Value = "验方编号*";
                herbSheet.Cells[1, 2].Value = "药材名称*";
                herbSheet.Cells[1, 3].Value = "剂量*";
                herbSheet.Cells[1, 4].Value = "单位";
                herbSheet.Cells[1, 5].Value = "用法";
                herbSheet.Cells[1, 6].Value = "炮制方法";
                herbSheet.Cells[1, 7].Value = "备注";

                using (var range = herbSheet.Cells[1, 1, 1, 7])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // 示例数据
                herbSheet.Cells[2, 1].Value = "F001";
                herbSheet.Cells[2, 2].Value = "柴胡";
                herbSheet.Cells[2, 3].Value = "24";
                herbSheet.Cells[2, 4].Value = "g";
                herbSheet.Cells[2, 5].Value = "";
                herbSheet.Cells[2, 6].Value = "";
                herbSheet.Cells[2, 7].Value = "";

                herbSheet.Cells[3, 1].Value = "F001";
                herbSheet.Cells[3, 2].Value = "黄芩";
                herbSheet.Cells[3, 3].Value = "9";
                herbSheet.Cells[3, 4].Value = "g";
                herbSheet.Cells[3, 5].Value = "";
                herbSheet.Cells[3, 6].Value = "";
                herbSheet.Cells[3, 7].Value = "";

                herbSheet.Cells[4, 1].Value = "F001";
                herbSheet.Cells[4, 2].Value = "半夏";
                herbSheet.Cells[4, 3].Value = "12";
                herbSheet.Cells[4, 4].Value = "g";
                herbSheet.Cells[4, 5].Value = "";
                herbSheet.Cells[4, 6].Value = "";
                herbSheet.Cells[4, 7].Value = "";

                herbSheet.Cells.AutoFitColumns();

                package.Save();
            }

            stream.Position = 0;
            return stream;
        }

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复方法实现 ==========

        /// <summary>
        /// 切换验方状态（启用/禁用）
        /// </summary>
        public async Task<Result<FormulaDetailDto>> ToggleStatusAsync(Guid id)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                return Result<FormulaDetailDto>.Failure("验方不存在");
            }

            // 切换状态
            entity.Status = entity.Status == CommonStatus.Enabled
                ? CommonStatus.Disabled
                : CommonStatus.Enabled;
            entity.UpdatedAt = DateTime.Now;

            var result = await _repository.UpdateAsync(entity);
            var dto = _mapper.Map<FormulaDetailDto>(result);

            _logger.LogInformation("验方状态已切换: {FormulaId}, 新状态: {Status}", id, entity.Status);

            return Result<FormulaDetailDto>.Success(dto);
        }

        /// <summary>
        /// 恢复软删除的验方
        /// </summary>
        public async Task<Result<FormulaDetailDto>> RestoreAsync(Guid id)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // 使用GetByIdIncludingDeletedAsync获取包括已删除的实体
            var entity = await _repository.GetByIdIncludingDeletedAsync(id);
            if (entity == null)
            {
                return Result<FormulaDetailDto>.Failure("验方不存在");
            }

            if (!entity.IsDeleted)
            {
                return Result<FormulaDetailDto>.Failure("该验方未被删除，无需恢复");
            }

            // 恢复软删除
            entity.IsDeleted = false;
            entity.UpdatedAt = DateTime.Now;

            var result = await _repository.UpdateAsync(entity);
            var dto = _mapper.Map<FormulaDetailDto>(result);

            _logger.LogInformation("验方已恢复: {FormulaId}, {FormulaName}", id, entity.Name);

            return Result<FormulaDetailDto>.Success(dto);
        }

        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <summary>
        /// 批量删除验方
        /// </summary>
        public async Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids)
        {
            var result = new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                SuccessCount = 0,
                FailureCount = 0
            };

            foreach (var id in ids)
            {
                try
                {
                    var entity = await _repository.GetByIdAsync(id);
                    if (entity == null)
                    {
                        result.FailureCount++;
                        result.FailedIds.Add(id);
                        result.FailedItems.Add(new BatchOperationFailureItem
                        {
                            Id = id,
                            Reason = "方剂不存在"
                        });
                        continue;
                    }

                    // 软删除
                    entity.IsDeleted = true;
                    entity.UpdatedAt = DateTime.Now;
                    await _repository.UpdateAsync(entity);

                    result.SuccessCount++;
                    result.SuccessfulIds.Add(id);
                    _logger.LogInformation("批量删除 - 方剂已删除: {FormulaId}, {FormulaName}", id, entity.Name);
                }
                catch (Exception ex)
                {
                    // 保留项级错误隔离，ERR-012: 使用安全错误消息
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = "删除操作失败"
                    });
                    _logger.LogError(ex, "批量删除 - 删除方剂失败: {FormulaId}", id);
                }
            }

            result.IsSuccess = result.SuccessCount > 0;
            result.Message = $"批量删除完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条";

            return Result<BatchOperationResultDto>.Success(result);
        }

        /// <summary>
        /// 批量更新方剂状态
        /// </summary>
        public async Task<Result<BatchOperationResultDto>> BatchUpdateStatusAsync(List<Guid> ids, CommonStatus status)
        {
            var result = new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                SuccessCount = 0,
                FailureCount = 0
            };

            var statusText = status == CommonStatus.Enabled ? "启用" : "禁用";

            foreach (var id in ids)
            {
                try
                {
                    var formula = await _repository.GetByIdAsync(id);
                    if (formula == null || formula.IsDeleted)
                    {
                        result.FailureCount++;
                        result.FailedIds.Add(id);
                        result.FailedItems.Add(new BatchOperationFailureItem
                        {
                            Id = id,
                            Reason = "方剂不存在"
                        });
                        continue;
                    }

                    formula.Status = status;
                    formula.UpdatedAt = DateTime.Now;
                    await _repository.UpdateAsync(formula);

                    result.SuccessCount++;
                    result.SuccessfulIds.Add(id);
                    _logger.LogInformation("批量{StatusText} - 方剂状态已更新: {FormulaId}, {FormulaName}", statusText, id, formula.Name);
                }
                catch (Exception ex)
                {
                    // 保留项级错误隔离，ERR-012: 使用安全错误消息
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = "状态更新失败"
                    });
                    _logger.LogError(ex, "批量{StatusText} - 更新方剂状态失败: {FormulaId}", statusText, id);
                }
            }

            await _repository.SaveChangesAsync();

            result.IsSuccess = result.SuccessCount > 0;
            result.Message = $"批量{statusText}完成: 成功 {result.SuccessCount} 个, 失败 {result.FailureCount} 个";

            return Result<BatchOperationResultDto>.Success(result);
        }
    }
}
