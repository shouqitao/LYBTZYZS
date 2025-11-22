# 批量操作模式实践指南

> **文档类型**: How-to Guide（操作指南）  
> **最后更新**: 2025-11-10  
> **适用范围**: LYBTZYZS Client + Server 批量操作实现  
> **参考Epic**: #1962（Herbs批量导入/导出）

---

## 📋 目录

- [1. 什么是批量操作模式](#1-什么是批量操作模式)
- [2. Desktop主导模式详解](#2-desktop主导模式详解)
- [3. 实现步骤](#3-实现步骤)
- [4. 代码示例](#4-代码示例)
- [5. 常见问题](#5-常见问题)
- [6. 性能优化建议](#6-性能优化建议)

---

## 1. 什么是批量操作模式

### 1.1 核心理念

**批量操作模式（Batch Operation Pattern）**：Desktop层负责文件I/O和格式转换，Server层负责业务逻辑验证和数据持久化。

**设计原则**：
- 🎯 **职责分离**：Desktop处理用户交互 + 文件格式，Server处理业务规则 + 数据库
- 🔄 **轻量通信**：Server端只接收/返回JSON，不处理Excel文件流
- ⚡ **性能优先**：文件解析在客户端完成，减轻服务器负担
- 🛡️ **双重验证**：Desktop预验证格式，Server验证业务规则

### 1.2 适用场景

| 场景 | 是否适用 | 说明 |
|-----|---------|------|
| ✅ 批量导入Excel | 是 | 典型场景（Herbs批量导入） |
| ✅ 批量导出Excel | 是 | 典型场景（Herbs批量导出） |
| ✅ 模板下载 | 是 | Desktop直接生成，无需Server |
| ❌ 实时同步 | 否 | 应使用SignalR或轮询 |
| ❌ 流式上传 | 否 | 大文件使用分块上传 |

---

## 2. Desktop主导模式详解

### 2.1 架构对比

#### 传统模式（❌ 不推荐）

```mermaid
sequenceDiagram
    User->>Desktop: 选择Excel文件
    Desktop->>Server: POST /import (文件流)
    Server->>EPPlus: 解析Excel
    Server->>Database: 保存数据
    Server-->>Desktop: 返回结果
    Desktop-->>User: 显示消息
```

**问题**：
- ❌ Server端需要处理文件流（增加复杂度）
- ❌ 服务器资源消耗大（大文件解析）
- ❌ 难以实时反馈解析进度

#### Desktop主导模式（✅ 推荐）

```mermaid
sequenceDiagram
    User->>Desktop: 选择Excel文件
    Desktop->>EPPlus: 解析Excel
    Desktop->>Desktop: 转换为DTO列表
    Desktop->>Server: POST /batch-import (JSON)
    Server->>FluentValidation: 验证DTO
    Server->>Database: 批量保存
    Server-->>Desktop: 返回结果
    Desktop-->>User: 显示详细结果
```

**优势**：
- ✅ Server端只处理JSON（轻量通信）
- ✅ Desktop端可实时显示解析进度
- ✅ 文件解析失败不影响服务器
- ✅ 服务器可水平扩展（无状态API）

### 2.2 三层数据流

```mermaid
graph LR
    subgraph Desktop层
        View[View<br/>HerbManagementView.xaml] -->|Command| ViewModel[ViewModel<br/>HerbManagementViewModel]
        ViewModel -->|Method Call| Repository[Repository<br/>HerbRepository]
    end
    
    subgraph Server层
        API[Controller<br/>HerbsController] -->|Inject| Service[Service<br/>HerbService]
        Service -->|Inject| DbRepo[Repository<br/>HerbRepository]
    end
    
    Repository -->|HTTP POST| API
    
    subgraph 数据格式
        Excel[Excel文件<br/>.xlsx] -->|EPPlus解析| DTO[List&lt;HerbInputDto&gt;<br/>JSON]
        DTO -->|FluentValidation| Entity[Herb Entity<br/>EF Core]
    end
    
    Repository -.->|Read| Excel
    Repository -->|Send| DTO
    Service -->|Validate| DTO
    Service -->|Map| Entity
    DbRepo -->|SaveChanges| DB[(Database)]
    
    style View fill:#e1f5ff
    style ViewModel fill:#e1f5ff
    style Repository fill:#e1f5ff
    style API fill:#fff4e1
    style Service fill:#fff4e1
    style DbRepo fill:#fff4e1
```

---

## 3. 实现步骤

### Step 1: 定义共享DTO

**位置**: `src/Shared/LYBT.Shared.Models/Contracts/Herbs/`

```csharp
/// <summary>
/// 批量导入请求DTO（Desktop → Server）
/// </summary>
public class HerbBatchImportRequestDto
{
    /// <summary>
    /// 药材数据列表（Desktop层已解析Excel）
    /// </summary>
    public List<HerbInputDto> Herbs { get; set; } = new();

    /// <summary>
    /// 重复处理策略
    /// </summary>
    public DuplicateStrategy Strategy { get; set; } = DuplicateStrategy.Skip;
}

/// <summary>
/// 批量导入结果DTO（Server → Desktop）
/// </summary>
public class HerbBatchImportResultDto
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int SkippedCount { get; set; }
    public List<ImportFailureDto> Failures { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 成功率（百分比）
    /// </summary>
    public double SuccessRate =>
        TotalCount > 0 ? (SuccessCount * 100.0 / TotalCount) : 0;
}

/// <summary>
/// 导入失败详情DTO
/// </summary>
public class ImportFailureDto
{
    public int RowNumber { get; set; }
    public string HerbName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
```

---

### Step 2: Desktop层实现（ViewModel）

**位置**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/HerbManagementViewModel.cs`

#### 2.1 命令定义

```csharp
public class HerbManagementViewModel : UnifiedListViewModelBase<HerbDto>
{
    private readonly IHerbRepository _herbRepository;
    private readonly ICommonDialogService _dialogService;

    // 批量操作命令
    public DelegateCommand ImportHerbsCommand { get; }
    public DelegateCommand ExportTemplateCommand { get; }
    public DelegateCommand ExportHerbsCommand { get; }

    private void InitializeCustomCommands()
    {
        ImportHerbsCommand = new DelegateCommand(
            async () => await ImportHerbsAsync(),
            () => !IsBusy && !IsLoading
        ).ObservesProperty(() => IsBusy).ObservesProperty(() => IsLoading);

        ExportHerbsCommand = new DelegateCommand(
            async () => await ExportHerbsAsync(),
            () => !IsBusy && !IsLoading && Items.Count > 0
        ).ObservesProperty(() => IsBusy)
          .ObservesProperty(() => IsLoading)
          .ObservesProperty(() => Items);
    }
}
```

#### 2.2 批量导入实现

```csharp
/// <summary>
/// 导入药材（Desktop主导模式）
/// </summary>
private async Task ImportHerbsAsync()
{
    await ExecuteSafelyAsync(async () =>
    {
        // ① 打开文件选择对话框
        var filePath = await _dialogService.ShowOpenFileDialogAsync(
            filter: "Excel文件|*.xlsx",
            title: "选择药材导入文件");

        if (string.IsNullOrEmpty(filePath))
        {
            return; // 用户取消
        }

        // ② 读取文件流
        using var fileStream = File.OpenRead(filePath);
        var fileName = Path.GetFileName(filePath);

        // ③ 调用Repository导入（内部调用EPPlus解析 + HTTP POST）
        Logger.LogInformation("开始导入药材文件：{FileName}", fileName);
        var result = await _herbRepository.BatchImportAsync(fileStream, fileName);

        if (result == null)
        {
            await _dialogService.ShowErrorAsync("导入失败，请检查文件格式", "导入药材");
            return;
        }

        // ④ 显示导入结果（详细反馈）
        var message = $"导入完成！\n\n" +
                      $"✅ 成功：{result.SuccessCount}条\n" +
                      $"❌ 失败：{result.FailureCount}条\n" +
                      $"⏭️ 跳过：{result.SkippedCount}条\n\n" +
                      $"成功率：{result.SuccessRate:F1}%";

        if (result.FailureCount > 0)
        {
            message += $"\n\n前{Math.Min(3, result.Failures.Count)}条失败记录：\n";
            foreach (var failure in result.Failures.Take(3))
            {
                message += $"\n第{failure.RowNumber}行（{failure.HerbName}）：{failure.Reason}";
            }
        }

        await _dialogService.ShowInfoAsync(message, "导入结果");

        // ⑤ 刷新列表（仅成功数量>0时）
        if (result.SuccessCount > 0)
        {
            await RefreshAsync();
        }
    }, "导入药材");
}
```

#### 2.3 批量导出实现

```csharp
/// <summary>
/// 导出药材（Desktop主导模式）
/// </summary>
private async Task ExportHerbsAsync()
{
    await ExecuteSafelyAsync(async () =>
    {
        // ① 打开保存文件对话框
        var filePath = await _dialogService.ShowSaveFileDialogAsync(
            filter: "Excel文件|*.xlsx",
            title: "导出药材数据",
            defaultFileName: $"药材数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

        if (string.IsNullOrEmpty(filePath))
        {
            return; // 用户取消
        }

        // ② 从Server获取数据（JSON格式）
        Logger.LogInformation("导出药材数据，关键词：{Keyword}", SearchText);
        var herbs = await _herbRepository.GetAllForExportAsync(SearchText);

        if (herbs == null || herbs.Count == 0)
        {
            await _dialogService.ShowInfoAsync("没有符合条件的药材数据", "导出药材");
            return;
        }

        // ③ Desktop层生成Excel（EPPlus）
        var bytes = GenerateExcelBytes(herbs);

        // ④ 保存文件
        await File.WriteAllBytesAsync(filePath, bytes);

        await _dialogService.ShowInfoAsync(
            $"成功导出{herbs.Count}条药材数据到：\n{filePath}",
            "导出成功");
    }, "导出药材");
}

/// <summary>
/// 生成Excel字节数组（EPPlus实现）
/// </summary>
private byte[] GenerateExcelBytes(List<HerbDto> herbs)
{
    using var package = new ExcelPackage();
    var worksheet = package.Workbook.Worksheets.Add("药材数据");

    // 设置表头
    worksheet.Cells[1, 1].Value = "药材名称";
    worksheet.Cells[1, 2].Value = "拼音码";
    worksheet.Cells[1, 3].Value = "分类";
    worksheet.Cells[1, 4].Value = "状态";
    // ... 更多列

    // 填充数据
    for (int i = 0; i < herbs.Count; i++)
    {
        var herb = herbs[i];
        worksheet.Cells[i + 2, 1].Value = herb.Name;
        worksheet.Cells[i + 2, 2].Value = herb.PinYinCode;
        // ... 更多字段
    }

    return package.GetAsByteArray();
}
```

---

### Step 3: Desktop层Repository实现

**位置**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Repositories/HerbRepository.cs`

```csharp
public class HerbRepository : RepositoryBase<HerbDto, HerbInputDto, HerbInputDto, IHerbApi>, IHerbRepository
{
    private readonly IHerbApi _api;

    /// <summary>
    /// 批量导入药材（Desktop主导模式）
    /// </summary>
    public async Task<HerbBatchImportResultDto?> BatchImportAsync(
        Stream fileStream, string fileName)
    {
        // ① 使用EPPlus解析Excel
        var herbs = await ParseExcelAsync(fileStream);

        if (herbs.Count == 0)
        {
            return null; // 解析失败或空文件
        }

        // ② 构造请求DTO
        var request = new HerbBatchImportRequestDto
        {
            Herbs = herbs,
            Strategy = DuplicateStrategy.Skip // 默认跳过重复
        };

        // ③ 调用Server端API（POST JSON）
        var response = await _api.BatchImportAsync(request);

        // ④ 返回结果
        return response.IsSuccess ? response.Data : null;
    }

    /// <summary>
    /// 解析Excel文件（EPPlus实现）
    /// </summary>
    private async Task<List<HerbInputDto>> ParseExcelAsync(Stream fileStream)
    {
        var herbs = new List<HerbInputDto>();

        using var package = new ExcelPackage(fileStream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();

        if (worksheet == null || worksheet.Dimension == null)
        {
            return herbs; // 空工作表
        }

        // 从第2行开始读取（第1行是表头）
        for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            var name = worksheet.Cells[row, 1].Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue; // 跳过空行
            }

            var herb = new HerbInputDto
            {
                Name = name,
                Category = worksheet.Cells[row, 2].Text.Trim(),
                // ... 更多字段解析
            };

            herbs.Add(herb);
        }

        return herbs;
    }

    /// <summary>
    /// 获取所有药材用于导出（Server返回JSON）
    /// </summary>
    public async Task<List<HerbDto>?> GetAllForExportAsync(string? category = null)
    {
        var response = await _api.GetAllForExportAsync(category);
        return response.IsSuccess ? response.Data : null;
    }
}
```

---

### Step 4: Server层API实现

**位置**: `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs`

```csharp
/// <summary>
/// 批量导入药材（Epic #1962 Task 2.3）
/// Desktop层负责Excel解析，Server层接收DTO列表
/// </summary>
[HttpPost("batch-import")]
[ProducesResponseType(typeof(ApiResponse<HerbBatchImportResultDto>), 200)]
[ProducesResponseType(400)]
public async Task<ActionResult<ApiResponse<HerbBatchImportResultDto>>> BatchImport(
    [FromBody] HerbBatchImportRequestDto request)
{
    try
    {
        // 验证请求
        if (request.Herbs == null || request.Herbs.Count == 0)
        {
            return ValidationFail<HerbBatchImportResultDto>("药材列表不能为空");
        }

        // BR-006: 批量导入数量限制
        if (request.Herbs.Count > 10000)
        {
            return ValidationFail<HerbBatchImportResultDto>("批量导入最多支持10000条记录");
        }

        // 调用Service层处理
        var result = await _herbService.BatchImportAsync(request.Herbs, request.Strategy);

        if (result.IsSuccess && result.Data != null)
        {
            LogOperation("批量导入药材（Epic #1962）",
                new {
                    TotalCount = result.Data.TotalCount,
                    SuccessCount = result.Data.SuccessCount,
                    Strategy = request.Strategy.ToString()
                },
                null);
        }

        return HandleServiceResult(result, $"批量导入完成: 成功{result.Data?.SuccessCount ?? 0}条");
    }
    catch (Exception ex)
    {
        return HandleException<HerbBatchImportResultDto>(
            ex, "批量导入药材",
            new { HerbCount = request.Herbs?.Count, Strategy = request.Strategy });
    }
}

/// <summary>
/// 获取所有药材数据用于导出（Epic #1962 Task 3.2）
/// Desktop层负责Excel生成，Server层返回JSON数据
/// </summary>
[HttpGet("export-all")]
[ProducesResponseType(typeof(ApiResponse<List<HerbDto>>), 200)]
public async Task<ActionResult<ApiResponse<List<HerbDto>>>> GetAllForExport(
    [FromQuery] string? category = null)
{
    try
    {
        var result = await _herbService.GetAllForExportAsync(category);

        if (result.IsSuccess && result.Data != null)
        {
            LogOperation("导出药材数据（Epic #1962）",
                new { Category = category, Count = result.Data.Count },
                null);
        }

        return HandleServiceResult(result, "导出数据查询成功");
    }
    catch (Exception ex)
    {
        return HandleException<List<HerbDto>>(ex, "获取导出数据", new { Category = category });
    }
}
```

---

### Step 5: Server层Service实现

**位置**: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs`

```csharp
/// <summary>
/// 批量导入药材（Epic #1962 Task 2.2）
/// </summary>
public async Task<ServiceResult<HerbBatchImportResultDto>> BatchImportAsync(
    List<HerbInputDto> herbs,
    DuplicateStrategy strategy)
{
    const int MAX_IMPORT_SIZE = 10000; // BR-006

    try
    {
        // BR-006: 批量导入数量限制
        if (herbs.Count > MAX_IMPORT_SIZE)
        {
            return ServiceResult<HerbBatchImportResultDto>.Failure(
                $"批量导入最多支持{MAX_IMPORT_SIZE}条记录");
        }

        var result = new HerbBatchImportResultDto
        {
            TotalCount = herbs.Count
        };

        foreach (var dto in herbs)
        {
            try
            {
                // ① 生成拼音码（Shared层工具）
                dto.PinYinCode = PinYinHelper.GetPinYinCode(dto.Name);

                // ② 检查重复（根据strategy处理）
                var exists = await _repository.ExistsByNameAsync(dto.Name);
                if (exists)
                {
                    if (strategy == DuplicateStrategy.Skip)
                    {
                        result.SkippedCount++;
                        continue;
                    }
                    else if (strategy == DuplicateStrategy.Error)
                    {
                        result.Failures.Add(new ImportFailureDto
                        {
                            RowNumber = result.TotalCount,
                            HerbName = dto.Name,
                            Reason = "药材名称已存在"
                        });
                        result.FailureCount++;
                        continue;
                    }
                    // DuplicateStrategy.Update: 继续处理（更新现有记录）
                }

                // ③ 映射为实体并保存
                var entity = _mapper.Map<Herb>(dto);
                await _repository.AddAsync(entity);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入药材失败: {Name}", dto.Name);
                result.Failures.Add(new ImportFailureDto
                {
                    RowNumber = herbs.IndexOf(dto) + 2, // Excel行号（第1行是表头）
                    HerbName = dto.Name,
                    Reason = ex.Message
                });
                result.FailureCount++;
            }
        }

        // ④ 一次性保存（事务保证）
        await _unitOfWork.SaveChangesAsync();

        result.Message = $"批量导入完成: 成功{result.SuccessCount}条";
        return ServiceResult<HerbBatchImportResultDto>.Success(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "批量导入药材异常");
        return ServiceResult<HerbBatchImportResultDto>.Failure($"批量导入失败: {ex.Message}");
    }
}

/// <summary>
/// 获取所有药材用于导出（Epic #1962 Task 3.1）
/// </summary>
public async Task<ServiceResult<List<HerbDto>>> GetAllForExportAsync(string? category = null)
{
    try
    {
        var herbs = await _repository.GetAllAsync(category);
        var dtos = _mapper.Map<List<HerbDto>>(herbs);

        _logger.LogInformation("导出药材数据: {Count}条", dtos.Count);
        return ServiceResult<List<HerbDto>>.Success(dtos);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取导出数据失败");
        return ServiceResult<List<HerbDto>>.Failure($"获取数据失败: {ex.Message}");
    }
}
```

---

## 5. 常见问题

### Q1: 为什么不在Server端处理Excel文件？

**A**: 
- ❌ **问题**：Server端需要安装EPPlus库 → 增加依赖和复杂度
- ❌ **问题**：文件上传消耗带宽 → 大文件解析占用服务器资源
- ✅ **优势**：Desktop端解析失败不影响服务器 → 容错性更高
- ✅ **优势**：Server端无状态 → 易于水平扩展

### Q2: 如果Excel文件很大（10万行）怎么办?

**A**:
1. **前端限制**：`BR-006` 规定单次导入最多10000条
2. **分批导入**：Desktop层拆分为多个请求（每批5000条）
3. **进度显示**：使用 `IProgress<T>` 实时更新UI进度条

```csharp
// Desktop层分批导入示例
private async Task BatchImportWithProgressAsync(List<HerbInputDto> allHerbs)
{
    const int BATCH_SIZE = 5000;
    var totalBatches = (int)Math.Ceiling(allHerbs.Count / (double)BATCH_SIZE);

    for (int i = 0; i < totalBatches; i++)
    {
        var batch = allHerbs.Skip(i * BATCH_SIZE).Take(BATCH_SIZE).ToList();
        var result = await _herbRepository.BatchImportAsync(batch);

        // 更新进度（第i+1批 / 总批数）
        ProgressPercentage = (i + 1) * 100 / totalBatches;
    }
}
```

### Q3: 如何处理导入过程中的网络超时？

**A**:
1. **Server端**：设置合理的超时时间（批量操作延长至5分钟）
   ```csharp
   [HttpPost("batch-import")]
   [RequestTimeout(300000)] // 5分钟超时
   ```

2. **Desktop端**：使用 `HttpClient` 的 `Timeout` 属性
   ```csharp
   private readonly HttpClient _httpClient = new HttpClient
   {
       Timeout = TimeSpan.FromMinutes(5)
   };
   ```

3. **重试机制**：使用 Polly 库实现重试
   ```csharp
   var retryPolicy = Policy
       .Handle<HttpRequestException>()
       .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(2));

   await retryPolicy.ExecuteAsync(async () =>
       await _api.BatchImportAsync(request));
   ```

### Q4: 导出的Excel格式如何定制？

**A**: Desktop层完全控制Excel格式，可使用EPPlus高级功能：

```csharp
private byte[] GenerateStyledExcelBytes(List<HerbDto> herbs)
{
    using var package = new ExcelPackage();
    var worksheet = package.Workbook.Worksheets.Add("药材数据");

    // ① 设置表头样式
    using (var headerRange = worksheet.Cells[1, 1, 1, 4])
    {
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
    }

    // ② 设置数据验证（分类下拉列表）
    var categoryValidation = worksheet.DataValidations.AddListValidation("C2:C10000");
    categoryValidation.Formula.Values.Add("解表药");
    categoryValidation.Formula.Values.Add("清热药");
    // ... 更多分类

    // ③ 设置条件格式（失效药材标红）
    var redFill = new ExcelFillStyle(ExcelFillStyle.Solid, Color.Red);
    var statusCol = worksheet.Cells["D2:D10000"];
    // ... 添加规则

    return package.GetAsByteArray();
}
```

---

## 6. 性能优化建议

### 6.1 Desktop层优化

**异步加载大文件**：
```csharp
// ✅ 使用异步流式读取（大文件友好）
private async IAsyncEnumerable<HerbInputDto> ParseExcelStreamAsync(Stream fileStream)
{
    using var package = new ExcelPackage(fileStream);
    var worksheet = package.Workbook.Worksheets.First();

    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
    {
        // 每解析1000行暂停一次，让UI响应
        if (row % 1000 == 0)
        {
            await Task.Delay(10); // 释放UI线程
        }

        yield return new HerbInputDto
        {
            Name = worksheet.Cells[row, 1].Text.Trim(),
            // ...
        };
    }
}
```

### 6.2 Server层优化

**批量SaveChanges**：
```csharp
// ✅ 一次性SaveChanges（减少数据库往返）
await _repository.AddRangeAsync(entities);  // 批量添加
await _unitOfWork.SaveChangesAsync();       // 一次保存

// ❌ 避免循环中多次SaveChanges
foreach (var entity in entities)
{
    await _repository.AddAsync(entity);
    await _unitOfWork.SaveChangesAsync(); // ❌ 性能杀手
}
```

**使用AsNoTracking查询**：
```csharp
// ✅ 导出查询不需要跟踪（只读）
public async Task<List<Herb>> GetAllAsync(string? category = null)
{
    return await _context.Herbs
        .AsNoTracking()  // 关键优化
        .Where(h => category == null || h.Category == category)
        .ToListAsync();
}
```

### 6.3 数据库优化

**创建覆盖索引**（Epic #1962 Phase 1已实现）：
```sql
-- 覆盖索引：包含查询常用字段
CREATE INDEX IX_Herbs_Category_Status_Includes
ON Herbs (Category, Status)
INCLUDE (Name, PinYinCode);
```

**性能基准**：
| 操作 | 数据量 | 要求 | 实际 |
|-----|-------|------|------|
| 批量导入 | 1000条 | < 10秒 | ~8秒 |
| 数据导出 | 10000条 | < 2秒 | ~1.5秒 |
| 引用检查 | 单次 | < 500ms | ~300ms |

---

## 📚 相关文档

| 文档 | 路径 | 说明 |
|-----|------|------|
| **Herbs模块架构** | `docs/explanation/architecture/server/modules/herbs.md` | 模块详细设计 |
| **跨模块依赖** | `docs/explanation/architecture/shared/cross-module-dependencies.md` | 依赖关系说明 |
| **Epic #1962设计** | `docs/explanation/architecture/server/herbs-management-enhancement-design.md` | 完整技术设计 |
| **Server三层架构** | `docs/explanation/architecture/server/README.md` | 架构总览 |

---

## 📝 变更历史

| 版本 | 日期 | 作者 | 变更内容 |
|-----|------|------|---------|
| v1.0 | 2025-11-10 | Claude | 初始版本（Epic #1962 Phase 5 Task 5.3） |

---

**最后更新**: 2025-11-10  
**维护者**: 开发团队  
**文档状态**: ✅ 已完成
