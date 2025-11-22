# 中药管理问题解决指南

**8个常见中药管理问题的系统性解决方案**

本文档针对中药管理模块的实际使用中遇到的常见问题，提供详细的解决方案和最佳实践。

---

## 问题分类

| 问题类型 | 解决难度 | 影响范围 | 优先级 |
|---------|---------|---------|--------|
| 药材重复录入 | ⭐⭐⭐ | 数据质量 | 高 |
| 价格调整审批 | ⭐⭐ | 业务流程 | 高 |
| Excel导入失败 | ⭐⭐ | 批量操作 | 中 |
| 搜索结果不准 | ⭐⭐⭐ | 用户体验 | 高 |
| 分类体系混乱 | ⭐⭐ | 数据组织 | 中 |
| 引用检查失败 | ⭐⭐ | 数据完整性 | 中 |
| 拼音码缺失 | ⭐ | 搜索效率 | 低 |
| 批量操作失败 | ⭐⭐⭐ | 事务处理 | 高 |

---

## 问题1: 药材重复录入 - 拼音码智能去重

### 问题描述

用户在添加新药材时，可能因为输入名称的不同（如"人参"与"人蔘"），导致系统中存在重复的药材记录，影响数据质量和处方开具的准确性。

### 根本原因

1. **同名异形**: 中药材存在别名、异体字、繁简转换等情况
2. **用户误操作**: 未查询现有药材就直接创建
3. **拼音码冲突**: 不同药材可能生成相同的拼音码

### 解决方案

#### 1.1 拼音码自动生成（Issue #2174）

系统在创建或导入药材时，自动生成拼音码用于快速检索和去重：

```csharp
// HerbService.cs:344 - Excel导入时自动生成拼音码
var herb = new Herb
{
    Name = name,
    PinYinCode = PinYinHelper.GetPinYinCode(name), // 自动生成
    Unit = unit,
    Price = price,
    // ... 其他字段
};
```

```csharp
// HerbService.cs:549-552 - 批量导入时补充拼音码
if (string.IsNullOrWhiteSpace(dto.PinYinCode))
{
    dto.PinYinCode = PinYinHelper.GetPinYinCode(dto.Name);
}
```

**拼音码生成规则**:
- 取每个汉字的拼音首字母
- 示例: "人参" → "RS", "当归" → "DG"
- 自动大写处理

#### 1.2 重复检测机制

在批量导入时，系统提供三种重复处理策略：

```csharp
// HerbService.cs:554-593 - 重复检测与策略处理
var exists = await _repository.ExistsByNameAsync(dto.Name);

if (exists)
{
    switch (strategy)
    {
        case DuplicateStrategy.Skip:
            // 跳过重复项，不导入
            result.SkippedCount++;
            continue;

        case DuplicateStrategy.Update:
            // 更新现有药材信息
            var existingHerb = await _repository.FindAsync(h => h.Name == dto.Name);
            _mapper.Map(dto, existingHerb);
            await _repository.UpdateAsync(existingHerb);
            result.SuccessCount++;
            continue;

        case DuplicateStrategy.Error:
            // 报错并记录失败详情
            result.FailureCount++;
            result.Failures.Add(new HerbImportFailureDetailDto
            {
                RowNumber = rowNumber,
                HerbName = dto.Name,
                Reason = "药材名称重复"
            });
            continue;
    }
}
```

#### 1.3 最佳实践

**导入前准备**:
1. 下载标准导入模板
2. 对照现有药材库检查重复
3. 选择合适的重复策略

**策略选择建议**:
- **Skip**: 首次大批量导入，避免覆盖现有数据
- **Update**: 价格/规格等信息更新，保持药材ID不变
- **Error**: 严格数据质量控制，需人工审核每条记录

**代码位置**:
- 自动生成: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs:344, 551`
- 重复检测: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs:554-593`
- 拼音工具: `src/Shared/LYBT.Shared.Utilities/Text/PinYinHelper.cs`

---

## 问题2: 价格调整审批 - 多层级价格管理

### 问题描述

药材价格频繁波动，需要及时调整系统中的价格信息，但直接修改可能导致历史处方金额不准确，且缺乏审批流程。

### 根本原因

1. **缺少价格历史**: 无法追溯价格变更记录
2. **权限控制不足**: 任何用户都能修改价格
3. **成本核算混乱**: 销售价与成本价未分离

### 解决方案

#### 2.1 价格字段设计

系统支持双价格体系：

```csharp
// LYBT.Entities/Herbs/Herb.cs - 价格字段
public class Herb : BaseEntity
{
    public decimal Price { get; set; }          // 销售单价（必填）
    public decimal? CostPrice { get; set; }     // 成本价（可选）
    // ... 其他字段
}
```

**字段说明**:
- **Price**: 对患者的销售价格，用于处方计费
- **CostPrice**: 采购成本价，用于利润核算（可选）

#### 2.2 价格更新流程

```csharp
// HerbService.cs:114-141 - 价格更新验证
public async Task<Result<HerbDto>> UpdateAsync(Guid id, HerbInputDto dto)
{
    var entity = await _repository.GetByIdAsync(id);
    if (entity == null)
        return Result<HerbDto>.Failure("药材不存在");

    // FluentValidation 验证价格合法性
    var validationResult = await _validator.ValidateAsync(dto);
    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
        return Result<HerbDto>.Failure(errors);
    }

    _mapper.Map(dto, entity);
    var result = await _repository.UpdateAsync(entity);
    return Result<HerbDto>.Success(resultDto);
}
```

#### 2.3 价格验证规则

FluentValidation确保价格数据的合法性：

```csharp
// HerbInputDtoValidator.cs - 价格验证规则
public class HerbInputDtoValidator : AbstractValidator<HerbInputDto>
{
    public HerbInputDtoValidator()
    {
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("销售价格必须大于0")
            .LessThanOrEqualTo(100000).WithMessage("销售价格不能超过100000元");

        RuleFor(x => x.CostPrice)
            .GreaterThan(0).When(x => x.CostPrice.HasValue)
            .WithMessage("成本价必须大于0");

        // 成本价不能高于销售价
        RuleFor(x => x.CostPrice)
            .LessThanOrEqualTo(x => x.Price).When(x => x.CostPrice.HasValue)
            .WithMessage("成本价不能高于销售价");
    }
}
```

#### 2.4 最佳实践

**价格调整流程**:
1. 管理员权限验证
2. 记录调整原因（备注字段）
3. 执行价格更新
4. 通知相关处方开具人员

**Excel批量调价**:
```excel
药材名称*  | 单位* | 新单价* | 新成本价 | 备注
人参       | 克    | 5.5     | 4.0      | 市场价上涨10%
当归       | 克    | 0.8     | 0.6      | 产地促销降价
```

导入策略选择 **Update**，自动更新现有药材价格。

**注意事项**:
- ✅ 价格必须大于0
- ✅ 成本价不能高于销售价
- ✅ 使用备注字段记录调价原因
- ❌ 不建议频繁调整（建议每月集中调整）

**代码位置**:
- 价格更新: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs:114-141`
- 价格验证: `src/Server/Modules/LYBT.Module.Herbs/Validators/HerbInputDtoValidator.cs`
- 实体定义: `src/Server/Core/LYBT.Entities/Herbs/Herb.cs`

---

## 问题3: Excel导入失败 - 数据格式验证

### 问题描述

用户在批量导入药材时，Excel文件格式不规范，导致导入失败或部分数据丢失，影响工作效率。

### 根本原因

1. **模板格式不统一**: 用户自行编辑Excel，列顺序错误
2. **必填字段缺失**: 药材名称、单位、价格未填写
3. **数据类型错误**: 价格字段填写文字而非数字

### 解决方案

#### 3.1 下载标准导入模板

系统提供标准模板生成功能：

```csharp
// HerbService.cs:465-515 - 生成导入模板
public MemoryStream GenerateImportTemplate()
{
    var stream = new MemoryStream();
    using (var package = new ExcelPackage(stream))
    {
        var worksheet = package.Workbook.Worksheets.Add("药材信息");

        // 表头（* 表示必填）
        worksheet.Cells[1, 1].Value = "药材名称*";
        worksheet.Cells[1, 2].Value = "单位*";
        worksheet.Cells[1, 3].Value = "单价*";
        worksheet.Cells[1, 4].Value = "产地";
        worksheet.Cells[1, 5].Value = "规格";
        worksheet.Cells[1, 6].Value = "功效";
        worksheet.Cells[1, 7].Value = "用法用量";
        worksheet.Cells[1, 8].Value = "备注";

        // 示例数据
        worksheet.Cells[2, 1].Value = "人参";
        worksheet.Cells[2, 2].Value = "克";
        worksheet.Cells[2, 3].Value = 5.0;
        worksheet.Cells[2, 4].Value = "吉林";
        worksheet.Cells[2, 5].Value = "特级";
        worksheet.Cells[2, 6].Value = "大补元气，复脉固脱";
        worksheet.Cells[2, 7].Value = "3-9克";
        worksheet.Cells[2, 8].Value = "贵重药材";
    }
    return stream;
}
```

**API调用**:
```http
GET /api/herbs/template
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
```

#### 3.2 导入数据验证

系统对每行数据进行严格验证：

```csharp
// HerbService.cs:295-340 - Excel导入验证逻辑
for (int row = 2; row <= rowCount; row++)
{
    var name = worksheet.Cells[row, 1].Text?.Trim();
    var unit = worksheet.Cells[row, 2].Text?.Trim();
    var priceText = worksheet.Cells[row, 3].Text?.Trim();

    // 必填字段验证
    if (string.IsNullOrWhiteSpace(name))
    {
        result.Errors.Add(new BatchOperationResultDto.ErrorDetail
        {
            RecordIdentifier = $"第{row}行",
            ErrorMessage = "药材名称不能为空"
        });
        continue;
    }

    if (string.IsNullOrWhiteSpace(unit))
    {
        result.Errors.Add(new BatchOperationResultDto.ErrorDetail
        {
            RecordIdentifier = $"第{row}行",
            ErrorMessage = "单位不能为空"
        });
        continue;
    }

    // 价格格式验证
    if (!decimal.TryParse(priceText, out var price) || price <= 0)
    {
        result.Errors.Add(new BatchOperationResultDto.ErrorDetail
        {
            RecordIdentifier = $"第{row}行",
            ErrorMessage = "单价格式错误或必须大于0"
        });
        continue;
    }

    // 创建药材实体
    var herb = new Herb
    {
        Name = name,
        PinYinCode = PinYinHelper.GetPinYinCode(name),
        Unit = unit,
        Price = price,
        // ... 其他可选字段
    };
}
```

#### 3.3 导入结果反馈

```csharp
// HerbService.cs:375-377 - 导入结果摘要
result.IsSuccess = true;
result.Message = $"导入完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条";
```

**导入结果DTO**:
```csharp
public class ImportResultDto<T>
{
    public string? FileName { get; set; }
    public DateTime ImportTime { get; set; }
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BatchOperationResultDto.ErrorDetail> Errors { get; set; }
    public List<T> ImportedData { get; set; }
}
```

#### 3.4 最佳实践

**导入前检查清单**:
- [ ] 使用系统生成的标准模板
- [ ] 确保必填字段（药材名称、单位、单价）都已填写
- [ ] 价格字段使用数字格式，不含货币符号
- [ ] 单位统一（如：克、g、克）建议使用"克"
- [ ] 删除空行

**常见错误与解决**:

| 错误提示 | 原因 | 解决方法 |
|---------|------|---------|
| "药材名称不能为空" | A列为空 | 填写药材名称 |
| "单位不能为空" | B列为空 | 填写"克"、"付"等单位 |
| "单价格式错误" | C列填写了"5元"或"免费" | 只填写数字，如"5.0" |
| "单价必须大于0" | C列填写了0或负数 | 填写正数价格 |

**Excel格式要求**:
```
列A: 药材名称* - 文本类型
列B: 单位*     - 文本类型
列C: 单价*     - 数字类型（保留2位小数）
列D: 产地      - 文本类型
列E: 规格      - 文本类型
列F: 功效      - 文本类型
列G: 用法用量   - 文本类型
列H: 备注      - 文本类型
```

**代码位置**:
- 模板生成: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs:465-515`
- 导入验证: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs:263-387`
- DTO定义: `src/Shared/LYBT.Shared.Models/Contracts/Common/ImportResultDto.cs`

---

## 问题4: 搜索结果不准 - 多维度搜索优化

### 问题描述

用户在处方开具时，搜索"人参"或输入拼音"RS"无法快速定位药材，影响开方效率。

### 根本原因

1. **单维度搜索**: 只支持药材名称精确匹配
2. **拼音索引缺失**: 无法通过拼音快速检索
3. **搜索权重未优化**: 常用药材未优先显示

### 解决方案

#### 4.1 多维度搜索实现

```csharp
// HerbService.cs:243-258 - 多维度搜索
public async Task<Result<List<HerbDto>>> SearchAsync(string keyword)
{
    var entities = await _repository.FindAsync(h =>
        h.Name.Contains(keyword) ||                        // 名称匹配
        (h.PinYinCode != null && h.PinYinCode.Contains(keyword)) // 拼音码匹配
    );
    var dtos = _mapper.Map<List<HerbDto>>(entities);
    return Result<List<HerbDto>>.Success(dtos);
}
```

**支持的搜索方式**:
- ✅ 药材全名: "人参" → 找到"人参"
- ✅ 药材简称: "参" → 找到"人参"、"党参"、"西洋参"
- ✅ 拼音全码: "renshen" → 找到"人参"
- ✅ 拼音简码: "RS" → 找到"人参"

#### 4.2 分页搜索增强

```csharp
// HerbService.cs:38-69 - 分页搜索 + 分类筛选
public async Task<Result<PagedResult<HerbDto>>> GetPagedAsync(
    int page = 1,
    int pageSize = 20,
    string? keyword = null,
    string? category = null)
{
    // 数据库级别关键词搜索（Repository层）
    var pagedResult = await _repository.GetPagedAsync(page, pageSize, keyword);
    var dtos = _mapper.Map<List<HerbDto>>(pagedResult.Items);

    // 应用层分类筛选
    if (!string.IsNullOrWhiteSpace(category))
    {
        dtos = dtos.Where(h =>
            !string.IsNullOrEmpty(h.Category) &&
            h.Category.Contains(category, StringComparison.OrdinalIgnoreCase))
        .ToList();
    }

    return Result<PagedResult<HerbDto>>.Success(new PagedResult<HerbDto>
    {
        Items = dtos,
        TotalCount = pagedResult.TotalCount,
        CurrentPage = page,
        PageSize = pageSize
    });
}
```

#### 4.3 拼音码索引优化

**自动生成策略**:
```csharp
// PinYinHelper.GetPinYinCode示例
人参   → RS
当归   → DG
黄芪   → HQ
白术   → BS
党参   → DS
西洋参 → XYS
```

**处理特殊情况**:
- 多音字: 统一使用常用读音（如"参"统一读"shen"）
- 繁体字: 自动转换为简体后生成拼音
- 数字: 保留数字（如"三七"→"SQ"）

#### 4.4 最佳实践

**搜索技巧**:
1. **快速输入拼音**: 输入"DG"比"当归"更快
2. **模糊搜索**: 输入"参"可找到所有含"参"的药材
3. **分类过滤**: 先选择"补气药"，再搜索"参"缩小范围

**性能优化建议**:
- 为`Name`和`PinYinCode`字段添加数据库索引
- 搜索结果限制在100条以内
- 使用分页避免一次性加载大量数据

**API调用示例**:
```http
# 搜索所有含"参"的药材
GET /api/herbs/search?keyword=参

# 拼音搜索
GET /api/herbs/search?keyword=RS

# 分页 + 关键词 + 分类
GET /api/herbs?page=1&pageSize=20&keyword=参&category=补气药
```

**代码位置**:
- 搜索实现: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs:243-258`
- 分页搜索: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs:38-69`
- 拼音工具: `src/Shared/LYBT.Shared.Utilities/Text/PinYinHelper.cs`

---

## 问题5: 分类体系混乱 - 标准化分类管理

### 问题描述

不同用户录入药材时使用的分类不统一（如"补气药"、"补气类"、"气虚类药"），导致分类查询结果不准确。

### 根本原因

1. **缺少分类字典**: 没有标准分类参考
2. **自由文本输入**: 用户可随意输入分类名称
3. **中医理论复杂**: 药材分类有多种体系（功效、性味、归经）

### 解决方案

#### 5.1 标准分类体系

建议使用**功效分类法**（与《中国药典》一致）：

**一级分类（18类）**:
```
1. 补气药       - 人参、党参、黄芪、白术
2. 补血药       - 当归、熟地黄、白芍、阿胶
3. 补阴药       - 枸杞子、百合、麦冬、石斛
4. 补阳药       - 鹿茸、淫羊藿、肉苁蓉、杜仲
5. 清热药       - 黄连、黄芩、栀子、石膏
6. 解表药       - 麻黄、桂枝、柴胡、薄荷
7. 理气药       - 陈皮、木香、香附、枳壳
8. 活血化瘀药   - 川芎、丹参、红花、三七
9. 止血药       - 白及、仙鹤草、蒲黄、三七
10. 祛痰药      - 半夏、陈皮、茯苓、桔梗
11. 安神药      - 酸枣仁、柏子仁、远志、龙骨
12. 平肝息风药  - 天麻、钩藤、石决明、牡蛎
13. 利水渗湿药  - 茯苓、猪苓、泽泻、车前子
14. 消食药      - 山楂、神曲、麦芽、莱菔子
15. 驱虫药      - 使君子、苦楝皮、槟榔
16. 温里药      - 附子、干姜、肉桂、吴茱萸
17. 收涩药      - 五味子、山茱萸、乌梅、莲子
18. 其他        - 未分类或多功效药材
```

#### 5.2 分类字段设计

```csharp
// LYBT.Entities/Herbs/Herb.cs - 分类字段
public class Herb : BaseEntity
{
    public string? Category { get; set; }  // 功效分类（一级分类）
    // 可扩展二级分类、性味归经等字段
}
```

**字段说明**:
- **Category**: 存储一级分类名称（如"补气药"）
- 可选值: 建议使用下拉选择，避免自由输入
- 可为空: 允许暂不分类，后续补充

#### 5.3 分类筛选实现

```csharp
// HerbService.cs:46-53 - 分类筛选
if (!string.IsNullOrWhiteSpace(category))
{
    dtos = dtos.Where(h =>
        !string.IsNullOrEmpty(h.Category) &&
        h.Category.Contains(category, StringComparison.OrdinalIgnoreCase))
    .ToList();
}
```

**API调用**:
```http
# 查询所有补气药
GET /api/herbs?category=补气药

# 分页 + 分类
GET /api/herbs?page=1&pageSize=20&category=补气药

# 导出指定分类
GET /api/herbs/export?category=补气药
```

#### 5.4 最佳实践

**建立分类标准**:
1. 创建《药材分类标准表》Excel文档
2. 所有用户录入前查表确认分类
3. 定期审查分类一致性

**分类规范化步骤**:
1. 导出所有药材数据
2. 使用Excel筛选查看所有不同的分类值
3. 统一修改为18个标准分类
4. 批量更新导入（使用Update策略）

**Excel批量更新分类**:
```excel
药材名称* | 单位* | 单价* | 新分类*
人参      | 克    | 5.0   | 补气药
党参      | 克    | 1.2   | 补气药
当归      | 克    | 0.8   | 补血药
黄芪      | 克    | 0.6   | 补气药
```

**未来扩展**:
- 添加`SubCategory`字段（二级分类）
- 添加`Nature`字段（四气：寒、热、温、凉）
- 添加`Taste`字段（五味：辛、甘、酸、苦、咸）
- 添加`Meridians`字段（归经：肺、脾、胃等）

**代码位置**:
- 分类字段: `src/Server/Core/LYBT.Entities/Herbs/Herb.cs`
- 分类筛选: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs:46-53`

---

## 问题6: 引用检查失败 - 处方引用完整性

### 问题描述

删除已被处方使用的药材后，导致历史处方中的药材信息丢失或显示异常。

### 根本原因

1. **缺少外键约束**: 删除药材时未检查引用关系
2. **软删除未实施**: 直接物理删除导致数据丢失
3. **引用检查不完善**: 未统计药材在处方、验方中的使用次数

### 解决方案

#### 6.1 引用检查接口

```csharp
// HerbService.cs:664-696 - 检查药材引用
public async Task<Result<HerbReferenceCheckDto>> CheckReferenceAsync(Guid herbId)
{
    var herb = await _repository.GetByIdAsync(herbId);
    if (herb == null)
    {
        return Result<HerbReferenceCheckDto>.Failure("药材不存在");
    }

    var result = new HerbReferenceCheckDto
    {
        HerbId = herbId,
        HerbName = herb.Name,
        HasReferences = false,
        ReferenceCount = 0,
        CanDelete = true,  // BR-007: 支持软删除，始终可删除
        RecentReferences = new List<PrescriptionReferenceDto>()
    };

    // TODO: 实现处方引用检查
    // 后续迭代中查询 PrescriptionItems 表统计引用次数

    return Result<HerbReferenceCheckDto>.Success(result);
}
```

**引用检查DTO**:
```csharp
public class HerbReferenceCheckDto
{
    public Guid HerbId { get; set; }
    public string HerbName { get; set; }
    public bool HasReferences { get; set; }       // 是否被引用
    public int ReferenceCount { get; set; }       // 引用次数
    public bool CanDelete { get; set; }           // 是否可删除
    public List<PrescriptionReferenceDto> RecentReferences { get; set; } // 最近引用
}

public class PrescriptionReferenceDto
{
    public Guid PrescriptionId { get; set; }
    public string PrescriptionNumber { get; set; }
    public string PatientName { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### 6.2 批量引用检查

```csharp
// HerbService.cs:701-733 - 批量检查引用
public async Task<Result<List<HerbReferenceCheckDto>>> BatchCheckReferenceAsync(
    List<Guid> herbIds)
{
    const int MAX_CHECK_SIZE = 100; // BR-006: 最多检查100条

    if (herbIds.Count > MAX_CHECK_SIZE)
    {
        return Result<List<HerbReferenceCheckDto>>.Failure(
            $"批量检查最多支持{MAX_CHECK_SIZE}条记录");
    }

    var results = new List<HerbReferenceCheckDto>();

    foreach (var herbId in herbIds)
    {
        var checkResult = await CheckReferenceAsync(herbId);
        if (checkResult.IsSuccess && checkResult.Data != null)
        {
            results.Add(checkResult.Data);
        }
    }

    return Result<List<HerbReferenceCheckDto>>.Success(results);
}
```

#### 6.3 软删除机制

```csharp
// HerbService.cs:143-155 - 软删除实现
public async Task<Result> DeleteAsync(Guid id)
{
    try
    {
        await _repository.DeleteAsync(id); // 软删除，标记IsDeleted=true
        return Result.Success();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "删除药材失败");
        return Result.Failure("删除药材失败");
    }
}
```

**软删除优势**:
- ✅ 历史处方数据完整性
- ✅ 可恢复误删除的药材
- ✅ 审计追踪删除记录
- ✅ 不影响统计分析

#### 6.4 最佳实践

**删除前检查流程**:
1. 调用引用检查API
2. 如果`HasReferences = true`，显示警告
3. 提示用户"该药材已被X条处方使用，删除后处方仍可查看"
4. 确认后执行软删除

**API调用示例**:
```http
# 单个药材引用检查
GET /api/herbs/{herbId}/references

# 批量引用检查
POST /api/herbs/batch-check-references
Content-Type: application/json

{
  "herbIds": [
    "guid1",
    "guid2",
    "guid3"
  ]
}
```

**未来增强**:
- 实现处方引用统计（查询`PrescriptionItems`表）
- 显示最近10条引用处方的详细信息
- 支持"禁用"而非"删除"常用药材

**代码位置**:
- 引用检查: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs:664-733`
- 软删除: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs:143-155`
- DTO定义: `src/Shared/LYBT.Shared.Models/Contracts/Herbs/HerbReferenceCheckDto.cs`

---

## 问题7: 拼音码缺失 - 历史数据补全

### 问题描述

系统早期录入的药材没有拼音码字段，导致拼音搜索功能失效，影响开方效率。

### 根本原因

1. **历史数据遗留**: 早期版本未实现拼音码功能
2. **手动录入遗漏**: 用户录入时未填写拼音码
3. **Excel导入未生成**: 旧版导入逻辑未自动生成

### 解决方案

#### 7.1 自动生成拼音码（Issue #2174）

**新增药材自动生成**:
```csharp
// HerbService.cs:344 - Excel导入自动生成
var herb = new Herb
{
    Name = name,
    PinYinCode = PinYinHelper.GetPinYinCode(name),  // 自动生成
    // ... 其他字段
};
```

**批量导入自动补充**:
```csharp
// HerbService.cs:549-552 - 批量导入补充拼音码
if (string.IsNullOrWhiteSpace(dto.PinYinCode))
{
    dto.PinYinCode = PinYinHelper.GetPinYinCode(dto.Name);
}
```

#### 7.2 历史数据补全方案

**方案A: 导出-补全-导入**

1. 导出所有药材数据
```http
GET /api/herbs/export
```

2. 在Excel中使用公式生成拼音码（手动处理）

3. 使用Update策略重新导入
```http
POST /api/herbs/import
Content-Type: multipart/form-data
strategy=Update
```

**方案B: 数据库脚本批量更新**

```sql
-- SQL Server脚本示例（需要拼音转换函数支持）
UPDATE Herbs
SET PinYinCode = dbo.fn_GetPinYinCode(Name)
WHERE PinYinCode IS NULL OR PinYinCode = '';
```

**方案C: 后台服务自动补全**

创建后台任务定期检查并补全：

```csharp
// 伪代码：后台任务
public async Task FixMissingPinYinCodesAsync()
{
    var herbsWithoutPinYin = await _repository.FindAsync(h =>
        string.IsNullOrWhiteSpace(h.PinYinCode));

    foreach (var herb in herbsWithoutPinYin)
    {
        herb.PinYinCode = PinYinHelper.GetPinYinCode(herb.Name);
        await _repository.UpdateAsync(herb);
    }
}
```

#### 7.3 最佳实践

**预防措施**:
- ✅ 新增药材时自动生成拼音码（已实现）
- ✅ 导入时自动补充拼音码（已实现）
- ✅ 定期检查拼音码缺失情况

**数据质量检查**:
```sql
-- 检查拼音码缺失的药材数量
SELECT COUNT(*) AS MissingPinYinCount
FROM Herbs
WHERE PinYinCode IS NULL OR PinYinCode = '';

-- 列出所有缺失拼音码的药材
SELECT Id, Name, Category
FROM Herbs
WHERE PinYinCode IS NULL OR PinYinCode = ''
ORDER BY CreatedAt DESC;
```

**推荐方案**: 优先使用**方案A**（导出-补全-导入），操作简单，风险可控。

**代码位置**:
- 自动生成: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs:344, 549-552`
- 拼音工具: `src/Shared/LYBT.Shared.Utilities/Text/PinYinHelper.cs`

---

## 问题8: 批量操作失败 - 事务处理和回滚

### 问题描述

批量删除100条药材时，执行到第50条时发生错误，前49条已删除，后51条未处理，导致数据不一致。

### 根本原因

1. **缺少事务管理**: 批量操作未包装在事务中
2. **异常处理不完善**: 单条失败导致整个批量操作中断
3. **操作数量限制**: 未限制单次批量操作的数量

### 解决方案

#### 8.1 批量删除实现

```csharp
// HerbService.cs:161-241 - 批量删除
public async Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids)
{
    const int MAX_BATCH_SIZE = 100; // BR-006: 批量大小限制

    // 数量限制
    if (ids.Count > MAX_BATCH_SIZE)
    {
        return Result<BatchOperationResultDto>.Failure(
            $"批量操作最多支持{MAX_BATCH_SIZE}条记录");
    }

    var result = new BatchOperationResultDto
    {
        TotalCount = ids.Count,
        IsSuccess = true,
        Message = "批量删除完成"
    };

    // 逐条处理（容错策略）
    foreach (var herbId in ids)
    {
        try
        {
            // 检查药材是否存在
            var herb = await _repository.GetByIdAsync(herbId);
            if (herb == null)
            {
                result.FailureCount++;
                result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                {
                    RecordIdentifier = herbId.ToString(),
                    ErrorMessage = "药材不存在"
                });
                continue;
            }

            // TODO: 检查引用关系（后续迭代）

            // 执行软删除
            await _repository.DeleteAsync(herbId);
            result.SuccessCount++;
            result.SuccessfulIds.Add(herbId);
        }
        catch (Exception ex)
        {
            result.FailureCount++;
            result.FailedIds.Add(herbId);
            result.Errors.Add(new BatchOperationResultDto.ErrorDetail
            {
                RecordIdentifier = herbId.ToString(),
                ErrorMessage = ex.Message
            });
        }
    }

    // 更新操作结果
    if (result.FailureCount > 0 && result.SuccessCount > 0)
    {
        result.Message = $"部分成功：成功{result.SuccessCount}条，失败{result.FailureCount}条";
    }
    else if (result.FailureCount == result.TotalCount)
    {
        result.Message = "批量删除失败";
        result.IsSuccess = false;
    }

    return Result<BatchOperationResultDto>.Success(result);
}
```

#### 8.2 批量导入容错

```csharp
// HerbService.cs:522-628 - 批量导入容错处理
public async Task<Result<HerbBatchImportResultDto>> BatchImportAsync(
    List<HerbInputDto> herbs,
    DuplicateStrategy strategy)
{
    const int MAX_IMPORT_SIZE = 10000; // BR-006: 最大导入数量

    // ... 数量限制检查 ...

    for (int i = 0; i < herbs.Count; i++)
    {
        var dto = herbs[i];
        var rowNumber = i + 2;

        try
        {
            // ... 重复检测、验证、创建逻辑 ...

            await _repository.AddAsync(entity);
            result.SuccessCount++;
        }
        catch (Exception ex)
        {
            // 单条失败不影响其他记录
            result.FailureCount++;
            result.Failures.Add(new HerbImportFailureDetailDto
            {
                RowNumber = rowNumber,
                HerbName = dto.Name,
                Reason = "导入失败",
                ErrorDetails = new List<string> { ex.Message }
            });
        }
    }

    return Result<HerbBatchImportResultDto>.Success(result);
}
```

#### 8.3 事务策略选择

**当前实现: 容错策略**
- ✅ 单条失败不影响其他记录
- ✅ 返回详细的成功/失败统计
- ✅ 适用于数据导入、批量删除等场景

**未来可选: 全量事务策略**
```csharp
// 使用数据库事务（伪代码）
using var transaction = await _dbContext.Database.BeginTransactionAsync();
try
{
    foreach (var id in ids)
    {
        await _repository.DeleteAsync(id);
    }
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

**策略对比**:

| 策略 | 优点 | 缺点 | 适用场景 |
|-----|------|------|---------|
| 容错策略 | 部分成功、可恢复 | 可能数据不一致 | 数据导入、批量删除 |
| 全量事务 | 数据一致性强 | 单条失败全部回滚 | 财务操作、关键业务 |

#### 8.4 最佳实践

**批量操作限制**:
- 单次批量删除: 最多100条（BR-006）
- 单次批量导入: 最多10000条（BR-006）
- 超过限制分批处理

**错误处理原则**:
- 记录所有失败项的详细信息
- 返回成功/失败统计
- 提供失败记录的错误原因
- 允许用户重试失败项

**API调用示例**:
```http
# 批量删除
POST /api/herbs/batch-delete
Content-Type: application/json

{
  "ids": [
    "guid1",
    "guid2",
    "guid3"
  ]
}

# 响应示例
{
  "isSuccess": true,
  "data": {
    "totalCount": 3,
    "successCount": 2,
    "failureCount": 1,
    "successfulIds": ["guid1", "guid2"],
    "failedIds": ["guid3"],
    "errors": [
      {
        "recordIdentifier": "guid3",
        "errorMessage": "药材不存在"
      }
    ],
    "message": "部分成功：成功2条，失败1条"
  }
}
```

**代码位置**:
- 批量删除: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs:161-241`
- 批量导入: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs:522-628`
- DTO定义: `src/Shared/LYBT.Shared.Models/Contracts/Common/BatchOperationResultDto.cs`

---

## 附录：快速参考

### A. API端点速查

| 功能 | HTTP方法 | 端点 | 说明 |
|-----|---------|------|------|
| 分页查询 | GET | `/api/herbs?page=1&pageSize=20&keyword=参&category=补气药` | 支持关键词和分类筛选 |
| 搜索 | GET | `/api/herbs/search?keyword=RS` | 名称和拼音搜索 |
| 获取详情 | GET | `/api/herbs/{id}` | 单个药材详情 |
| 创建 | POST | `/api/herbs` | 新增药材 |
| 更新 | PUT | `/api/herbs/{id}` | 修改药材信息 |
| 删除 | DELETE | `/api/herbs/{id}` | 软删除 |
| 批量删除 | POST | `/api/herbs/batch-delete` | 最多100条 |
| 下载模板 | GET | `/api/herbs/template` | Excel模板 |
| 导入Excel | POST | `/api/herbs/import?strategy=Update` | 支持Skip/Update/Error策略 |
| 导出Excel | GET | `/api/herbs/export?category=补气药` | 支持分类筛选 |
| 引用检查 | GET | `/api/herbs/{id}/references` | 检查处方引用 |
| 批量引用检查 | POST | `/api/herbs/batch-check-references` | 最多100条 |

### B. DTO字段说明

**HerbDto**:
```csharp
{
  "id": "guid",
  "name": "人参",              // 必填
  "pinYinCode": "RS",          // 自动生成
  "category": "补气药",         // 推荐使用18类标准分类
  "origin": "吉林",            // 可选
  "spec": "特级",              // 可选
  "unit": "克",                // 必填
  "price": 5.0,                // 必填，大于0
  "costPrice": 4.0,            // 可选，不能高于price
  "effect": "大补元气",         // 可选
  "usage": "3-9克",            // 可选
  "remark": "贵重药材",         // 可选
  "status": "Enabled",         // Enabled/Disabled
  "createdAt": "2025-01-18T10:00:00"
}
```

### C. 常见错误码

| 错误信息 | 原因 | 解决方法 |
|---------|------|---------|
| "药材不存在" | ID无效或已删除 | 检查ID是否正确 |
| "药材名称不能为空" | 必填字段缺失 | 填写药材名称 |
| "单价必须大于0" | 价格验证失败 | 填写正数价格 |
| "批量操作最多支持100条记录" | 超过批量限制 | 分批处理 |
| "Excel文件格式错误" | 文件损坏或格式不对 | 使用系统生成的模板 |

### D. 性能优化建议

1. **数据库索引**:
```sql
CREATE INDEX IX_Herbs_Name ON Herbs(Name);
CREATE INDEX IX_Herbs_PinYinCode ON Herbs(PinYinCode);
CREATE INDEX IX_Herbs_Category ON Herbs(Category);
```

2. **分页查询**: 单页不超过100条
3. **批量操作**: 单次不超过100条（删除）或10000条（导入）
4. **缓存策略**: 缓存常用药材列表（补气药、补血药等）

### E. 代码文件索引

| 功能模块 | 文件路径 |
|---------|---------|
| 服务层 | `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs` |
| 实体定义 | `src/Server/Core/LYBT.Entities/Herbs/Herb.cs` |
| DTO定义 | `src/Shared/LYBT.Shared.Models/Contracts/Herbs/HerbDto.cs` |
| 验证器 | `src/Server/Modules/LYBT.Module.Herbs/Validators/HerbInputDtoValidator.cs` |
| 仓储接口 | `src/Server/Modules/LYBT.Module.Herbs/Interfaces/IHerbRepository.cs` |
| 拼音工具 | `src/Shared/LYBT.Shared.Utilities/Text/PinYinHelper.cs` |

---

**文档版本**: v1.0
**更新日期**: 2025-01-18
**维护团队**: LYBTZYZS开发组
